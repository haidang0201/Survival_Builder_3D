using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Stone : MonoBehaviour
{
    public static List<Stone> Registry = new List<Stone>();

    [Header("Stone Settings")]
    public int maxHealth = 4;

    [Header("Drop Settings")]
    public ObjectPool stonePool;
    public int dropAmount = 2;

    private int  currentHealth;
    private bool isOccupied = false;
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void OnEnable()
    {
        currentHealth = maxHealth;
        isOccupied    = false;
        transform.localScale = originalScale;
        
        // Đã xóa Contains, add thẳng O(1)
        Registry.Add(this);
    }

    void OnDisable()
    {
        // FIX: Chặn lỗi văng game do Coroutine thao tác trên object đã bị disable
        StopAllCoroutines(); 
        Registry.Remove(this);
    }

    public bool TryClaim()
    {
        if (isOccupied) return false;
        isOccupied = true;
        return true;
    }

    public void Release() => isOccupied = false;

    public StonePickup[] TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0) return DestroyStone();
        StartCoroutine(ChippingEffect());
        return null;
    }

    IEnumerator ChippingEffect()
    {
        float healthPercent = (float)currentHealth / maxHealth;
        Vector3 targetScale = originalScale * Mathf.Lerp(0.6f, 1f, healthPercent);
        transform.localScale = targetScale * 0.8f; 
        yield return new WaitForSeconds(0.1f);
        transform.localScale = targetScale;
    }

    StonePickup[] DestroyStone()
    {
        StonePickup[] drops = DropStone();
        isOccupied = false;
        gameObject.SetActive(false);
        return drops;
    }

    StonePickup[] DropStone()
    {
        if (stonePool == null) return null;

        StonePickup[] drops = new StonePickup[dropAmount];
        for (int i = 0; i < dropAmount; i++)
        {
            GameObject obj = stonePool.GetObject();
            Vector3 dropPos = transform.position + new Vector3(Random.Range(-0.8f, 0.8f), 0.5f, Random.Range(-0.8f, 0.8f));
            obj.transform.position = dropPos;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic     = false;
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(Vector3.up * 2.5f + Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
            }
            drops[i] = obj.GetComponent<StonePickup>();
        }
        return drops;
    }
}