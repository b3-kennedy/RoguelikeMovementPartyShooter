using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class NetworkCanvas : MonoBehaviour
{

    public Button hostButton;
    public Button joinButton;

    public GameObject lobbyCam;
    
    void Start()
    {
        hostButton.onClick.AddListener(Host);
        joinButton.onClick.AddListener(Join);
    }

    public void Host()
    {
        NetworkManager.Singleton.StartHost();
        lobbyCam.SetActive(false);
        gameObject.SetActive(false);
    }
    
    public void Join()
    {
        NetworkManager.Singleton.StartClient();
        lobbyCam.SetActive(false);
        gameObject.SetActive(false);
    }
}
