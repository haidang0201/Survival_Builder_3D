using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public int poolSize = 10;

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Start()
    {
        if (prefab == null)
        {
            Debug.LogError($"[ObjectPool] '{name}' chưa gán prefab!");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            CreateAndEnqueue();
        }

        Debug.Log($"[ObjectPool] '{name}' khởi tạo {poolSize} object từ prefab '{prefab.name}'.");
    }

    public GameObject GetObject()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        // Pool hết → tạo thêm và cảnh báo
        Debug.LogWarning($"[ObjectPool] '{name}' hết object — tạo thêm. " +
                         $"Cân nhắc tăng poolSize (hiện tại: {poolSize}).");
        return CreateAndEnqueue(activate: true);
    }

    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    // ===== INTERNAL =====

    GameObject CreateAndEnqueue(bool activate = false)
    {
        GameObject obj = Instantiate(prefab, transform); // đặt vào pool transform cho gọn hierarchy
        obj.SetActive(activate);

        if (!activate)
            pool.Enqueue(obj);

        return obj;
    }
}