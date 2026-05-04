using UnityEngine;

public class WoodPickup : MonoBehaviour
{
    public ObjectPool pool;

    private Rigidbody rb;
    private Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Pickup(Transform handPoint)
    {
        Debug.Log("Cầm gỗ!");

        if (rb != null)
        {
            // 🔥 CHỈ reset velocity khi KHÔNG phải kinematic
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 🔥 sau đó mới set kinematic
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

            // 🔥 lúc này mới được set velocity
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (col != null)
        {
            col.enabled = true;
        }
    }
}