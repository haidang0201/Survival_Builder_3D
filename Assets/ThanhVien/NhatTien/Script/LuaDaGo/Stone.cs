using UnityEngine;
using System.Collections.Generic;

public class Stone : MonoBehaviour
{
    // Đăng ký tĩnh siêu tối ưu hiệu năng
    public static List<Stone> Registry = new List<Stone>();

    [Header("Stone Settings")]
    public int maxHealth = 4;

    [Header("Drop Settings")]
    public ObjectPool stonePool;
    public int dropAmount = 2;

    private int  currentHealth;
    private bool isOccupied = false;

    void OnEnable()
    {
        currentHealth = maxHealth;
        isOccupied    = false;

        if (!Registry.Contains(this)) Registry.Add(this);
    }

    void OnDisable()
    {
        if (Registry.Contains(this)) Registry.Remove(this);
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
        return null;
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
            Vector3 dropPos = transform.position + new Vector3(
                Random.Range(-0.8f, 0.8f), 0.5f, Random.Range(-0.8f, 0.8f)
            );
            
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