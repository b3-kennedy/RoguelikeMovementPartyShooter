using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

public class ReverseSpeechRoomsManager : NetworkBehaviour
{
    private GameObject room1EnterDoor;
    private GameObject room1ExitDoor;
    
    private GameObject room2EnterDoor;
    
    private GameObject room2ExitDoor;

    public Receiver receiver;

    public override void OnNetworkSpawn()
    {
        GetComponent<ReverseGameWords>().failCount.OnValueChanged += HandleFailCountChanged;
    }

    public override void OnNetworkDespawn()
    {
        GetComponent<ReverseGameWords>().failCount.OnValueChanged -= HandleFailCountChanged;
    }

    public void CheckGuessRpc(string guess)
    {
        if(guess.ToLower() == GetComponent<ReverseGameWords>().currentWord.ToLower())
        {
            Debug.Log(guess + " is correct!");
            OnRoomCompleteRpc();
            
        }
        else
        {
            Debug.Log(guess + " is incorrect.");
            OnRoomFailRpc();
            
        }
    }
    
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void OnRoomCompleteRpc()
    {
        if (IsServer)
        {
            GameManager.Instance.points.Value += 500;
        }
        GetComponent<Room>().OnComplete();
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void OnRoomFailRpc()
    {
        ReverseGameWords rgw = GetComponent<ReverseGameWords>();
        GetComponent<Room>().OnFail();

        if (IsServer)
        {
            rgw.failCount.Value++;

            if (rgw.failCount.Value >= 3)
            {
                StartCoroutine(Reset());
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ThirdFailRpc()
    {
        ReverseGameWords rgw = GetComponent<ReverseGameWords>();
        for (int i = 0; i < rgw.failIndicatorParent.childCount; i++)
        {
            rgw.failIndicatorParent.GetChild(i).gameObject.SetActive(false);
        }

        if (IsServer)
        {
            rgw.PickWordRpc();
        }
    }

    IEnumerator Reset()
    {
        ReverseGameWords rgw = GetComponent<ReverseGameWords>();
        yield return new WaitForSeconds(1f);
        ThirdFailRpc();
        receiver.hasClip.Value = false;
        rgw.failCount.Value = 0;
    }


    private void HandleFailCountChanged(int previousValue, int newValue)
    {
        ReverseGameWords rgw = GetComponent<ReverseGameWords>();
        if (newValue > previousValue && newValue - 1 < rgw.failIndicatorParent.childCount)
        {
            rgw.failIndicatorParent.GetChild(newValue - 1).gameObject.SetActive(true);
        }
    }

    public void OpenExitDoors()
    {
        
    }
    
    public void CloseEnterDoors()
    {
        
    }
}
