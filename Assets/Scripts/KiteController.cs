using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KiteController : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Distance")]
    public float maxDistance = 6f;     // rope length before tension
    public float pullStrength = 20f;   // how hard it pulls back

    [Header("Lift")]
    public float liftMultiplier = 30f;
    public float liftExponent = 2.5f;
    public float fallSpeed = 5f;
    public float maxHeight = 12f;
    public float groundHeight = 0.5f;

    [Header("Movement")]
    public float airDrag = 2f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.mass = 0.2f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 1f;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        float tension = 0f;

        // 🪢 1. ONLY pull if too far → creates tension
        if (distance > maxDistance)
        {
            float excess = distance - maxDistance;

            // normalize tension (0 → 1)
            tension = Mathf.Clamp01(excess / maxDistance);

            Vector3 pullDir = toPlayer.normalized;

            rb.AddForce(pullDir * pullStrength * tension, ForceMode.Acceleration);
        }

        // 🌬 2. EXPONENTIAL LIFT BASED ON TENSION
        if (tension > 0.01f)
        {
            float curved = Mathf.Pow(tension, liftExponent);
            float liftForce = curved * liftMultiplier;

            rb.AddForce(Vector3.up * liftForce, ForceMode.Acceleration);
        }
        else
        {
            // 🍂 No tension → fall
            rb.AddForce(Vector3.down * fallSpeed, ForceMode.Acceleration);
        }

        // 🧊 3. Horizontal drag
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        horizontalVel *= Mathf.Exp(-airDrag * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);

        // 🧱 4. Clamp height
        if (transform.position.y > maxHeight)
        {
            transform.position = new Vector3(transform.position.x, maxHeight, transform.position.z);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }

        if (transform.position.y < groundHeight)
        {
            transform.position = new Vector3(transform.position.x, groundHeight, transform.position.z);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        }
    }
}