using UnityEngine;

public class Tree : MonoBehaviour
{
    public int health = 3;

    [Header("Drop Settings")]
    public ObjectPool woodPool;
    public int dropAmount = 3;

    private bool isOccupied = false;

    void OnEnable()
    {
        health = 3;
        isOccupied = false;
    }

    public bool TryClaim()
    {
        if (isOccupied) return false;
        isOccupied = true;
        return true;
    }

    public void Release()
    {
        isOccupied = false;
    }

    // Trả về mảng gỗ nếu cây chết, null nếu cây còn sống
    public WoodPickup[] TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            return DestroyTree();
        }

        return null;
    }

    WoodPickup[] DestroyTree()
    {
        WoodPickup[] woods = DropWood();
        isOccupied = false;
        gameObject.SetActive(false);
        return woods;
    }

    WoodPickup[] DropWood()
    {
        if (woodPool == null) return null;

        WoodPickup[] woods = new WoodPickup[dropAmount];

        for (int i = 0; i < dropAmount; i++)
        {
            GameObject obj = woodPool.GetObject();

            Vector3 dropPos = transform.position + new Vector3(
                Random.Range(-1f, 1f),
                0.5f,
                Random.Range(-1f, 1f)
            );

            obj.transform.position = dropPos;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(Vector3.up * 3f + Random.insideUnitSphere, ForceMode.Impulse);
            }

            woods[i] = obj.GetComponent<WoodPickup>();
        }

        return woods;
    }
}