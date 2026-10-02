using UnityEngine;
using Unity.Netcode;
using TMPro;

public class MagDumpRoom : NetworkBehaviour
{

    public GameObject joe;
    public GameObject barrier;

    public TextMeshProUGUI timerText;
    
    public float timeLimit = 60f;
    float timer;
    
    bool roomEnabled = false;
    
    Health joeHealth;
    public Transform joeHealthBar;
    void Start()
    {
        joeHealth = joe.GetComponent<Health>();
        timer = timeLimit;
    }
    
    void OnEnable()
    {
        joe.GetComponent<Health>().OnDeath += OnJoeDeath;
    }
    
    void OnDisable()
    {
        joe.GetComponent<Health>().OnDeath -= OnJoeDeath;
    }
    
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void OnEnteredRpc()
    {
        //joe.GetComponent<NetworkObject>().Spawn();
        barrier.GetComponent<MeshRenderer>().enabled = false;
        roomEnabled = true;
    }
    
    
    void Update()
    {
        if(!roomEnabled) return;
        
        timer -= Time.deltaTime;
        timerText.text = $"Time Left: {Mathf.CeilToInt(timer)}";
        
        if(timer <= 0)
        {
            if(IsServer)
            {
                OnCompleteRpc();
            }
            
            roomEnabled = false;
        }
    }
    
    void OnJoeDeath()
    {
        OnCompleteRpc();
    }


    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    void OnCompleteRpc()
    {
        GetComponent<Room>().OnComplete();
        if(IsServer)
        {
            if (joeHealth.health.Value <= 0)
            {
                GameManager.Instance.points.Value += 1000;
            }
            else
            {
                GameManager.Instance.points.Value += Mathf.CeilToInt((joeHealth.health.Value / joeHealth.maxHealth) * 1000);
            }
        }


    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    public void UpdateHealthBarRpc()
    {
        joeHealthBar.localScale = new Vector3(joeHealth.health.Value / joeHealth.maxHealth, 1f, 1f);
    }
}
