using UnityEngine;
using Unity.Netcode;

public class SpecialRoom : NetworkBehaviour
{
    public void OnEnter()
    {
        if(IsServer)
        {
            GameManager.Instance.IsPaused(true);
        }
    }
}
