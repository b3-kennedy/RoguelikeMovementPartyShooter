using Unity.Netcode;
using UnityEngine;
using TMPro;

public class AimTrainRoom : NetworkBehaviour
{
    public Material redMat;
    public Material blueMat;

    public GameObject redTarget;
    public GameObject blueTarget;
    public Collider targetWall;
    
    public float roomDuration = 30f;
    NetworkVariable<float> roomTimer = new NetworkVariable<float>();

    public float timeBetweenTargets = 1f;
    float spawnZ;

    bool start;
    float timer;
    
    public NetworkVariable<int> gunsPickedUp = new NetworkVariable<int>();

    public NetworkVariable<int> roomScore;

    public TextMeshProUGUI startTimerText;
    public TextMeshProUGUI roomTimerText;
    float startTimer = 5f;

    public int upperScoreBoundary = 11000;
    public int middleScoreBoundary = 9000;
    public int lowerScoreBoundary = 7000;
    
    
    public void StartRoom()
    {
        start = true;
        timer = 0f;
        spawnZ = targetWall.bounds.max.z + -0.75f;
        roomTimerText.gameObject.SetActive(true);
        startTimerText.gameObject.SetActive(true);
        if(IsServer)
        {
            roomTimer.Value = roomDuration;
            roomScore.Value = 0;
        }
        
    }

    void Update()
    {

        if (gunsPickedUp.Value >= 2 && !start)
        {
            startTimerText.gameObject.SetActive(true);
            startTimer -= Time.deltaTime;
            startTimerText.text = Mathf.CeilToInt(startTimer).ToString();
            if (startTimer <= 0f)
            {
                StartRoom();
                startTimerText.gameObject.SetActive(false);
            }
        }

        roomTimerText.text = Mathf.CeilToInt(roomTimer.Value).ToString();

        if (!IsServer) return;
        if (!start) return;


        
        roomTimer.Value -= Time.deltaTime;
        if (roomTimer.Value <= 0f)
        {
            start = false;
            gunsPickedUp.Value = 0;
            HideRoomTimerRpc();
            OnCompleteRpc();
            Debug.Log($"[AimTrainRoom] Room complete! Final score: {roomScore.Value}");
            return;
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = timeBetweenTargets;
        SpawnTarget();
    }
    
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    void OnCompleteRpc()
    {
        GetComponent<Room>().OnComplete();
        if(IsServer)
        {
            if (roomScore.Value >= upperScoreBoundary)
            {
                GameManager.Instance.points.Value += 1000;
            }
            else if (roomScore.Value >= middleScoreBoundary)
            {
                GameManager.Instance.points.Value += 500;
            }
            else if (roomScore.Value >= lowerScoreBoundary)
            {
                GameManager.Instance.points.Value += 100;
            }
            else
            {
                GameManager.Instance.points.Value += 0;
            }
        }

        
    }

    void SpawnTarget()
    {
        Bounds b = targetWall.bounds;

        Vector3 redPos = new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            spawnZ
        );

        Vector3 bluePos = new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            spawnZ
        );

        GameObject newTarget = Instantiate(redTarget, redPos, redTarget.transform.rotation);
        GameObject newTarget2 = Instantiate(blueTarget, bluePos, blueTarget.transform.rotation);
        var redComp = newTarget.GetComponent<RedTarget>();
        redComp.onHitCorrect.AddListener(OnHitCorrect);
        redComp.onHitIncorrect.AddListener(OnHitIncorrect);
        var blueComp = newTarget2.GetComponent<RedTarget>();
        blueComp.onHitCorrect.AddListener(OnHitCorrect);
        blueComp.onHitIncorrect.AddListener(OnHitIncorrect);
        newTarget.GetComponent<NetworkObject>().Spawn(); //host owns red targets
        newTarget2.GetComponent<NetworkObject>().SpawnWithOwnership(NetworkManager.Singleton.ConnectedClientsList[1].ClientId); //client owns blue targets
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void OnPickedUpRpc()
    {
        gunsPickedUp.Value++;
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    void HideRoomTimerRpc()
    {
        roomTimerText.gameObject.SetActive(false);
    }

    void OnHitCorrect()
    {
        roomScore.Value += 100;
    }
    
    void OnHitIncorrect()
    {
        roomScore.Value -= 100;
    }
    
}