using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Interact : NetworkBehaviour
{

    public Camera cam;
    PushToTalkController pushToTalkController;

    private Terminal currentTerminal;
    private Collider lastHitCollider;

    public Transform pickupPoint;

    private Pickupable heldPickupable;
    private Pickupable HeldPickupable
    {
        get => heldPickupable;
        set
        {
            heldPickupable = value;
            if (throwUI != null)
                throwUI.SetActive(value != null);
        }
    }

    private float throwForce;
    public float baseThrowForce;
    public float maxThrowForce = 25f;
    public float maxChargeTime = 1f;
    public AnimationCurve throwForceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    private float chargeTime;

    GameObject throwUI;
    Transform throwBar;

    PlayerGunManager playerGunManager;

    [HideInInspector] public Transform gunHolder;
    private FirstPersonRigidbodyController controller;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pushToTalkController = GetComponent<PushToTalkController>();
        throwUI = GameManager.Instance.throwUI;
        throwUI.SetActive(false);
        throwBar = GameManager.Instance.throwUI.transform.GetChild(1);
        throwBar.localScale = new Vector3(1, 0, 1);
        playerGunManager = GetComponent<PlayerGunManager>();
    }

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<FirstPersonRigidbodyController>();
        controller.playerCameraRef.OnValueChanged += OnCameraAssigned;
        TryResolveCamera();
    }

    public override void OnNetworkDespawn()
    {
        controller.playerCameraRef.OnValueChanged -= OnCameraAssigned;
    }

    private void OnCameraAssigned(NetworkObjectReference oldRef, NetworkObjectReference newRef)
    {
        TryResolveCamera();
    }

    private void TryResolveCamera()
    {
        if (cam != null) return;

        if (controller.playerCameraRef.Value.TryGet(out NetworkObject camObj))
        {
            SetCamera(camObj.GetComponent<Camera>());
        }
        else
        {
            StartCoroutine(WaitForCameraSpawn());
        }
    }

    private IEnumerator WaitForCameraSpawn()
    {
        while (cam == null)
        {
            if (controller.playerCameraRef.Value.TryGet(out NetworkObject camObj))
            {
                
                SetCamera(camObj.GetComponentInChildren<Camera>());
                yield break;
            }
            yield return null;
        }
    }

    private void SetCamera(Camera assignedCam)
    {
        cam = assignedCam;
        var netCam = cam.GetComponentInChildren<NetworkPlayerCamera>();
        pickupPoint = netCam.pickupPoint;
        gunHolder = netCam.gunHoldPoint;

        if (IsLocalPlayer)
            GameManager.Instance.SetLocalPlayerCamera(cam);
    }

    void TerminalInteraction()
    {
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 5f)
        && hit.collider.CompareTag("Terminal"))
        {
            if (hit.collider != lastHitCollider)
            {
                lastHitCollider = hit.collider;
                currentTerminal = hit.collider.GetComponent<TerminalRef>().terminal;
                currentTerminal.cursor.SetActive(true);
            }

            Vector2 screenPoint = cam.WorldToScreenPoint(hit.point);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                currentTerminal.canvas.GetComponent<RectTransform>(),
                screenPoint,
                cam,
                out Vector2 localPoint
            );
            currentTerminal.SetCursorScreenPoint(screenPoint);
            currentTerminal.cursor.GetComponent<RectTransform>().anchoredPosition = localPoint;
        }
        else if (currentTerminal != null)
        {
            currentTerminal.cursor.SetActive(false);
            currentTerminal = null;
            lastHitCollider = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        if (!cam) return;

        TerminalInteraction();
        Throw();

        if (gunHolder.childCount > 0)
        {
            if (Input.GetKey(KeyCode.Q))
            {
                throwUI.SetActive(true);
                chargeTime = Mathf.Clamp(chargeTime + Time.deltaTime, 0f, maxChargeTime);
                float chargeAmount = chargeTime / maxChargeTime;
                float curveValue = throwForceCurve.Evaluate(chargeAmount);
                throwForce = Mathf.Lerp(baseThrowForce, maxThrowForce, curveValue);
            }

            throwForce = Mathf.Clamp(throwForce, baseThrowForce, maxThrowForce);
            float barAmount = (throwForce - baseThrowForce) / (maxThrowForce - baseThrowForce);
            throwBar.localScale = new Vector3(1, barAmount, 1);

            if (Input.GetKeyUp(KeyCode.Q))
            {
                GameObject gun = gunHolder.GetChild(0).gameObject;
                int id = gun.GetComponent<GunReferenceHolder>().gunID;
                
                DropGunRpc(id, OwnerClientId, throwForce);
                chargeTime = 0f;
                throwForce = 0f;
                throwUI.SetActive(false);
            }
        }

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 5f))
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (hit.collider.CompareTag("Receiver"))
                    hit.collider.GetComponent<Receiver>().PlayAudioClip();

                Pickupable pickupable = hit.collider.GetComponent<Pickupable>();
                WorldButton worldButton = hit.collider.GetComponent<WorldButton>();
                GunReferenceHolder gunReferenceHolder = hit.collider.GetComponent<GunReferenceHolder>();
                if (pickupable != null)
                {
                    pickupable.RequestPickupRpc(NetworkManager.Singleton.LocalClientId);
                    if(playerGunManager.GetGun())
                    {
                        ShowGunRpc(false, OwnerClientId);
                    }
                    
                    HeldPickupable = pickupable;
                    throwForce = baseThrowForce;
                    chargeTime = 0f;
                }

                if (worldButton != null)
                {
                    worldButton.press.Invoke();
                }

                if (gunReferenceHolder)
                {
                    if(playerGunManager.GetGun())
                    {
                        DropGunRpc(gunReferenceHolder.gunID, OwnerClientId, 0);
                    }
                    if (gunReferenceHolder.GetComponent<OnPickup>())
                    {
                        gunReferenceHolder.GetComponent<OnPickup>().OnPickedUpRpc();
                    }
                    SpawnGunRpc(gunReferenceHolder.gunID, OwnerClientId);
                    DestroyGunPickupRpc(gunReferenceHolder.pickupInstanceID);
                }
                
            }

            if (Input.GetKey(KeyCode.E) && hit.collider.CompareTag("Sender"))
                pushToTalkController.StartRecording();

            if (Input.GetKeyUp(KeyCode.E) && hit.collider.CompareTag("Sender"))
                pushToTalkController.StopRecording();
        }
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
    void ShowGunRpc(bool value, ulong clientID)
    {
        //if (NetworkManager.Singleton.LocalClientId != clientID) return;
        if(gunHolder)
        {
            NetworkManager.ConnectedClients[clientID].PlayerObject.GetComponent<Interact>().gunHolder.GetChild(0).gameObject.SetActive(value);
        }
        
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    void SpawnGunRpc(int gunID, ulong clientID)
    {
        GameObject gunPrefab = GameManager.Instance.guns[gunID].gun;
        GameObject gun = Instantiate(gunPrefab, cam.GetComponent<NetworkPlayerCamera>().gunHoldPoint);
        Debug.Log($"Spawning gun {gunID} for client {clientID}. Gun prefab: {gunPrefab.name}");
        gun.GetComponent<GunReferenceHolder>().gunID = gunID;
        if (NetworkManager.Singleton.LocalClientId != clientID) return;
        var player = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<FirstPersonRigidbodyController>();
        player.OnPickupGun();
        Gun gunComp = gun.GetComponent<Gun>();
        GetComponent<PlayerGunManager>().SetGun(gunComp);

    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    void DropGunRpc(int gunID, ulong clientID, float force)
    {
        Transform holder = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject.GetComponent<Interact>().gunHolder;
        GameObject gun = holder.transform.GetChild(0).gameObject;
        Vector3 gunPos = gun.transform.GetChild(0).position;
        Quaternion gunRot = gun.transform.GetChild(0).rotation;
        GameObject spawner = GameManager.Instance.guns[gunID].pickupObject;
        Destroy(gun);
        GameObject spawned = Instantiate(spawner, gunPos, gunRot);
        spawned.GetComponent<Rigidbody>().AddForce(cam.transform.forward * force, ForceMode.Impulse);
        spawned.GetComponent<GunReferenceHolder>().SetPickupInstanceID(GameManager.Instance.nextPickupID++);
        if (NetworkManager.Singleton.LocalClientId != clientID) return;
        GetComponent<PlayerGunManager>().RemoveGun();
        
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    void DestroyGunPickupRpc(int pickupInstanceID)
    {
        Debug.Log($"Trying to destroy pickup ID {pickupInstanceID}. Registered keys: {string.Join(",", GameManager.Instance.activePickups.Keys)}");

        if (GameManager.Instance.activePickups.TryGetValue(pickupInstanceID, out var p))
        {
            Destroy(p.gameObject);
        }
    }

    void Throw()
    {
        if (HeldPickupable != null && IsLocalPlayer)
        {
            if (Input.GetButton("Fire1"))
            {
                chargeTime = Mathf.Clamp(chargeTime + Time.deltaTime, 0f, maxChargeTime);
                float chargeAmount = chargeTime / maxChargeTime;
                float curveValue = throwForceCurve.Evaluate(chargeAmount);
                throwForce = Mathf.Lerp(baseThrowForce, maxThrowForce, curveValue);
            }

            throwForce = Mathf.Clamp(throwForce, baseThrowForce, maxThrowForce);
            float barAmount = (throwForce - baseThrowForce) / (maxThrowForce - baseThrowForce);
            throwBar.localScale = new Vector3(1, barAmount, 1);

            if (Input.GetButtonUp("Fire1"))
            {
                HeldPickupable.ApplyThrowForceRpc(throwForce, cam.transform.forward);
                HeldPickupable.RequestDropRpc();
                HeldPickupable = null;
                throwForce = baseThrowForce;
                chargeTime = 0f;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void DespawnHeldObjectRpc(ulong netObjID)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjID, out var obj)) return;

        obj.Despawn();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RightPathComplete"))
        {
            other.GetComponentInParent<PathRoom>().SetRightPathCompleteRpc();
        }

        if (other.CompareTag("LeftPathComplete"))
        {
            other.GetComponentInParent<PathRoom>().SetLeftPathCompleteRpc();
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (heldPickupable)
        {
            if (other.CompareTag("DestroyHeldObject"))
            {
                DespawnHeldObjectRpc(heldPickupable.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;

        if (other.CompareTag("Enter"))
        {
            Room room = other.GetComponentInParent<Room>();
            room.PlayerEntered();
            if (playerGunManager.GetGun())
            {
                if (room.canHaveGuns)
                {
                    playerGunManager.GetGun().SetHidden(false);
                    ShowGunRpc(true, OwnerClientId);
                }
                else
                {
                    playerGunManager.GetGun().SetHidden(true);
                    ShowGunRpc(false, OwnerClientId);
                }
            }

            
        }

        if (other.CompareTag("Exit"))
        {
            other.GetComponentsInParent<Room>()[0].PlayerExited();
        }
    }
}