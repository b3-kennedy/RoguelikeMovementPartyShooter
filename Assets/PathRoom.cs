using Unity.Netcode;
using UnityEngine;

public class PathRoom : NetworkBehaviour
{
    private NetworkVariable<int> pathSeed = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public PathGenerator[] generators;

    public NetworkVariable<bool> rightPathComplete = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> leftPathComplete = new NetworkVariable<bool>(false);

    public NetworkVariable<bool> roomComplete = new NetworkVariable<bool>(false);


    public override void OnNetworkSpawn()
    {
        rightPathComplete.OnValueChanged += OnPathCompleteChanged;
        leftPathComplete.OnValueChanged += OnPathCompleteChanged;

        // Check the current values too, in case they were already changed
        CheckRoomComplete();
    }

    public override void OnNetworkDespawn()
    {
        rightPathComplete.OnValueChanged -= OnPathCompleteChanged;
        leftPathComplete.OnValueChanged -= OnPathCompleteChanged;
    }

    private void OnPathCompleteChanged(bool oldValue, bool newValue)
    {
        CheckRoomComplete();
    }

    private void CheckRoomComplete()
    {
        if (!IsServer)
            return;

        roomComplete.Value = rightPathComplete.Value && leftPathComplete.Value;
        if(roomComplete.Value)
        {
            SetOnCompleteRpc();
        }
    }
    
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    void SetOnCompleteRpc()
    {
        GetComponent<Room>().OnComplete();
    }
    
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetRightPathCompleteRpc()
    {
        rightPathComplete.Value = true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetLeftPathCompleteRpc()
    {
        leftPathComplete.Value = true;
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void SpawnPathsRpc()
    {
        foreach (PathGenerator gen in generators)
        {
            gen.OnSpawned();
        }
    }
}
