using UnityEngine;

public class OnPickup : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


    public void OnPickedUpRpc()
    {
        GetComponentInParent<AimTrainRoom>().OnPickedUpRpc();
    }
}
