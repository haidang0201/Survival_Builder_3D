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

    void OnEnable()
    {
        isTaken = false;
        transform.SetParent(null);
        ResetPhysics(kinematic: false, collisions: true);
    }

    public bool IsTaken() => isTaken;
    public void MarkTaken() => isTaken = true;

    public void Pickup(Transform handPoint)
    {
        if (handPoint == null) return;

        ResetPhysics(kinematic: true, collisions: false);

        transform.SetParent(handPoint);
        transform.localPosition = Vector3.zero;
        
        // THAY ĐỔI: Đổi Quaternion.identity thành góc xoay Y = 90 độ tại đây
        transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
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
            // Phải reset velocity TRƯỚC khi set isKinematic = true
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = kinematic;
            col.enabled = collisions;
        }
    }
}