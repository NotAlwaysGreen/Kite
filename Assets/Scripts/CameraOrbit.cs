using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbit : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform target;                  // Player
    public Vector3 offset = new Vector3(0, 5, -12);

    [Header("Rotation Settings")]
    public float mouseSensitivity = 0.2f;
    public float smoothSpeed = 5f;            // how fast camera follows
    public float minPitch = -30f;
    public float maxPitch = 80f;

    private float yaw = 0f;
    private float pitch = 20f;
    private Vector3 currentVelocity;          // for SmoothDamp

    void LateUpdate()
    {
        if (target == null || Mouse.current == null) return;

        // Mouse input
        Vector2 delta = Mouse.current.delta.ReadValue();
        yaw += delta.x * mouseSensitivity;
        pitch -= delta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Desired camera position
        Vector3 desiredPosition = target.position + rotation * offset;

        // Smoothly ease camera to target position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / smoothSpeed);

        // Look at player
        transform.LookAt(target.position);
    }
}