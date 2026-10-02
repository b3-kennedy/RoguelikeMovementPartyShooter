using System.Net.NetworkInformation;
using Unity.Netcode;
using UnityEngine;

public class FirstPersonRigidbodyController : NetworkBehaviour
{
    [SerializeField] private Transform playerObject;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jump")]
    [SerializeField] private float sprintJumpBoost = 1.3f;
    [SerializeField] private float jumpForce = 5f;

    [SerializeField] private float sprintMultiplier = 2f;

    [Header("Air Control")]
    [SerializeField] private float airAcceleration = 8f;
    float sprMultiplier;

    [Header("Look")]
    [SerializeField] private GameObject playerCameraPrefab;
    private Camera playerCamera;
    Transform cameraRot;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxPitchAngle = 80f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0.7f, 0f);

    [Header("Ground Check")]
    [SerializeField] private float capsuleRadius = 0.3f;
    [SerializeField] private float groundCheckDist = 0.15f;
    [SerializeField] private float skinWidth = 0.03f;
    [SerializeField] private float maxSlopeAngle = 55f;

    [Header("Wall Jumping")]
    [SerializeField] private float wallCheckDistance = 0.25f;
    [SerializeField] private float wallNormalForce = 3f;

    // Synced reference so ALL clients know which camera belongs to this player
    public NetworkVariable<NetworkObjectReference> playerCameraRef = new NetworkVariable<NetworkObjectReference>();

    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    private Rigidbody rb;
    private CapsuleCollider col;
    private float pitch = 0f;
    private float yaw = 0f;

    private GunBob gunBob => gunManager != null ? gunManager.GetGunBob() : null;

    Interact interact;
    bool isSprinting;
    
    PlayerGunManager gunManager;
    
    float lastAirYVel;
    public float minLandSpeed = 0f;
    public event System.Action<float> Landed;

    bool wasGrounded;

    bool canSlide = true;
    bool isSliding;
    Vector3 slideDir;
    float slideSpeed;
    float standHeight;
    Vector3 standCenter;
    Vector3 previousWallNormal;
    float camDrop;

    float groundLockTimer;

    bool hasWallJumped = false;

    [Header("Slide")]
    [SerializeField] private float slideBoost = 1.2f;
    [SerializeField] private float slideDeceleration = 10f;
    [SerializeField] private float slideHeight = 1f;
    [SerializeField] private float slideCameraDrop = 0.5f;
    [SerializeField] private float slideCameraSpeed = 12f;
    [SerializeField] private float minAirSlideSpeed = 6f;
    [SerializeField] private float slopeAcceleration = 30f;
    [SerializeField] private float maxSlideSpeed = 25f;
    // Slide steering
    [SerializeField] private float slideControl = 5f;
    [SerializeField] private float airSlideControl = 1.5f;
    [SerializeField] private float slideTurnLoss = 8f;
    [SerializeField] private float minTurnMultiplier = 0.25f;
    [SerializeField] private float slideLandingLoss = 0.5f;   // speed lost per unit of impact speed
    [SerializeField] private float minSlideLandSpeed = 4f;     // ignore small hops
    [SerializeField] private float flatLandAngle = 5f;   // max slope angle that counts as "flat"
    [SerializeField] private float ceilingCheckDistance = 0.25f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        gunManager = GetComponent<PlayerGunManager>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.freezeRotation = true;

        standHeight = col.height;
        standCenter = col.center;

        yaw = transform.eulerAngles.y;
        interact = GetComponent<Interact>();

        sprMultiplier = sprintMultiplier;
        
    }

    #region Networked Camera Handling
    public override void OnNetworkSpawn()
    {
        // Subscribe to network variable changes so remote clients get camera references
        playerCameraRef.OnValueChanged += OnCameraReferenceChanged;

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SpawnCameraServerRpc(OwnerClientId);
        }

        // Handle existing camera reference if spawning mid-game or on late sync
        if (playerCameraRef.Value.TryGet(out NetworkObject existingCam))
        {
            AssignCamera(existingCam);
        }
    }

    public override void OnNetworkDespawn()
    {
        playerCameraRef.OnValueChanged -= OnCameraReferenceChanged;
    }

    private void OnCameraReferenceChanged(NetworkObjectReference oldRef, NetworkObjectReference newRef)
    {
        if (newRef.TryGet(out NetworkObject camObj))
        {
            AssignCamera(camObj);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnCameraServerRpc(ulong clientID)
    {
        GameObject playerCam = Instantiate(playerCameraPrefab, Vector3.zero, Quaternion.identity);
        NetworkObject camNetObj = playerCam.GetComponent<NetworkObject>();

        camNetObj.SpawnWithOwnership(clientID);

        // Setting the NetworkVariable broadcasts the reference to ALL clients
        playerCameraRef.Value = camNetObj;
    }

    private void AssignCamera(NetworkObject camNetObj)
    {
        playerCamera = camNetObj.GetComponentInChildren<Camera>();
        cameraRot = playerCamera.transform.parent.parent;
        // 1. Assign camera reference to Interact component across ALL clients (fixes pickup nullref)
        if (TryGetComponent<Interact>(out var interact))
        {
            interact.cam = playerCamera;
            interact.gunHolder = playerCamera.transform.GetChild(0);
        }

        // 2. Configure components based on local ownership
        if (IsOwner)
        {
            playerCamera.enabled = true;
            playerCamera.gameObject.SetActive(true);

            if (playerCamera.TryGetComponent<AudioListener>(out var listener))
            {
                listener.enabled = true;
            }

            // gunBob = playerCamera.GetComponentInChildren<GunBob>();
            // if (gunBob != null)
            // {
            //     gunBob.enabled = true;
            // }
        }
        else
        {
            // Disable rendering & audio for remote instances to prevent screen overlapping
            playerCamera.enabled = false;

            if (playerCamera.TryGetComponent<AudioListener>(out var listener))
            {
                listener.enabled = false;
            }
        }
    }
    
     #endregion
    
    public void OnPickupGun()
    {
        // gunBob = playerCamera.GetComponentInChildren<GunBob>();
        // if (gunBob != null)
        // {
        //     gunBob.enabled = true;
        // }
    }
    
    public bool IsSprinting()
    {
        return isSprinting;
    }

    private void Update()
    {
        if(playerCamera == null && interact.cam != null) //works for now, better solution needed later
        {
            playerCamera = GetComponent<Interact>().cam;
        }
    
        if (!IsOwner) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);

        // Rotate player body immediately in Update
        playerObject.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (Input.GetButtonDown("Jump") && isGrounded && !(isSliding && HasCeiling()))
        {
            if (isSliding)
            {
                StopSlide(); // leaves horizontal velocity alone, so the slide speed carries into the jump
            }
            else if (isSprinting)
            {
                Vector3 v = rb.linearVelocity;
                rb.linearVelocity = new Vector3(v.x * sprintJumpBoost, v.y, v.z * sprintJumpBoost);
            }

            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            groundLockTimer = 0.15f;
        }

        if (Input.GetKeyDown(KeyCode.Space) && !isGrounded && !hasWallJumped)
        {
            if (CheckForWall(out RaycastHit hit))
            {            
                Vector3 wallNormal = hit.normal;
                if (wallNormal == previousWallNormal) return;
                previousWallNormal = wallNormal;
                
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                rb.AddForce(wallNormal * wallNormalForce + Vector3.up * jumpForce * 1.5f, ForceMode.Impulse);

            }
        }




        if (Input.GetKeyDown(KeyCode.C) && canSlide && !isSliding && CanStartSlide())
        {
            StartSlide();
        }


        if (Input.GetKeyDown(KeyCode.LeftShift) && isGrounded && !isSliding)
        {
            if (isSprinting)
                isSprinting = false;
            else if (Input.GetAxisRaw("Vertical") > 0.1f)
                isSprinting = true;
        }

        if (Input.GetAxisRaw("Vertical") <= 0.1f || isSliding)
        {
            isSprinting = false;
        }

        if (playerCamera != null)
        {
            if(isSliding)
            {
                cameraRot.transform.position = transform.position + new Vector3(cameraOffset.x, cameraOffset.y - slideCameraDrop, cameraOffset.z);
            }
            else
            {
                cameraRot.transform.position = transform.position + cameraOffset;
            }
            cameraRot.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        else
        {
            Debug.Log("playerCamera is null on this controller for local client");
        }

        if (gunBob != null && !isSliding)
        {
            gunBob.pause = rb.linearVelocity.sqrMagnitude <= 0.01f || !isGrounded;
        }
    }

    bool CanStartSlide()
    {
        if (isGrounded) return true;

        // In the air: your slide-jump momentum is what qualifies you, not the sprint flag
        Vector3 hv = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        return hv.magnitude >= minAirSlideSpeed;
    }

    float GetSlideSlope()
    {
        if (!isGrounded) return 0f;
        Vector3 along = Vector3.ProjectOnPlane(slideDir, groundNormal).normalized;
        return -along.y;
    }

    void StartSlide()
    {
        Debug.Log("Starting slide");
        Vector3 hv = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (hv.sqrMagnitude < 0.01f) return;
        if(gunBob)
        {
            gunBob.pause = true;
        }
        
        slideDir = hv.normalized;
        slideSpeed = isGrounded ? hv.magnitude * slideBoost : hv.magnitude;
        isSprinting = false;
        isSliding = true;


        Vector3 scale = playerObject.transform.localScale;
        playerObject.transform.localScale = new Vector3(scale.x, 0.5f, scale.z);
        // shrink the collider, keeping the feet where they are
        float targetHeight = Mathf.Max(slideHeight, col.radius * 2f);
        col.height = targetHeight;
        col.center = new Vector3(standCenter.x,
                                 standCenter.y - (standHeight - targetHeight) * 0.5f,
                                 standCenter.z);

        gunManager.SetUseAmmo(false);
        gunManager.GetAnimator()?.SetBool("Sprint", false);
    }

    void StopSlide()
    {
        Debug.Log("Stopping slide");
        isSliding = false;
        if(gunBob)
        {
            gunBob.pause = false;
        }
        
        col.height = standHeight;
        col.center = standCenter;
        Vector3 scale = playerObject.transform.localScale;
        playerObject.transform.localScale = new Vector3(scale.x, 1f, scale.z);
        gunManager.SetUseAmmo(true);
        
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (groundLockTimer > 0f) groundLockTimer -= Time.fixedDeltaTime;

        wasGrounded = isGrounded;
        CheckGrounded();
       

    
        if (!isGrounded)
        {
            // Record before the physics engine zeroes it on impact
            lastAirYVel = rb.linearVelocity.y;
        }
        else if (!wasGrounded)
        {
            float impactSpeed = -lastAirYVel;
            if (impactSpeed >= minLandSpeed)
                OnLand(impactSpeed);
            lastAirYVel = 0f;
        }

        HandleMovement();
    }

    bool HasCeiling()
    {
        Vector3 top = transform.position + col.center + Vector3.up * (col.height * 0.5f);
        return Physics.Raycast(top, Vector3.up, ceilingCheckDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    private void OnLand(float impactSpeed)
    {
        Landed?.Invoke(impactSpeed);
        previousWallNormal = Vector3.zero;

        bool landedOnFlat = Vector3.Angle(groundNormal, Vector3.up) <= flatLandAngle;

        if (isSliding && landedOnFlat && impactSpeed >= minSlideLandSpeed)
        {
            float minSlide = HasCeiling() ? moveSpeed * 0.5f : 0f;
            slideSpeed = Mathf.Max(slideSpeed - impactSpeed * slideLandingLoss, minSlide);
        }
    }

    private void HandleSlideSteering()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 inputDir =
            playerObject.right * h +
            playerObject.forward * v;

        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        if (inputDir.sqrMagnitude < 0.01f)
            return;

        // Follow the slope when grounded
        Vector3 desiredDir;

        if (isGrounded)
        {
            desiredDir = Vector3.ProjectOnPlane(
                inputDir,
                groundNormal
            ).normalized;
        }
        else
        {
            desiredDir = inputDir.normalized;
        }

        // How sharply are we trying to turn?
        float angle = Vector3.Angle(
            slideDir,
            desiredDir
        );

        // 0° = straight ahead
        // 90° = sharp turn
        // 180° = complete reversal
        float turnAmount = angle / 180f;

        // Lose more speed when turning harder.
        float turnLoss = turnAmount *
                         slideTurnLoss *
                         Time.fixedDeltaTime;

        slideSpeed = Mathf.Max(
            slideSpeed - turnLoss,
            moveSpeed * minTurnMultiplier
        );

        // Ground = strong control
        // Air = weak control
        float control = isGrounded
            ? slideControl
            : airSlideControl;

        slideDir = Vector3.RotateTowards(
            slideDir,
            desiredDir,
            control * Time.fixedDeltaTime,
            0f
        ).normalized;
    }


    private void HandleMovement()
    {

        if (isSliding)
        {
            float minSlide = HasCeiling()
                ? moveSpeed * 0.5f
                : 0f;

            // -----------------------------
            // SLOPE ACCELERATION
            // -----------------------------

            float slopeGain = GetSlideSlope() * slopeAcceleration;

            if (isGrounded)
            {
                float accel = slopeGain - slideDeceleration;

                slideSpeed += accel * Time.fixedDeltaTime;

                // Only cap speed when gaining speed.
                if (accel > 0f)
                {
                    slideSpeed = Mathf.Min(
                        slideSpeed,
                        maxSlideSpeed
                    );
                }

                slideSpeed = Mathf.Max(
                    slideSpeed,
                    minSlide
                );
            }
            

            if (!isGrounded && !HasCeiling())
            {
                // Cast along the slide direction; if a wall is right in front, end the slide
                if (CheckWall(slideDir, out RaycastHit wallHit) &&
                    Vector3.Angle(wallHit.normal, Vector3.up) > maxSlopeAngle)
                {
                    StopSlide();
                    return;
                }
            }

            // -----------------------------
            // STEERING
            // -----------------------------

            HandleSlideSteering();

            // -----------------------------
            // STOP CONDITIONS
            // -----------------------------

            bool released = !Input.GetKey(KeyCode.C);

            bool gainingSpeed =
                isGrounded &&
                slopeGain > slideDeceleration;

            bool slowedDown =
                isGrounded &&
                slideSpeed <= moveSpeed &&
                !gainingSpeed;

            if ((slowedDown || released) && !HasCeiling())
            {
                StopSlide();
                return;
            }

            // -----------------------------
            // APPLY VELOCITY
            // -----------------------------
            
            gunManager.CanFire(true);
            gunManager.CanAim(true);

            Vector3 velocity;

            if (isGrounded)
            {
                Vector3 slopeDirection =
                    Vector3.ProjectOnPlane(
                        slideDir,
                        groundNormal
                    ).normalized;

                velocity = slopeDirection * slideSpeed;
            }
            else
            {
                velocity = new Vector3(
                    slideDir.x * slideSpeed,
                    rb.linearVelocity.y,
                    slideDir.z * slideSpeed
                );
            }

            rb.linearVelocity = velocity;

            return;
        }


        gunManager.CanFire(!isSprinting); // Disable firing while sprinting
        gunManager.CanAim(!isSprinting); // Disable aiming while sprinting
        if(gunManager.GetGun() != null)
        {
            gunManager.GetAnimator()?.SetBool("Sprint", isSprinting);
        }
        
        
        if(!isSprinting)
        {
            sprMultiplier = 1f;
            if(gunBob)
            {
                gunBob.SetSprinting(false);
            }
            
        }
        else
        {
            sprMultiplier = sprintMultiplier;
            if(gunBob)
            {
                gunBob.SetSprinting(true);
            }
            
        }
    
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = (playerObject.right * h + playerObject.forward * v).normalized;

        if (isGrounded)
        {
            move = Vector3.ProjectOnPlane(move, groundNormal).normalized;

            Vector3 targetVel = move * moveSpeed * sprMultiplier;

            rb.linearVelocity = targetVel;
        }
        else
        {
            Vector3 currentVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 newVel = currentVel;

            if (move.sqrMagnitude > 0.01f)
            {
                float maxSpeed = Mathf.Max(currentVel.magnitude, moveSpeed);
                newVel = Vector3.ClampMagnitude(
                    currentVel + move * airAcceleration * Time.fixedDeltaTime,
                    maxSpeed
                );
            }

            rb.linearVelocity = new Vector3(
                newVel.x,
                rb.linearVelocity.y,
                newVel.z
            );
        }

    }

    private bool CheckWall(Vector3 direction, out RaycastHit hit)
    {
        float radius = col.radius - skinWidth;

        Vector3 center = transform.TransformPoint(col.center);

        float halfHeight = Mathf.Max(
            radius,
            col.height * 0.5f - col.radius
        );

        Vector3 top = center + Vector3.up * halfHeight;
        Vector3 bottom = center - Vector3.up * halfHeight;

        return Physics.CapsuleCast(
            top,
            bottom,
            radius,
            direction,
            out hit,
            wallCheckDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private bool CheckForWall(out RaycastHit wallHit)
    {
        Vector3[] directions =
        {
        playerObject.right,
        -playerObject.right,

        // (playerObject.forward + playerObject.right).normalized,
        // (playerObject.forward - playerObject.right).normalized,
        // (-playerObject.forward + playerObject.right).normalized,
        // (-playerObject.forward - playerObject.right).normalized
    };

        foreach (Vector3 direction in directions)
        {
            if (CheckWall(direction, out RaycastHit hit))
            {
                wallHit = hit;
                return true;
            }
        }

        wallHit = default;
        return false;
    }




    private void CheckGrounded()
    {
        float castRadius = Mathf.Max(0.01f, capsuleRadius - skinWidth);
        float halfHeight = Mathf.Max(castRadius, col.height * 0.5f - capsuleRadius);

        Vector3 bottom = transform.position + col.center - Vector3.up * (halfHeight - skinWidth);
        Vector3 top = transform.position + col.center + Vector3.up * halfHeight;

        isGrounded = false;
        groundNormal = Vector3.up;

        RaycastHit[] hits = Physics.CapsuleCastAll(
            top,
            bottom,
            castRadius,
            Vector3.down,
            groundCheckDist + skinWidth
        );

        float closestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            if (hit.collider == col || hit.collider.transform.IsChildOf(transform))
                continue;

            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
                continue;

            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                isGrounded = true;
                groundNormal = hit.normal;
            }
        }

        if (groundLockTimer > 0f)
        {
            isGrounded = false;
            groundNormal = Vector3.up;
        }
    }
}