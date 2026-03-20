using UnityEngine;
using UnityEngine.InputSystem; // Needed for new Input System

[RequireComponent(typeof(Rigidbody))]
public class KiteController : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Distance")]
    public float maxDistance = 6f;
    public float pullStrength = 25f;

    [Header("Lift")]
    public float liftMultiplier = 30f;
    public float liftExponent = 2.0f;
    public float fallSpeed = 5f;
    public float maxHeight = 12f;

    [Header("Ground")]
    public float groundHeight = 0.5f;

    [Header("Movement")]
    public float airDrag = 2f;

    [Header("Rotation")]
    public float rotationSmooth = 5f;
    public float tiltAmount = 25f;
    public float yawSmooth = 3f;

    [Header("Rope Length")]
    [Range(0.5f, 2f)] public float ropeLength = 1f; // Multiplier for distance and height
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
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Flat baseline (kite lying on ground correctly)
        flatRotation = Quaternion.Euler(-90f, transform.eulerAngles.y + 180f, 0f);
        transform.rotation = flatRotation;

        currentYaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    void Update()
    {
        // -----------------------------
        // Adjust rope length with mouse wheel (new Input System)
        // -----------------------------
        if (Mouse.current != null)
        {
            Vector2 scroll = Mouse.current.scroll.ReadValue();
            if (scroll.y != 0f)
            {
                ropeLength += scroll.y * 0.05f; // 0.05 increments
                ropeLength = Mathf.Clamp(ropeLength, 0.5f, 2f);
            }
        }

        // Display rope length
        ropeLengthDisplay = "Rope Length: " + ropeLength.ToString("F2");
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // Adjusted distance and height by rope length multiplier
        float adjustedMaxDistance = maxDistance * ropeLength;
        float adjustedMaxHeight = maxHeight * ropeLength;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        float tension = 0f;

        // -----------------------------
        // ELASTIC ROPE (one-sided)
        // -----------------------------
        if (distance > adjustedMaxDistance)
        {
            float excess = distance - adjustedMaxDistance;

            float normalized = excess / adjustedMaxDistance;

            // exponential pull (stronger when far)
            float strength = Mathf.Pow(normalized, 1.8f);

            Vector3 dir = toPlayer.normalized;

            rb.AddForce(dir * pullStrength * strength, ForceMode.Acceleration);

            tension = strength;
        }

        // -----------------------------
        // LIFT with HEIGHT FALLOFF
        // -----------------------------
        if (tension > 0.01f)
        {
            float height = transform.position.y;
            float height01 = Mathf.Clamp01(height / adjustedMaxHeight);

            // reduce lift near max height
            float heightFactor = 1f - height01;
            heightFactor = Mathf.Pow(heightFactor, 2f);

            float liftForce = Mathf.Pow(tension, liftExponent) * liftMultiplier * heightFactor;

            rb.AddForce(Vector3.up * liftForce, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(Vector3.down * fallSpeed, ForceMode.Acceleration);
        }

        // -----------------------------
        // DRAG (creates smooth easing)
        // -----------------------------
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
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
        // YAW (face pull direction)
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
        // PITCH (tilt based on vertical movement)
        // -----------------------------
        float targetPitch = 0f;

        if (!nearGround)
        {
            float biasedSpeed = verticalSpeed + 1f;
            float normalized = Mathf.Clamp(biasedSpeed / 5f, -1f, 1f);
            targetPitch = -normalized * tiltAmount; // negative = nose up
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