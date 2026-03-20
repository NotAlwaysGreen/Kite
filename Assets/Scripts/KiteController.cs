using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KiteController : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Distance")]
    public float maxDistance = 6f;

    [Header("Pull & Lift")]
    public float pullStrength = 25f;       // base strength of elastic pull
    public float liftMultiplier = 30f;
    public float liftExponent = 2.0f;
    public float fallSpeed = 5f;

    [Header("Height & Ground")]
    public float maxHeight = 12f;
    public float groundHeight = 0.5f;      // pivot stays above ground

    [Header("Movement")]
    public float airDrag = 2f;

    [Header("Rotation")]
    public float rotationSmooth = 5f;
    public float tiltAmount = 25f;
    public float yawSmooth = 3f;

    [Header("Smoothing & Limits")]
    public float maxPullForce = 15f;        // cap for pull force
    public float accelerationSmooth = 5f;   // lerp speed for pull force

    private Rigidbody rb;
    private Quaternion flatRotation;        // kite lying flat on ground
    private Quaternion currentYaw;          // yaw persists while falling
    private float currentPull = 0f;         // smooth pull force

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 0.2f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 1f;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // baseline rotation: flat on ground, nose forward
        flatRotation = Quaternion.Euler(-90f, transform.eulerAngles.y + 180f, 0f);
        transform.rotation = flatRotation;

        currentYaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        float tension = 0f;

        // -----------------------------
        // ELASTIC ROPE PULL (smoothed)
        // -----------------------------
        if (distance > maxDistance)
        {
            float excess = distance - maxDistance;
            float normalized = excess / maxDistance;

            // exponential pull based on distance
            float targetForce = Mathf.Pow(normalized, 1.8f) * pullStrength;

            // cap pull force
            targetForce = Mathf.Min(targetForce, maxPullForce);

            // smooth transition of force
            currentPull = Mathf.Lerp(currentPull, targetForce, accelerationSmooth * Time.fixedDeltaTime);

            // apply pull
            rb.AddForce(toPlayer.normalized * currentPull, ForceMode.Acceleration);

            tension = currentPull / pullStrength; // scaled tension for lift
        }
        else
        {
            currentPull = 0f;
        }

        // -----------------------------
        // LIFT with HEIGHT FALL OFF
        // -----------------------------
        if (tension > 0.01f)
        {
            float height01 = Mathf.Clamp01(transform.position.y / maxHeight);
            float heightFactor = Mathf.Pow(1f - height01, 2f); // smoother falloff
            float liftForce = Mathf.Pow(tension, liftExponent) * liftMultiplier * heightFactor;
            rb.AddForce(Vector3.up * liftForce, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(Vector3.down * fallSpeed, ForceMode.Acceleration);
        }

        // -----------------------------
        // DRAG (horizontal smoothing)
        // -----------------------------
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        horizontalVel *= Mathf.Exp(-airDrag * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);

        // -----------------------------
        // GROUND STABILIZATION
        // -----------------------------
        if (transform.position.y < groundHeight)
        {
            Vector3 pos = rb.position;
            pos.y = groundHeight;
            rb.position = pos;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }

        HandleRotation();
    }

    void HandleRotation()
    {
        float verticalSpeed = rb.linearVelocity.y;
        bool nearGround = transform.position.y <= groundHeight + 0.01f;

        // -----------------------------
        // YAW: face pull direction only while lifted
        // -----------------------------
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

        // -----------------------------
        // PITCH: tilt based on vertical movement
        // -----------------------------
        float targetPitch = 0f;
        if (!nearGround)
        {
            float biasedSpeed = verticalSpeed + 1f;
            float normalized = Mathf.Clamp(biasedSpeed / 5f, -1f, 1f);
            targetPitch = -normalized * tiltAmount; // nose up when rising, down when falling
        }

        Quaternion yawRotation = currentYaw;
        Quaternion pitchRotation = Quaternion.Euler(targetPitch, 0f, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            yawRotation * pitchRotation * flatRotation,
            rotationSmooth * Time.deltaTime
        );
    }
}