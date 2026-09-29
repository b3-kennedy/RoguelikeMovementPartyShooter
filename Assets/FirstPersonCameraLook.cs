using UnityEngine;

public class FirstPersonCameraLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform followTarget; // empty at eye height on the player
    [SerializeField] private Transform playerBody;    // used only to seed initial yaw

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    public float Yaw { get; private set; }
    private float pitch;

    private void Start()
    {
        Yaw = playerBody.eulerAngles.y;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        Yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.position = followTarget.position;
        transform.rotation = Quaternion.Euler(pitch, Yaw, 0f);
    }

    // private void LateUpdate()
    // {
    //     // LateUpdate runs after the rigidbody's interpolated transform has been
    //     // updated for the frame, so following here (instead of via parenting)
    //     // eliminates the jitter.
    //     transform.position = followTarget.position;
    //     transform.rotation = Quaternion.Euler(pitch, Yaw, 0f);
    // }
}