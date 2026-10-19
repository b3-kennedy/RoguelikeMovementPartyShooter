using UnityEngine;
using Unity.Netcode;

public class SpecialRoom : NetworkBehaviour
{

    public WeaponSpawner[] weaponSpawners;
    
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void OnEnterRpc()
    {
        if (IsServer)
        {
            GameManager.Instance.IsPaused(true);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (var spawner in weaponSpawners)
            {
                spawner.SpawnWeapon();
            }
        }
    }
}
