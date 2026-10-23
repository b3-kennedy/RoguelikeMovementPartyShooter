using Unity.Netcode;
using UnityEngine;

public class PointsCollectable : NetworkBehaviour
{
    public int pointValue;
    [HideInInspector] public GameObject player;

    public float pullForce = 5f;
    public float liftAmount = 2f;
    public float distanceMultiplier = 1f;

    Rigidbody rb;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if(player)
        {
            Vector3 toCenter = (player.transform.position - rb.position).normalized;
            float distance = Vector3.Distance(player.transform.position, rb.position);
            Vector3 force = (toCenter + Vector3.up * liftAmount) * (pullForce * distance);
            rb.linearVelocity = force;
            if(distance <= 0.5f && IsServer)
            {
                GameManager.Instance.points.Value += pointValue;
                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}
