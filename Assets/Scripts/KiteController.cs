using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class KiteController : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Distance")]
    public float maxDistance = 6f;
    public float pullStrength = 25f;

    [Header("Tension Smoothing")]
    public float tensionRiseSpeed = 4f;   // how fast tension builds
    public float tensionFallSpeed = 1.2f; // how slowly it fades
    private float smoothedTension = 0f;

    [Header("Lift")]
    public float baseLift = 3f;
    public float liftMultiplier = 30f;
    public float liftExponent = 2.0f;
    public float maxHeight = 12f;

    [Header("Gravity")]
    public float customGravity = 4f;

    [Header("Vertical Control")]
    public float verticalDamping = 0.97f;
    public float maxFallSpeed = -3f;

    [Header("Ground")]
    public float groundHeight = 0.5f;

    [Header("Movement")]
    public float airDrag = 2f;

    [Header("Rotation")]
    public float rotationSmooth = 5f;
    public float tiltAmount = 25f;
    public float yawSmooth = 3f;

    [Header("Rope Length")]
    [Range(0.5f, 2f)] public float ropeLength = 1f;
    private string ropeLengthDisplay = "";

    private Rigidbody rb;
    private Quaternion flatRotation;
    private Quaternion currentYaw;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.mass = 0.2f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 1f;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        flatRotation = Quaternion.Euler(-90f, transform.eulerAngles.y + 180f, 0f);
        transform.rotation = flatRotation;

        currentYaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    void Update()
    {
        if (Mouse.current != null)
        {
            Vector2 scroll = Mouse.current.scroll.ReadValue();
            if (scroll.y != 0f)
            {
                ropeLength += scroll.y * 0.05f;
                ropeLength = Mathf.Clamp(ropeLength, 0.5f, 2f);
            }
        }

        ropeLengthDisplay = "Rope Length: " + ropeLength.ToString("F2");
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float adjustedMaxDistance = maxDistance * ropeLength;
        float adjustedMaxHeight = maxHeight * ropeLength;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        // -----------------------------
        // RAW TENSION
        // -----------------------------
        float rawTension = 0f;

        if (distance > adjustedMaxDistance)
        {
            float excess = distance - adjustedMaxDistance;
            float normalized = excess / adjustedMaxDistance;

            float strength = Mathf.Pow(normalized, 1.8f);
            Vector3 dir = toPlayer.normalized;

            rb.AddForce(dir * pullStrength * strength, ForceMode.Acceleration);

            rawTension = strength;
        }

        // -----------------------------
        // SMOOTHED TENSION
        // -----------------------------
        float speed = (rawTension > smoothedTension) ? tensionRiseSpeed : tensionFallSpeed;

        smoothedTension = Mathf.MoveTowards(
            smoothedTension,
            rawTension,
            speed * Time.fixedDeltaTime
        );

        // -----------------------------
        // HEIGHT FACTOR
        // -----------------------------
        float height = transform.position.y;
        float height01 = Mathf.Clamp01(height / adjustedMaxHeight);
        float heightFactor = Mathf.Pow(1f - height01, 1.2f);

        // -----------------------------
        // LIFT
        // -----------------------------
        float tensionLift = Mathf.Pow(smoothedTension, liftExponent) * liftMultiplier;
        float liftForce = (baseLift + tensionLift) * heightFactor;

        rb.AddForce(Vector3.up * liftForce, ForceMode.Acceleration);

        // -----------------------------
        // CUSTOM GRAVITY
        // -----------------------------
        rb.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);

        // -----------------------------
        // HORIZONTAL DRAG
        // -----------------------------
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        horizontalVel *= Mathf.Exp(-airDrag * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);

        // -----------------------------
        // VERTICAL CONTROL
        // -----------------------------
        if (rb.linearVelocity.y < maxFallSpeed)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                maxFallSpeed,
                rb.linearVelocity.z
            );
        }

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            rb.linearVelocity.y * verticalDamping,
            rb.linearVelocity.z
        );

        // -----------------------------
        // GROUND CLAMP
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

        float targetPitch = 0f;

        if (!nearGround)
        {
            float biasedSpeed = verticalSpeed + 1f;
            float normalized = Mathf.Clamp(biasedSpeed / 5f, -1f, 1f);
            targetPitch = -normalized * tiltAmount;
        }

        Quaternion yawRotation = currentYaw;
        Quaternion pitchRotation = Quaternion.Euler(targetPitch, 0f, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            yawRotation * pitchRotation * flatRotation,
            rotationSmooth * Time.deltaTime
        );
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 20), ropeLengthDisplay);
    }
}