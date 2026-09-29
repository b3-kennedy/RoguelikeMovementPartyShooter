using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Receiver : Terminal
{
    AudioSource audioSource;
    public NetworkVariable<bool> hasClip = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public GameObject messageReceivedText;
    public GameObject noMessageText;
    
    public TMPro.TMP_InputField inputField;
    public ReverseSpeechRoomsManager reverseSpeechRoomsManager;
    Terminal terminal;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        hasClip.OnValueChanged += HandleHasClipChanged;
        terminal = GetComponent<Terminal>();

    }
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            hasClip.Value = false;
        }
    }

    private void HandleHasClipChanged(bool oldValue, bool newValue)
    {
        messageReceivedText.SetActive(newValue);
        noMessageText.SetActive(!newValue);
    }

    public void GetAudioClip(float[] reversed, int sampleRate)
    {
        AudioClip clip = AudioClip.Create("ReversedRemote", reversed.Length, 1, sampleRate, false);
        clip.SetData(reversed, 0);
        audioSource.clip = clip;
        SetHasClipRpc(true);
    }
    
    public void GuessWord()
    {
        string guess = inputField.text;
        reverseSpeechRoomsManager.CheckGuessRpc(guess);
    }
    
    void Update()
    {
    }
    
    public void PlayAudioClip()
    {
        if (audioSource.clip != null)
        {
            StartCoroutine(PlayAudioClipCoroutine());
        }
        else
        {
            Debug.LogWarning("No audio clip to play.");
        }
    }
        
    IEnumerator PlayAudioClipCoroutine()
    {
        audioSource.Play();
        yield return new WaitForSeconds(audioSource.clip.length);
        // audioSource.clip = null;
        // SetHasClipRpc(false);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetHasClipRpc(bool value)
    {
        hasClip.Value = value;
    }

    public override void OnNetworkDespawn()
    {
        hasClip.OnValueChanged -= HandleHasClipChanged;
    }
}
