using UnityEngine;

public class MicRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int maxDurationSeconds = 30;

    private string micDevice;
    private AudioClip recordingClip;
    private bool isRecording;

    // In-scene storage of the last captured audio
    public float[] LastRecordedSamples { get; private set; }
    public int SampleRate => sampleRate;
    public bool IsRecording => isRecording;

    void Awake()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No microphone devices found.");
            return;
        }

        micDevice = Microphone.devices[0];
        Debug.Log($"Using microphone: {micDevice}");
    }

    public void StartRecording()
    {
        if (micDevice == null || isRecording) return;

        // loop=false so it stops writing once it hits maxDurationSeconds
        recordingClip = Microphone.Start(micDevice, false, maxDurationSeconds, sampleRate);
        isRecording = true;
    }

    public void StopRecording()
    {
        if (!isRecording) return;

        int recordedSampleCount = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);
        isRecording = false;

        if (recordedSampleCount <= 0)
        {
            Debug.LogWarning("No audio was captured.");
            LastRecordedSamples = new float[0];
            return;
        }

        // Pull only the portion that was actually recorded
        float[] samples = new float[recordedSampleCount];
        recordingClip.GetData(samples, 0);

        LastRecordedSamples = samples;
        Debug.Log($"Captured {samples.Length} samples ({samples.Length / (float)sampleRate:F2}s)");
    }

    // Forward playback (unmodified)
    public AudioClip GetRecordedClipForPlayback()
    {
        if (LastRecordedSamples == null || LastRecordedSamples.Length == 0)
            return null;

        AudioClip clip = AudioClip.Create("RecordedVoice", LastRecordedSamples.Length, 1, sampleRate, false);
        clip.SetData(LastRecordedSamples, 0);
        return clip;
    }

    // Reversed playback (copies the array so the original is untouched)
    public AudioClip GetReversedClipForPlayback()
    {
        if (LastRecordedSamples == null || LastRecordedSamples.Length == 0)
            return null;

        float[] reversed = AudioUtils.Reverse(LastRecordedSamples);

        AudioClip clip = AudioClip.Create("ReversedVoice", reversed.Length, 1, sampleRate, false);
        clip.SetData(reversed, 0);
        return clip;
    }
}