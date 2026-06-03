using UnityEngine;

public class Rice : MonoBehaviour
{
    [Header("Rice Settings")]
    public int maxHealth = 2;

    [Header("Drop Settings")]
    public ObjectPool ricePool;
    public int dropAmount = 2;

    private int  currentHealth;
    private bool isOccupied = false;

    void OnEnable()
    {
        currentHealth = maxHealth;
        isOccupied    = false;
        WorkerFindRice.Registry.Add(this);
    }

    void OnDisable()
    {
        WorkerFindRice.Registry.Remove(this);
    }

    public bool TryClaim()
    {
        if (isOccupied) return false;
        isOccupied = true;
        return true;
    }

    public void Release() => isOccupied = false;

    public RicePickup[] TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"[Rice] '{name}' bị gặt. HP còn lại: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) return HarvestRice();
        return null;
    }

    RicePickup[] HarvestRice()
    {
        RicePickup[] drops = DropRice();
        isOccupied = false;
        gameObject.SetActive(false);
        Debug.Log($"[Rice] '{name}' đã gặt xong → tắt.");
        return drops;
    }

    RicePickup[] DropRice()
    {
        if (ricePool == null)
        {
            Debug.LogWarning($"[Rice] '{name}' không có ricePool — không rơi lúa.");
            return null;
        }

        RicePickup[] drops = new RicePickup[dropAmount];
        for (int i = 0; i < dropAmount; i++)
        {
            GameObject obj = ricePool.GetObject();
            Vector3 dropPos = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 0.3f, Random.Range(-0.5f, 0.5f));
            obj.transform.position = dropPos;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic     = false;
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(Vector3.up * 2f + Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
            }
            drops[i] = obj.GetComponent<RicePickup>();
        }
        Debug.Log($"[Rice] '{name}' rơi {dropAmount} lúa.");
        return drops;
    }
}