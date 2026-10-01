using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Dissonance;
using System.Collections;

public class Room : NetworkBehaviour
{
    public enum RoomType { REVERSE_SPEECH, BASKETBALL, PATH, MAG_DUMP, AIM_TRAIN}
    public RoomType roomType;
    public Light[] roomLights;
    public GameObject[] enterDoors;
    public GameObject[] exitDoors;

    public bool canHaveGuns;
    Color normalLightColour;

    public bool turnLightNormal;

    // Server-only bookkeeping — dedupes by client, so one player can never count twice.
    private readonly HashSet<ulong> _enteredClients = new HashSet<ulong>();
    private readonly HashSet<ulong> _exitedClients = new HashSet<ulong>();

    void Start()
    {
        if (roomLights.Length <= 0) return;
        normalLightColour = roomLights[0].color;
    }

    public void PlayerEntered()
    {
        if (IsServer)
        {
            RegisterPlayerEntered(NetworkManager.ServerClientId);
        }
        else
        {
            RegisterPlayerEnteredRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RegisterPlayerEnteredRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        RegisterPlayerEntered(senderId);
    }

    private void RegisterPlayerEntered(ulong clientId)
    {
        if (!_enteredClients.Add(clientId))
        {
            Debug.LogWarning($"[Room] Client {clientId} tried to register entry twice, ignoring.");
            return;
        }

        Debug.Log($"[Room] Client {clientId} entered. {_enteredClients.Count}/2");

        if (_enteredClients.Count == 2)
        {
            OnEnter();
        }
    }

    public void PlayerExited()
    {
        if (IsServer)
        {
            RegisterPlayerExited(NetworkManager.ServerClientId);
        }
        else
        {
            RegisterPlayerExitedRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RegisterPlayerExitedRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        RegisterPlayerExited(senderId);
    }

    private void RegisterPlayerExited(ulong clientId)
    {
        if (!_exitedClients.Add(clientId))
        {
            Debug.LogWarning($"[Room] Client {clientId} tried to register exit twice, ignoring.");
            return;
        }

        Debug.Log($"[Room] Client {clientId} exited. {_exitedClients.Count}/2");

        if (_exitedClients.Count == 2)
        {
            OnExit();
        }
    }

    private void OnEnter()
    {
        CloseEnterDoorRpc();

        switch (roomType)
        {
            case RoomType.REVERSE_SPEECH:
                GetComponentInParent<ReverseGameWords>().StartRoom();
                break;

            case RoomType.BASKETBALL:
                GetComponentInParent<BasketBallRooms>().StartRoom();
                GameManager.Instance.OnEnterBasketballRoom();
                break;
            
            case RoomType.PATH:
                GameManager.Instance.SwitchCamerasRpc();
                GetComponent<PathRoom>().SpawnPathsRpc();
                EnableGlobalCommsRpc(true);
                break;
            
            case RoomType.MAG_DUMP:
                GetComponent<MagDumpRoom>().OnEnteredRpc();
                //GameManager.Instance.OnEnterMagDumpRoom(GetComponent<MagDumpRoom>().joeSpawnPoint.position);
                break;
            
            case RoomType.AIM_TRAIN:
                //GetComponent<AimTrainRoom>().StartRoom();
                break;

            default:
                Debug.LogWarning($"Unknown room type: {roomType}");
                break;
        }
    }
    
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    void EnableGlobalCommsRpc(bool value)
    {
        GameManager.Instance.EnableGlobalComms(value);
    }

    public void OnExit()
    {
        CloseExitDoorRpc();

        switch (roomType)
        {
            case RoomType.REVERSE_SPEECH:
                break;

            case RoomType.BASKETBALL:
                GameManager.Instance.OnExitBasketBallRoom();
                break;

            case RoomType.PATH:
                
                EnableGlobalCommsRpc(false);
                break;
                
            case RoomType.MAG_DUMP:
                //GameManager.Instance.OnExitMagDumpRoom();
                break;

            default:
                Debug.LogWarning($"Unknown room type: {roomType}");
                break;
        }
    }

    public void OnComplete()
    {
        
        
        foreach (Light light in roomLights)
        {
            light.color = Color.green;
        }
        
        foreach (GameObject door in exitDoors)
        {
            door.SetActive(false);
        }

        switch (roomType)
        {
            case RoomType.REVERSE_SPEECH:
                break;

            case RoomType.BASKETBALL:
                break;

            case RoomType.PATH:
                GameManager.Instance.SwitchCamerasRpc();
                EnableGlobalCommsRpc(false);
                break;

            default:
                Debug.LogWarning($"Unknown room type: {roomType}");
                break;
        }
        
        if(IsServer)
        {
            GameManager.Instance.roomsCleared.Value++;
            GameManager.Instance.roomCleared.Invoke();
        }
        
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void OpenEnterDoorRpc()
    {
        foreach (GameObject door in enterDoors)
        {
            door.SetActive(true);
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void CloseEnterDoorRpc()
    {
        foreach (GameObject door in enterDoors)
        {
            door.SetActive(true);
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void CloseExitDoorRpc()
    {
        foreach (GameObject door in exitDoors)
        {
            door.SetActive(true);
        }
    }

    public void OnFail()
    {
        foreach (Light light in roomLights)
        {
            light.color = Color.red;
            if (turnLightNormal)
            {
                StartCoroutine(TurnLightNormal());
            }
        }
    }
    
    IEnumerator TurnLightNormal()
    {
        yield return new WaitForSeconds(1f);
        foreach (Light light in roomLights)
        {
            light.color = normalLightColour;
        }
    }
}