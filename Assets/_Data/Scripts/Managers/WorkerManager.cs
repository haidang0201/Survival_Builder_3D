using UnityEngine;
using System.Collections.Generic;

/*
 * WorkerManager.cs — ĐÃ ĐƠN GIẢN HÓA
 *
 * Chức năng còn lại:
 *   - Theo dõi danh sách worker đang có trong Scene (RegisterWorker / UnregisterWorker).
 *   - GetAllStates()  → gom vị trí worker để Save.
 *   - LoadStates()    → Spawn lại worker đúng vị trí khi Load game.
 *
 * KHÔNG còn lưu trạng thái "đang cầm đồ" vì worker không còn vận chuyển tài nguyên nữa.
 */
public class WorkerManager : Singleton<WorkerManager>
{
    public class WorkerRef
    {
        public GameObject workerObj;
        public string     type;
    }

    private List<WorkerRef> activeWorkers = new List<WorkerRef>();

    // ── Register / Unregister ──────────────────────────────────────────────────

    public void RegisterWorker(GameObject worker, string type)
    {
        if (worker == null) return;
        activeWorkers.Add(new WorkerRef { workerObj = worker, type = type });
    }

    public void UnregisterWorker(GameObject worker)
    {
        if (worker == null) return;
        activeWorkers.RemoveAll(w => w.workerObj == worker);
    }

    // ── Save / Load ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gom trạng thái (vị trí + loại) của tất cả worker đang sống để ghi vào file JSON.
    /// </summary>
    public List<WorkerState> GetAllStates()
    {
        var states = new List<WorkerState>();
        foreach (var w in activeWorkers)
        {
            if (w.workerObj == null) continue;
            states.Add(new WorkerState
            {
                workerType     = w.type,
                position       = new SerializableVector3(w.workerObj.transform.position),
                rotation       = new SerializableVector3(w.workerObj.transform.eulerAngles),
                isCarryingItem = false   // Worker không còn vận chuyển — luôn false
            });
        }
        return states;
    }

    /// <summary>
    /// Spawn lại worker từ file JSON đã lưu.
    /// </summary>
    public void LoadStates(List<WorkerState> states)
    {
        if (states == null || states.Count == 0) return;

        // Dọn worker cũ
        foreach (var w in activeWorkers)
            if (w.workerObj != null) Destroy(w.workerObj);
        activeWorkers.Clear();

        if (WorkerSpawner.Instance == null)
        {
            Debug.LogError("[WorkerManager] Không tìm thấy WorkerSpawner trong Scene!");
            return;
        }

        foreach (var state in states)
        {
            WorkerSpawner.WorkerType type;
            if      (state.workerType == "Tree")  type = WorkerSpawner.WorkerType.Tree;
            else if (state.workerType == "Rice")  type = WorkerSpawner.WorkerType.Rice;
            else if (state.workerType == "Stone") type = WorkerSpawner.WorkerType.Stone;
            else continue;

            Vector3 pos = state.position.ToVector3();
            GameObject newWorker = WorkerSpawner.Instance.SpawnWorker(type, pos, 0f);
            if (newWorker != null)
                newWorker.transform.eulerAngles = state.rotation.ToVector3();
        }
        Debug.Log($"[WorkerManager] Đã load {states.Count} worker.");
    }
}
