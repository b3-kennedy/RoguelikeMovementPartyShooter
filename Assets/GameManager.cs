using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
using UnityEngine.Events;
using TMPro;

[System.Serializable]
public class GunAndPickup
{
    public GameObject gun;
    public GameObject pickupObject;
}

public class GameManager : NetworkBehaviour
{

    public static GameManager Instance { get; private set; }
    private Camera localPlayerCamera;

    public GameObject[] rooms;
    public GunAndPickup[] guns;
    List<GameObject> spawnedRooms = new List<GameObject>();

    public int numberOfRooms = 10;

    public Transform roomStartPoint;

    public GameObject throwUI;
    public GameObject reloadUI;

    public GameObject ammoUI;

    bool canSpawnRooms = true;

    public GameObject dissonanceSetupObject;

    // ---------- Pickup registry ----------
    // Runtime-dropped guns count up from 1000. Placed pickups use fixed ranges far above that,
    // so the three sources can never collide.
    const int ScenePickupIdBase = 1_000_000; // pickups placed directly in the scene
    const int RoomPickupIdBase = 2_000_000; // pickups inside network-spawned room prefabs
    const int MaxPickupsPerRoom = 100;

    public readonly Dictionary<int, GunReferenceHolder> activePickups = new();
    public int nextPickupID = 1000;

    public void RegisterPickup(GunReferenceHolder p) => activePickups[p.pickupInstanceID] = p;

    public void UnregisterPickup(GunReferenceHolder p)
    {
        // Only remove the entry if it still points at this pickup, so a stale
        // unregister can't knock out a different object that now owns the key.
        if (activePickups.TryGetValue(p.pickupInstanceID, out var registered) && registered == p)
            activePickups.Remove(p.pickupInstanceID);
    }

    /// <summary>
    /// Gives every un-ID'd weapon pickup under 'root' a deterministic ID.
    /// Order comes from hierarchy position, so every client computes the same IDs.
    /// </summary>
    void AssignPickupIDs(Transform root, int idBase, IEnumerable<GunReferenceHolder> candidates)
    {
        var pickups = candidates
            .Where(p => p.GetComponent<WeaponPickup>() != null)   // removed the < 0 check
            .OrderBy(p => GetHierarchyPath(p.transform, root), System.StringComparer.Ordinal)
            .ToList();

        for (int i = 0; i < pickups.Count; i++)
            pickups[i].SetPickupInstanceID(idBase + i);
    }

    // Pickups placed directly in the scene (not inside a networked room or player)
    void AssignScenePickupIDs()
    {
        var all = FindObjectsByType<GunReferenceHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(p => p.GetComponentInParent<NetworkObject>() == null);

        AssignPickupIDs(null, ScenePickupIdBase, all);
    }

    /// <summary>
    /// Call this from the room's OnNetworkSpawn so pickups inside spawned rooms get IDs.
    /// The room's NetworkObjectId is identical on every client, which keeps IDs in sync.
    /// </summary>
    public void AssignRoomPickupIDs(NetworkObject room)
    {
        int idBase = RoomPickupIdBase + (int)room.NetworkObjectId * MaxPickupsPerRoom;
        var pickups = room.GetComponentsInChildren<GunReferenceHolder>(true);
        AssignPickupIDs(room.transform, idBase, pickups);
    }

    static string GetHierarchyPath(Transform t, Transform root)
    {
        string path = t.GetSiblingIndex().ToString("D4");
        while (t.parent != null && t.parent != root)
        {
            t = t.parent;
            path = t.GetSiblingIndex().ToString("D4") + "/" + path;
        }
        return path;
    }
    // -------------------------------------

    [Header("Game State")]
    public NetworkVariable<int> roomsCleared = new NetworkVariable<int>();
    public UnityEvent roomCleared;
    public NetworkVariable<int> points = new NetworkVariable<int>();

    [Header("Game Timer")]
    public float maxGameTime = 300f; // 5 minutes
    private NetworkVariable<float> gameTimer = new NetworkVariable<float>();

    public NetworkVariable<bool> pauseTimer = new NetworkVariable<bool>(false);
    public TextMeshProUGUI timerText;
    int roomNumber = 0;

