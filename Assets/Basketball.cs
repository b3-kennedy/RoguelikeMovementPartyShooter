using Mono.Cecil.Cil;
using Unity.Netcode;
using UnityEngine;

public class Basketball : NetworkBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hoop"))
        {
            SendCompleteRpc(other.transform.parent.parent.parent.GetComponent<Room>().gameObject.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    void SendCompleteRpc(ulong roomNetID)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(roomNetID, out var room)) return;
        if(IsServer)
        {
            GameManager.Instance.points.Value += 500;
        }
        room.GetComponent<Room>().OnComplete();
    }
}
