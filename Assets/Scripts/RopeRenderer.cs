using UnityEngine;

public class RopeRenderer : MonoBehaviour
{
    public Transform playerHand;
    public Transform kite;

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
    }

    void Update()
    {
        line.SetPosition(0, playerHand.position);
        line.SetPosition(1, kite.position);
    }
}