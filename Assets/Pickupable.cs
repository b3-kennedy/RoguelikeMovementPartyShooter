using UnityEngine;
using Unity.Netcode;

public class Pickupable : NetworkBehaviour
{
    public NetworkVariable<ulong> currentHolderClientId = new NetworkVariable<ulong>(ulong.MaxValue);
    private Transform followTarget;
    private Rigidbody rb;
    private Collider col;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        currentHolderClientId.OnValueChanged += (_, newHolder) => ApplyHolder(newHolder);
        ApplyHolder(currentHolderClientId.Value);
    }

    private void ApplyHolder(ulong holderId)
    {
        if (holderId == ulong.MaxValue)
        {
            followTarget = null;
            rb.isKinematic = false;
            col.enabled = true;
            return;
        }

        NetworkClient client = NetworkManager.Singleton.ConnectedClients[holderId];
        followTarget = client.PlayerObject.GetComponent<Interact>().cam.transform.Find("PickupPoint");
        rb.isKinematic = true;
        col.enabled = false;
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;
        transform.position = followTarget.position;
        transform.rotation = followTarget.rotation;
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    public void ApplyThrowForceRpc(float force, Vector3 dir)
    {
        rb.isKinematic = false;
        col.enabled = true;
        followTarget = null;
        rb.AddForce(dir * force, ForceMode.Impulse);
    }

    [Rpc(SendTo.Server)]
    public void RequestPickupRpc(ulong clientId)
    {
        if (currentHolderClientId.Value != ulong.MaxValue) return;
        GetComponent<NetworkObject>().ChangeOwnership(clientId);
        currentHolderClientId.Value = clientId;
    }

    [Rpc(SendTo.Server)]
    public void RequestDropRpc()
    {
        currentHolderClientId.Value = ulong.MaxValue;
        GetComponent<NetworkObject>().RemoveOwnership();
    }
}