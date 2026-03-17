using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KiteController : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Lift Settings")]
    public float liftMultiplier = 2f;    // how much horizontal speed generates lift
    public float maxHeight = 12f;        // maximum kite height
    public float minHeight = 0.5f;       // kite stays on ground when idle
    public float horizontalDrag = 3f;    // slows kite forward velocity

    private Rigidbody rb;
    private SpringJoint spring;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.mass = 0.2f;           // light kite
        rb.linearDamping = 0.5f;           // gentle air resistance
        rb.angularDamping = 0.5f;    // prevents spinning
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = true;

        spring = GetComponent<SpringJoint>();
        if (spring == null)
        {
            Debug.LogWarning("KiteController requires a SpringJoint attached to the player!");
        }
    }

    void FixedUpdate()
    {
        if (player == null || spring == null) return;

        // Calculate horizontal speed of kite (ignore vertical)
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float speed = horizontalVelocity.magnitude;

        // Apply upward lift proportional to horizontal speed
        if (speed > 0.1f)
        {
            Vector3 lift = Vector3.up * speed * liftMultiplier * Time.fixedDeltaTime;
            rb.AddForce(lift, ForceMode.VelocityChange);
        }

        // Dampen horizontal velocity so kite doesn't shoot past player
        Vector3 dampedVelocity = horizontalVelocity * Mathf.Exp(-horizontalDrag * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(dampedVelocity.x, rb.linearVelocity.y, dampedVelocity.z);

        // Clamp kite height
        if (rb.position.y > maxHeight)
        {
            rb.position = new Vector3(rb.position.x, maxHeight, rb.position.z);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, 0), rb.linearVelocity.z);
        }

        if (rb.position.y < minHeight)
        {
            rb.position = new Vector3(rb.position.x, minHeight, rb.position.z);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, 0), rb.linearVelocity.z);
        }
    }
}