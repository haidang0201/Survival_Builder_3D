using System.Collections.Generic;
using UnityEngine;

public class PooledItem : MonoBehaviour
{
    public GameObject prefab;
}

public class ArrowPool : MonoBehaviour
{
    public static ArrowPool Instance { get; private set; }

    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        Queue<GameObject> q;
        if (!pools.TryGetValue(prefab, out q))
        {
            q = new Queue<GameObject>();
            pools[prefab] = q;
        }

        GameObject item = null;
        while (q.Count > 0)
        {
            var candidate = q.Dequeue();
            if (candidate != null)
            {
                item = candidate;
                break;
            }
        }

        if (item == null)
        {
            item = Instantiate(prefab, position, rotation);
            var tag = item.GetComponent<PooledItem>();
            if (tag == null) tag = item.AddComponent<PooledItem>();
            tag.prefab = prefab;
        }
        else
        {
            item.transform.SetPositionAndRotation(position, rotation);
            item.SetActive(true);
        }

        return item;
    }

    public void Release(GameObject obj)
    {
        if (obj == null) return;

        var tag = obj.GetComponent<PooledItem>();
        if (tag == null || tag.prefab == null)
        {
            // not a pooled item, destroy fallback
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        Queue<GameObject> q;
        if (!pools.TryGetValue(tag.prefab, out q))
        {
            q = new Queue<GameObject>();
            pools[tag.prefab] = q;
        }
        q.Enqueue(obj);
    }
}
