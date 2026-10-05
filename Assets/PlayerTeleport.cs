using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerTeleport : NetworkBehaviour
{
    [Rpc(SendTo.Owner)]
    public void TeleportRpc(Vector3 pos, Quaternion rot)
    {
        GetComponent<NetworkTransform>().Teleport(pos, rot, transform.localScale);
    }
}