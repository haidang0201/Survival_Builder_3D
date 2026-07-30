using UnityEngine;
using System.Collections.Generic;

/*
 * EnvironmentResourceManager.cs
 * Folder: Scripts/Managers/
 *
 * CHỨC NĂNG:
 * Quản lý tập trung dữ liệu Save/Load của các node tài nguyên tự nhiên trên bản đồ (Cây, Lúa, Đá).
 */
public class EnvironmentResourceManager : Singleton<EnvironmentResourceManager>
{
    public List<ResourceEntityState> GetAllStates()
    {
        List<ResourceEntityState> states = new List<ResourceEntityState>();

        // 1. Cây (Tree)
        if (WorkerFindTree.Registry != null)
        {
            foreach (var tree in WorkerFindTree.Registry)
            {
                if (tree == null) continue;
                states.Add(new ResourceEntityState
                {
                    resourceType = "Tree",
                    position = new SerializableVector3(tree.transform.position),
                    currentHealth = tree.GetCurrentHealth(),
                    isVisible = true // Đang trong registry tức là hiển thị
                });
            }
        }

        // 2. Lúa (Rice)
        if (WorkerFindRice.Registry != null)
        {
            foreach (var rice in WorkerFindRice.Registry)
            {
                if (rice == null) continue;
                states.Add(new ResourceEntityState
                {
                    resourceType = "Rice",
                    position = new SerializableVector3(rice.transform.position),
                    currentHealth = rice.GetCurrentHealth(),
                    isVisible = true
                });
            }
        }

        // 3. Đá (Stone)
        if (Stone.Registry != null)
        {
            foreach (var stone in Stone.Registry)
            {
                if (stone == null) continue;
                states.Add(new ResourceEntityState
                {
                    resourceType = "Stone",
                    position = new SerializableVector3(stone.transform.position),
                    currentHealth = stone.GetCurrentHealth(),
                    isVisible = true
                });
            }
        }

        return states;
    }

    public void LoadStates(List<ResourceEntityState> states)
    {
        if (states == null || states.Count == 0) return;

        // Dùng Dictionary để mapping dễ dàng dựa theo vị trí (sai số nhỏ để trừ hao sai lệch float)
        Dictionary<Vector3, ResourceEntityState> stateDict = new Dictionary<Vector3, ResourceEntityState>();
        foreach (var st in states)
        {
            stateDict[st.position.ToVector3()] = st;
        }

        // Load cho Tree
        if (WorkerFindTree.Registry != null)
        {
            foreach (var tree in WorkerFindTree.Registry)
            {
                if (tree == null) continue;
                var closest = FindClosestState(stateDict, tree.transform.position, "Tree");
                if (closest != null)
                {
                    tree.SetCurrentHealth(closest.currentHealth);
                    // Bỏ trạng thái khỏi dict để tối ưu
                    stateDict.Remove(closest.position.ToVector3());
                }
            }
        }

        // Load cho Rice
        if (WorkerFindRice.Registry != null)
        {
            foreach (var rice in WorkerFindRice.Registry)
            {
                if (rice == null) continue;
                var closest = FindClosestState(stateDict, rice.transform.position, "Rice");
                if (closest != null)
                {
                    rice.SetCurrentHealth(closest.currentHealth);
                    stateDict.Remove(closest.position.ToVector3());
                }
            }
        }

        // Load cho Stone
        if (Stone.Registry != null)
        {
            foreach (var stone in Stone.Registry)
            {
                if (stone == null) continue;
                var closest = FindClosestState(stateDict, stone.transform.position, "Stone");
                if (closest != null)
                {
                    stone.SetCurrentHealth(closest.currentHealth);
                    stateDict.Remove(closest.position.ToVector3());
                }
            }
        }
        
        Debug.Log($"[EnvironmentResourceManager] Đã load thông tin HP cho các node tài nguyên.");
    }

    private ResourceEntityState FindClosestState(Dictionary<Vector3, ResourceEntityState> dict, Vector3 pos, string type)
    {
        foreach (var kvp in dict)
        {
            if (kvp.Value.resourceType == type && Vector3.Distance(kvp.Key, pos) < 0.1f)
            {
                return kvp.Value;
            }
        }
        return null;
    }
}
