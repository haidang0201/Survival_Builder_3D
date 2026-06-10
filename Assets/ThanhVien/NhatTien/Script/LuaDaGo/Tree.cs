using UnityEngine;

public class Tree : MonoBehaviour
{
    [Header("Tree Settings")]
    public int maxHealth = 3;

    [Header("Drop Settings")]
    public ObjectPool woodPool;
    public int dropAmount = 3;

    private int currentHealth;
    private bool isOccupied = false;
    private bool isFalling  = false; // Chặn TakeDamage kép trong lúc animation đổ cây
    private TreeVisual treeVisual;

    void Awake()
    {
        treeVisual = GetComponent<TreeVisual>();
        if (treeVisual == null) Debug.LogWarning($"[Tree] '{name}' không có TreeVisual.");
    }

    void OnEnable()
    {
        currentHealth = maxHealth;
        isOccupied    = false;
        isFalling     = false;
        WorkerFindTree.Registry.Add(this);
    }

    void OnDisable()
    {
        WorkerFindTree.Registry.Remove(this);
    }

    public bool TryClaim()
    {
        if (isOccupied) return false;
        isOccupied = true;
        return true;
    }

    public void Release() => isOccupied = false;

    public WoodPickup[] TakeDamage(int damage)
    {
        if (isFalling) return null; // Đang đổ rồi, bỏ qua

        currentHealth -= damage;
        Debug.Log($"[Tree] '{name}' nhận {damage} damage. HP còn lại: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) return DestroyTree();
        treeVisual?.PlayShake();
        return null;
    }

    WoodPickup[] DestroyTree()
    {
        isFalling  = true;  // Khóa, không nhận damage nữa
        isOccupied = false; // Nhả claim ngay để worker khác không chờ vô ích

        WoodPickup[] woods = DropWood();

        if (treeVisual != null)
        {
            treeVisual.PlayFall(onFallComplete: () => {
                Debug.Log($"[Tree] '{name}' tắt sau khi đổ xong.");
                gameObject.SetActive(false);
            });
        }
        else gameObject.SetActive(false);

        return woods;
    }

    WoodPickup[] DropWood()
    {
        if (woodPool == null)
        {
            Debug.LogWarning($"[Tree] '{name}' không có woodPool — không rơi gỗ.");
            return null;
        }

        WoodPickup[] woods = new WoodPickup[dropAmount];
        for (int i = 0; i < dropAmount; i++)
        {
            GameObject obj = woodPool.GetObject();
            Vector3 dropPos = transform.position + new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
            obj.transform.position = dropPos;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic     = false;
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(Vector3.up * 3f + Random.insideUnitSphere, ForceMode.Impulse);
            }
            woods[i] = obj.GetComponent<WoodPickup>();
        }
        Debug.Log($"[Tree] '{name}' rơi {dropAmount} gỗ.");
        return woods;
    }
}