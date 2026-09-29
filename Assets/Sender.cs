using UnityEngine;

public class Sender : Terminal
{

    PushToTalkController pushToTalkController;
    public GameObject startRecordingButton;
    public GameObject stopRecordingButton;

    public void StartRecording()
    {
        GetComponent<PushToTalkController>().StartRecording();
        startRecordingButton.SetActive(false);
        stopRecordingButton.SetActive(true);
    }
    
    public void StopRecording()
    {
        GetComponent<PushToTalkController>().StopRecording();
        startRecordingButton.SetActive(true);
        stopRecordingButton.SetActive(false);
    }
    
    public void ListenToRecording()
    {
        GetComponent<PushToTalkController>().ListenToRecording();
    }
    
    public void SendRecording()
    {
        GetComponent<PushToTalkController>().SendRecording();
    }
    
}
