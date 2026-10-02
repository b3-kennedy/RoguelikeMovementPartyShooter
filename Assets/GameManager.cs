using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Dissonance;
using UnityEngine.Events;

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

    public readonly Dictionary<int, GunReferenceHolder> activePickups = new();
    public int nextPickupID = 1000;

    public void RegisterPickup(GunReferenceHolder p) => activePickups[p.pickupInstanceID] = p;
    public void UnregisterPickup(GunReferenceHolder p) => activePickups.Remove(p.pickupInstanceID);
    
    public NetworkVariable<int> roomsCleared = new NetworkVariable<int>();
    public UnityEvent roomCleared;

    public GameObject joe;
    
    public NetworkVariable<int> points = new NetworkVariable<int>();

    int roomNumber = 0;


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
    
    public void OnEnterMagDumpRoom(Vector3 spawnPosition)
    {
        SpawnJoeRpc(spawnPosition);
    }

    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Server)]
    void SpawnJoeRpc(Vector3 position)
    {
        GameObject joeInstance = Instantiate(joe, position, Quaternion.identity);
        joeInstance.GetComponent<NetworkObject>().Spawn();
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
        if(IsServer)
        {
            if(Input.GetKeyDown(KeyCode.P) && canSpawnRooms)
            {
                SpawnRooms();
                canSpawnRooms = false;
            }
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


        // for (int i = 0; i < numberOfRooms; i++)
        // {

        //     GameObject roomPrefab = rooms[Random.Range(0, rooms.Length)];
        //     if (i == 0)
        //     {

        //         GameObject roomInstance = Instantiate(roomPrefab, roomStartPoint.position, Quaternion.identity);
        //         roomInstance.GetComponent<NetworkObject>().Spawn();
        //         spawnedRooms.Add(roomInstance);
        //     }
        //     else
        //     {
        //         Vector3 nextSpawn = spawnedRooms[i - 1].GetComponent<RoomConnectionPoint>().connectionPoint.position;
        //         GameObject roomInstance = Instantiate(roomPrefab, nextSpawn, Quaternion.identity);
        //         roomInstance.GetComponent<NetworkObject>().Spawn();
        //         spawnedRooms.Add(roomInstance);
        //     }



        // }
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
