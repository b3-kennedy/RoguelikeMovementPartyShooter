using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshMove : NetworkBehaviour
{
    NavMeshAgent agent;
    public Collider movementArea;
    Vector3 randomPosition;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }
    
    public override void OnNetworkSpawn()
    {
        if(!IsOwner)
        {
            agent.enabled = false;
        }
        agent = GetComponent<NavMeshAgent>();
        GetRandomPosition();
        agent.SetDestination(randomPosition);
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner) return;
        
        if (Vector3.Distance(transform.position, randomPosition) < 1f)
        {
            GetRandomPosition();
            agent.SetDestination(randomPosition);
        }
    }

    void GetRandomPosition()
    {
        randomPosition = new Vector3(Random.Range(movementArea.bounds.min.x, movementArea.bounds.max.x), transform.position.y,
            Random.Range(movementArea.bounds.min.z, movementArea.bounds.max.z));
    }
    
    void OnDrawGizmos()
    {
        if (movementArea)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(movementArea.bounds.center, movementArea.bounds.size);
        }
    }
}