    bool teleportedToBoss = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        AssignScenePickupIDs();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            gameTimer.Value = maxGameTime;
        }
    }

    void OnEnable()
    {
        roomCleared.AddListener(SpawnRooms);
    }

    void OnDisable()
    {
        roomCleared.RemoveListener(SpawnRooms);
    }

    public void OnEnterBasketballRoom()
    {

    }

    public void OnExitBasketBallRoom()
    {

    }
    public void EnableReloadUI(bool value)
    {
        reloadUI.SetActive(value);
    }

    public void EnableAmmoUI(bool value)
    {
        ammoUI.SetActive(value);
    }

    public void UpdateReloadProgress(float progress)
    {
        reloadUI.GetComponent<ReloadUI>().SetProgress(progress);
    }

    public void EnableGlobalComms(bool value)
    {
        dissonanceSetupObject.GetComponent<VoiceProximityBroadcastTrigger>().enabled = !value;
        dissonanceSetupObject.GetComponent<VoiceProximityReceiptTrigger>().enabled = !value;

        dissonanceSetupObject.GetComponent<VoiceBroadcastTrigger>().enabled = value;
        dissonanceSetupObject.GetComponent<VoiceReceiptTrigger>().enabled = value;
    }

    void Update()
    {
        if (!IsSpawned) return;

        if (IsServer)
        {
            if (Input.GetKeyDown(KeyCode.P) && canSpawnRooms)
            {
                SpawnRooms();
                canSpawnRooms = false;
            }


            if (!pauseTimer.Value)
            {
                gameTimer.Value = Mathf.Max(0f, gameTimer.Value - Time.deltaTime);
            }

            if (gameTimer.Value <= 0f && !teleportedToBoss)
            {
                SendToBossRpc();
                teleportedToBoss = true;
            }
        }

        int total = Mathf.CeilToInt(gameTimer.Value);
        timerText.text = $"{total / 60:00}:{total % 60:00}";
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Server)]
    void SendToBossRpc()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject.GetComponent<PlayerTeleport>();
            playerObject.TeleportRpc(new Vector3(100, 100, 100), Quaternion.identity);
        }

    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Server)]
    public void SwitchCamerasRpc()
    {
        var clients = NetworkManager.Singleton.ConnectedClientsList;

        if (clients.Count != 2)
            return;

        NetworkObject a = clients[0].PlayerObject;
        NetworkObject b = clients[1].PlayerObject;
        NetworkObject camA = clients[0].PlayerObject.GetComponent<Interact>().cam.transform.parent.parent.GetComponent<NetworkObject>();
        NetworkObject camB = clients[1].PlayerObject.GetComponent<Interact>().cam.transform.parent.parent.GetComponent<NetworkObject>();

        ulong ownerA = a.OwnerClientId;
        ulong ownerB = b.OwnerClientId;

        a.ChangeOwnership(ownerB);
        b.ChangeOwnership(ownerA);
        camA.ChangeOwnership(ownerB);
        camB.ChangeOwnership(ownerA);
    }

    public void SpawnRooms()
    {

        GameObject roomPrefab = rooms[Random.Range(0, rooms.Length)];
        if (roomNumber == 0)
        {

            GameObject roomInstance = Instantiate(roomPrefab, roomStartPoint.position, Quaternion.identity);
            roomInstance.GetComponent<NetworkObject>().Spawn();
            spawnedRooms.Add(roomInstance);
        }
        else
        {
            Vector3 nextSpawn = spawnedRooms[roomNumber - 1].GetComponent<RoomConnectionPoint>().connectionPoint.position;
            GameObject roomInstance = Instantiate(roomPrefab, nextSpawn, Quaternion.identity);
            roomInstance.GetComponent<NetworkObject>().Spawn();
            spawnedRooms.Add(roomInstance);
        }
        roomNumber++;
    }

    public void SetLocalPlayerCamera(Camera camera)
    {
        localPlayerCamera = camera;
    }

    public Camera GetLocalPlayerCamera()
    {
        return localPlayerCamera;
    }
}