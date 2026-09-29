using Unity.Netcode;
using UnityEngine;

public class GunSway : MonoBehaviour
{
    [SerializeField] private float aimMultiplier = 0.01f;

    [Header("Sway")]
    [SerializeField] private float swayAmount = 0.1f;
    [SerializeField] private float swaySmoothSpeed = 8f;
    [SerializeField] private float maxSway = 0.5f;
    [SerializeField] private float dampenSpeed = 5f;

    private Vector3 basePosition;
    private Vector3 targetPosition;

    private Quaternion previousCameraRotation;

    private float swayMultiplier = 1f;

    Gun gun;

    Transform rotationObject;

    NetworkObject networkObject;


    private void Start()
    {
        basePosition = transform.localPosition;
        rotationObject = transform.root;
        gun = GetComponentInParent<Gun>();
        networkObject = GetComponentInParent<NetworkObject>();
        if (transform.parent != null)
        {
            previousCameraRotation = rotationObject.localRotation;
        }
    }


    private void Update()
    {
        if (!networkObject.IsOwner) return;
        float targetMultiplier = gun.GetManager().isAiming() ? aimMultiplier : 1f;
        swayMultiplier = Mathf.Lerp(swayMultiplier, targetMultiplier, Time.deltaTime * dampenSpeed);

        UpdateSway();

    }


    private void UpdateSway()
    {
        if (transform.parent == null)
            return;

        Quaternion currentCameraRotation =
            rotationObject.localRotation;

        // Find how much the camera rotated since last frame.
        Quaternion rotationDifference =
            currentCameraRotation *
            Quaternion.Inverse(previousCameraRotation);

        rotationDifference.ToAngleAxis(
            out float angle,
            out Vector3 axis
        );

        // Convert angles greater than 180 into negative angles.
        if (angle > 180f)
        {
            angle -= 360f;
        }

        Vector3 rotationDelta = axis * angle;

        // Horizontal camera movement.
        float horizontalSway =
            -rotationDelta.y * swayAmount * swayMultiplier;

        // Vertical camera movement.
        float verticalSway =
            rotationDelta.x * swayAmount * swayMultiplier;

        targetPosition = basePosition + new Vector3(
            horizontalSway,
            verticalSway,
            0f
        );

        // Limit the maximum amount of sway.
        targetPosition.x = Mathf.Clamp(
            targetPosition.x,
            basePosition.x - maxSway,
            basePosition.x + maxSway
        );

        targetPosition.y = Mathf.Clamp(
            targetPosition.y,
            basePosition.y - maxSway,
            basePosition.y + maxSway
        );

        // Smoothly catch up to the target.
        float smooth =
            1f - Mathf.Exp(-swaySmoothSpeed * Time.deltaTime);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            smooth
        );

        previousCameraRotation = currentCameraRotation;
    }
}