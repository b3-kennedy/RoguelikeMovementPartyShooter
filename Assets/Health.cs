using Unity.Netcode;
using UnityEngine;
using System;

public class Health : NetworkBehaviour
{
    public Action OnDeath;

    public float maxHealth = 1000f;
    public NetworkVariable<float> health;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
        }
    }
    
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageRpc(float damage)
    {
        health.Value -= damage;
        if(health.Value <= 0)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if(netObj)
            {
                OnDeath?.Invoke();
                netObj.Despawn();
            }
            else
            {
                DieRpc();
            }
        }
    }
    
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    void DieRpc()
    {
        Destroy(gameObject);
    }
}
