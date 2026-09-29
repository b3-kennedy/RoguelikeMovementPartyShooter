using UnityEngine;
using Unity.Netcode;

public class Dispenser : NetworkBehaviour
{

    public GameObject basketBall;
    public Transform basketballSpawn;

    GameObject spawnedBall;
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SpawnBastketBallRpc()
    {
        spawnedBall = Instantiate(basketBall, basketballSpawn.position, Quaternion.identity);
        spawnedBall.GetComponent<NetworkObject>().Spawn();
    }
}
