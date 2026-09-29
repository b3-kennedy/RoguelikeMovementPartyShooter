using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public class RedTarget : NetworkBehaviour
{

    public UnityEvent onHitCorrect;
    public UnityEvent onHitIncorrect;
    public enum TargetColor
    {
        Red,
        Blue
    }
    
    public TargetColor targetColor;

    
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        Destroy(gameObject, 2f);
    }

    public override void OnNetworkDespawn()
    {
        onHitCorrect.RemoveAllListeners();
        onHitIncorrect.RemoveAllListeners();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void OnHitServerRpc(ulong clientID)
    {
        if(clientID == OwnerClientId)
        {
            onHitCorrect?.Invoke();
            GetComponent<NetworkObject>().Despawn();
            
        }
        else
        {
            onHitIncorrect?.Invoke();
            GetComponent<NetworkObject>().Despawn();
            
        }
    }
}
