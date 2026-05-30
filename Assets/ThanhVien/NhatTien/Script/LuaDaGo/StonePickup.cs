using UnityEngine;

public class StonePickup : MonoBehaviour
{
    public ObjectPool pool;

    private Rigidbody rb;
    private Collider  col;
    private bool      isTaken = false;

    void Awake()
    {
        rb  = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    void OnEnable()
    {
        isTaken = false;
        transform.SetParent(null);
        ResetPhysics(kinematic: false, collisions: true);
    }

    public bool IsTaken()   => isTaken;
    public void MarkTaken() => isTaken = true;

    public void Pickup(Transform handPoint)
    {
        if (handPoint == null) return;
        ResetPhysics(kinematic: true, collisions: false);
        transform.SetParent(handPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        transform.SetParent(null);
        ResetPhysics(kinematic: false, collisions: true);
    }

    void ResetPhysics(bool kinematic, bool collisions)
    {
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic      = kinematic;
            rb.detectCollisions = collisions;
        }
        if (col != null) col.enabled = collisions;
    }
}