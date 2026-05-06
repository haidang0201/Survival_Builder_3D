using UnityEngine;

public class WoodPickup : MonoBehaviour
{
    public ObjectPool pool;

    private Rigidbody rb;
    private Collider col;

    private bool isTaken = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public bool IsTaken()
    {
        return isTaken;
    }

    public void MarkTaken()
    {
        isTaken = true;
    }

    public void Pickup(Transform handPoint)
    {
        if (handPoint == null) return;

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        transform.SetParent(handPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (col != null)
        {
            col.enabled = true;
        }
    }

    void OnEnable()
    {
        isTaken = false;

        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (col != null)
        {
            col.enabled = true;
        }
    }
}