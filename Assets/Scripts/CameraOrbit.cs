using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbit : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // drag your Player here

    [Header("Distance & Height")]
    public float distance = 10f;
    public float height = 4f;

    [Header("Rotation")]
    public float mouseSensitivity = 0.2f;
    public float minPitch = -20f;
    public float maxPitch = 60f;

    [Header("Smoothing")]
    public float smoothTime = 0.1f;

    private float yaw = 0f;
    private float pitch = 20f;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target == null || Mouse.current == null) return;

        // 🖱 Mouse input (new Input System)
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * mouseSensitivity;
        pitch -= mouseDelta.y * mouseSensitivity;

        // 🎯 Clamp vertical rotation (prevents flipping)
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 🎯 Rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // 🎯 Desired position (behind player)
        Vector3 offset = new Vector3(0, height, -distance);
        Vector3 desiredPosition = target.position + rotation * offset;

        // 🎯 Smooth follow (prevents jitter)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            smoothTime
        );

        // 🎯 Always look at player
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}