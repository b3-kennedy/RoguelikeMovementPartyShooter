using Unity.Netcode;
using UnityEngine;

public class VoiceManager : NetworkBehaviour
{
    public AudioSource remotePlaybackSource;
    // Called locally by whichever client just finished talking
    public void SendReversedVoiceTo(float[] reversedSamples, int sampleRate, ulong targetClientId)
    {
        byte[] pcmBytes = AudioUtils.FloatsToPCM16Bytes(reversedSamples);

        // Step 1: send to the server (works from any client)
        RelayReversedVoiceRpc(pcmBytes, sampleRate, targetClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RelayReversedVoiceRpc(byte[] pcmBytes, int sampleRate, ulong targetClientId)
    {
        ReceiveReversedVoiceRpc(pcmBytes, sampleRate);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReceiveReversedVoiceRpc(byte[] pcmBytes, int sampleRate)
    {
        Debug.Log($"[VoiceManager] Received {pcmBytes.Length} bytes, playing at {sampleRate}Hz");

        float[] samples = AudioUtils.PCM16BytesToFloats(pcmBytes);

        AudioClip clip = AudioClip.Create("IncomingReversedVoice", samples.Length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        remotePlaybackSource.GetComponent<Receiver>().GetAudioClip(samples, sampleRate);
    }
}