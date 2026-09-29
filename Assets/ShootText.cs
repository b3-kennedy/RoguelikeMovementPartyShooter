using UnityEngine;
using TMPro;
using Unity.Netcode;

public class ShootText : NetworkBehaviour
{

    TextMeshProUGUI text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        text = GetComponent<TextMeshProUGUI>();
        if(NetworkManager.Singleton.LocalClientId == 0)
        {
            text.text = "SHOOT THE RED TARGETS";
        }
        else
        {
            text.text = "SHOOT THE BLUE TARGETS";
        }
    }

}
