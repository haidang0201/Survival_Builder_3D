using UnityEngine;

public class Tree : MonoBehaviour
{
    public int health = 3;

    [Header("Drop Settings")]
    public ObjectPool woodPool;
    public int dropAmount = 3;

    private bool isOccupied = false;

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

    // 🔥 TRẢ VỀ GỖ
    public WoodPickup[] TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            return DestroyTree();
        }

        return null;
    }

    // 🔥 TRẢ DANH SÁCH GỖ
    WoodPickup[] DestroyTree()
    {
        Debug.Log("Cây bị đốn!");

        WoodPickup[] woods = DropWood();

        isOccupied = false;
        gameObject.SetActive(false);

        return woods;
    }

    // 🔥 DROP GỖ
    WoodPickup[] DropWood()
    {
        if (woodPool == null) return null;

        WoodPickup[] woods = new WoodPickup[dropAmount];

        for (int i = 0; i < dropAmount; i++)
        {
            GameObject woodObj = woodPool.GetObject();

            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-1f, 1f),
                0.5f,
                Random.Range(-1f, 1f)
            );

            woodObj.transform.position = randomPos;

            Rigidbody rb = woodObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                rb.AddForce(
                    Vector3.up * 3f + Random.insideUnitSphere,
                    ForceMode.Impulse
                );
            }

            woods[i] = woodObj.GetComponent<WoodPickup>();
        }

        return woods;
    }
}