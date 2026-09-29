using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerCamera : NetworkBehaviour
{
    public Transform pickupPoint;
    public Transform gunHoldPoint;
    
    public override void OnNetworkSpawn()
    {
        if(!IsOwner)
        {
            GetComponent<Camera>().enabled = false;
            GetComponent<AudioListener>().enabled = false;
        }
    }
    
}
