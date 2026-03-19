using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KiteController : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Distance")]
    public float maxDistance = 6f;
    public float pullStrength = 20f;

    [Header("Lift")]
    public float liftMultiplier = 30f;
    public float liftExponent = 2.5f;
    public float fallSpeed = 5f;
    public float maxHeight = 12f;
    public float groundHeight = 0.5f;

    [Header("Movement")]
    public float airDrag = 2f;

    [Header("Rotation")]
    public float rotationSmooth = 5f;
    public float tiltAmount = 25f;
    public float yawSmooth = 3f;

    private Rigidbody rb;
    private Quaternion flatRotation;  // baseline rotation: flat on ground (-90 X + 180 Y)
    private Quaternion currentYaw;    // persistent yaw during fall

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 0.2f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 1f;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Set flat baseline rotation: flat on ground, nose forward
        flatRotation = Quaternion.Euler(-90f, transform.eulerAngles.y + 180f, 0f);
        transform.rotation = flatRotation;

        // Initialize yaw from flat rotation
        currentYaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        float tension = 0f;

        // Pull kite only if too far
        if (distance > maxDistance)
        {
            float excess = distance - maxDistance;
            tension = Mathf.Clamp01(excess / maxDistance);
            rb.AddForce(toPlayer.normalized * pullStrength * tension, ForceMode.Acceleration);
        }

        // Lift based on tension (exponential)
        if (tension > 0.01f)
        {
            float liftForce = Mathf.Pow(tension, liftExponent) * liftMultiplier;
            rb.AddForce(Vector3.up * liftForce, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(Vector3.down * fallSpeed, ForceMode.Acceleration);
        }

        // Horizontal drag
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z) * Mathf.Exp(-airDrag * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);

        // Clamp height
        if (transform.position.y > maxHeight)
            transform.position = new Vector3(transform.position.x, maxHeight, transform.position.z);
        if (transform.position.y < groundHeight)
            transform.position = new Vector3(transform.position.x, groundHeight, transform.position.z);

        // Handle rotation
        HandleRotation();
    }

    void HandleRotation()
    {
        float height = transform.position.y;
        float verticalSpeed = rb.linearVelocity.y;
        bool nearGround = height <= groundHeight + 0.3f;

        // Smooth yaw toward player only when lifted
        if (!nearGround && (verticalSpeed + 1f) > 0.05f)
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion desiredYaw = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                currentYaw = Quaternion.Slerp(currentYaw, desiredYaw, yawSmooth * Time.deltaTime);
            }
        }

        // Pitch logic along kite's local X
        float targetPitch = 0f;
        if (!nearGround)
        {
            float biasedSpeed = verticalSpeed + 1f;
            float normalized = Mathf.Clamp(biasedSpeed / 5f, -1f, 1f);

            // Negative = nose up, positive = nose down
            targetPitch = -normalized * tiltAmount;
        }

        // Combine: yaw + pitch + flat baseline
        Quaternion yawRotation = currentYaw;                   // world Y rotation
        Quaternion pitchRotation = Quaternion.Euler(targetPitch, 0f, 0f); // local X tilt
        transform.rotation = Quaternion.Slerp(transform.rotation, yawRotation * pitchRotation * flatRotation, rotationSmooth * Time.deltaTime);
    }
}