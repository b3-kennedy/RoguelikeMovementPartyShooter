using Unity.Netcode;
using UnityEngine;

public class PushToTalkController : NetworkBehaviour
{
    [SerializeField] private MicRecorder micRecorder;
    [SerializeField] private ulong targetClientId; // set this to whoever you're challenging

    float[] reversedAudio;
    AudioSource localPlaybackSource;


    void Start()
    {
        localPlaybackSource = GetComponent<AudioSource>();
    }

    void Update()
    {

        if (!IsOwner) return;
    
    }
    
    public void StartRecording()
    {
        micRecorder.StartRecording();
    }
    
    public void StopRecording()
    {
        micRecorder.StopRecording();

        if (micRecorder.LastRecordedSamples == null || micRecorder.LastRecordedSamples.Length == 0)
            return;

        reversedAudio = AudioUtils.Reverse(micRecorder.LastRecordedSamples); 
    }
    
    public void ListenToRecording()
    {
        AudioClip localClip = AudioClip.Create("ReversedLocal", reversedAudio.Length, 1, micRecorder.SampleRate, false);
        localClip.SetData(reversedAudio, 0);
        localPlaybackSource.clip = localClip;
        localPlaybackSource.Play();
    }


    public void SendRecording()
    {
        transform.parent.root.GetComponent<VoiceManager>().SendReversedVoiceTo(reversedAudio, micRecorder.SampleRate, targetClientId);
    }
}