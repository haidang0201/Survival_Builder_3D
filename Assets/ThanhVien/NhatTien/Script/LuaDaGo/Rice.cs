using UnityEngine;

public class Rice : MonoBehaviour
{
    [Header("Rice Settings")]
    public int maxHealth = 2; //[cite: 11]

    [Header("Drop Settings")]
    public ObjectPool ricePool; //[cite: 11]
    public int dropAmount = 2; //[cite: 11]

    // ===== INTERNAL =====
    private int  currentHealth; //[cite: 11]
    private bool isOccupied = false; //[cite: 11]

    void OnEnable()
    {
        currentHealth = maxHealth; //[cite: 11]
        isOccupied    = false; //[cite: 11]

        // [NÂNG CẤP]: Tự động đăng ký vào danh sách của AI khi lúa mọc / bật lên
        if (!WorkerFindRice.Registry.Contains(this))
        {
            WorkerFindRice.Registry.Add(this);
        }
    }

    // [NÂNG CẤP]: Tự động xóa khỏi danh sách AI khi bị gặt xong hoặc tắt đi
    void OnDisable()
    {
        if (WorkerFindRice.Registry.Contains(this))
        {
            WorkerFindRice.Registry.Remove(this);
        }
    }

    // ===== CLAIM / RELEASE =====

    public bool TryClaim()
    {
        if (isOccupied) return false; //[cite: 11]
        isOccupied = true; //[cite: 11]
        return true; //[cite: 11]
    }

    public void Release()
    {
        isOccupied = false; //[cite: 11]
    }

    // ===== HARVEST =====

    public RicePickup[] TakeDamage(int damage)
    {
        currentHealth -= damage; //[cite: 11]

        Debug.Log($"[Rice] '{name}' bị gặt. HP còn lại: {currentHealth}/{maxHealth}"); //[cite: 11]

        if (currentHealth <= 0) //[cite: 11]
            return HarvestRice(); //[cite: 11]

        return null; //[cite: 11]
    }

    // ===== INTERNAL =====

    RicePickup[] HarvestRice()
    {
        RicePickup[] drops = DropRice(); //[cite: 11]

        isOccupied = false; //[cite: 11]
        gameObject.SetActive(false); //[cite: 11]

        Debug.Log($"[Rice] '{name}' đã gặt xong → tắt."); //[cite: 11]

        return drops; //[cite: 11]
    }

    RicePickup[] DropRice()
    {
        if (ricePool == null) //[cite: 11]
        {
            Debug.LogWarning($"[Rice] '{name}' không có ricePool — không rơi lúa."); //[cite: 11]
            return null; //[cite: 11]
        }

        RicePickup[] drops = new RicePickup[dropAmount]; //[cite: 11]

        for (int i = 0; i < dropAmount; i++) //[cite: 11]
        {
            GameObject obj = ricePool.GetObject(); //[cite: 11]

            Vector3 dropPos = transform.position + new Vector3(
                Random.Range(-0.5f, 0.5f), //[cite: 11]
                0.3f, //[cite: 11]
                Random.Range(-0.5f, 0.5f) //[cite: 11]
            );

            obj.transform.position = dropPos; //[cite: 11]

            Rigidbody rb = obj.GetComponent<Rigidbody>(); //[cite: 11]
            if (rb != null) //[cite: 11]
            {
                rb.isKinematic     = false; //[cite: 11]
                rb.linearVelocity  = Vector3.zero; //[cite: 11]
                rb.angularVelocity = Vector3.zero; //[cite: 11]
                rb.AddForce(Vector3.up * 2f + Random.insideUnitSphere * 0.5f, ForceMode.Impulse); //[cite: 11]
            }

            drops[i] = obj.GetComponent<RicePickup>(); //[cite: 11]
        }

        Debug.Log($"[Rice] '{name}' rơi {dropAmount} lúa."); //[cite: 11]

        return drops; //[cite: 11]
    }
}