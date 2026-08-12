using UnityEngine;
using UnityEngine.AI;

/*
 * WorkerSpawner.cs — ĐÃ ĐƠN GIẢN HÓA
 *
 * Worker giờ KHÔNG còn tìm tài nguyên, vận chuyển, hay chạy trốn.
 * Chúng chỉ phát animation khai thác (WorkerHarvest.cs).
 *
 * WorkerSpawner vẫn được giữ lại để:
 *   - Spawn worker từ prefab khi được gọi từ HouseSpawnPanel.
 *   - Đăng ký vào WorkerManager (phục vụ Save/Load).
 *
 * Worker trong Prefab kho (gỗ/lúa/đá) là child object đặt sẵn — KHÔNG cần spawn qua đây.
 */
public class WorkerSpawner : MonoBehaviour
{
    public static WorkerSpawner Instance { get; private set; }

    public enum WorkerType { Tree, Rice, Stone }

    [Header("Prefabs theo loại tài nguyên")]
    public GameObject treeWorkerPrefab;
    public GameObject riceWorkerPrefab;
    public GameObject stoneWorkerPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Bán kính rải vị trí spawn ngẫu nhiên quanh điểm gốc (House).")]
    public float defaultSpawnScatterRadius = 2.5f;

    [Tooltip("Số lần thử tìm vị trí hợp lệ trên NavMesh trước khi fallback về điểm gốc.")]
    public int maxSpawnPositionAttempts = 8;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn 1 worker đúng loại, quanh vị trí gốc, gắn WorkerHarvest tương ứng.
    /// </summary>
    public GameObject SpawnWorker(WorkerType type, Vector3 originPosition)
    {
        return SpawnWorker(type, originPosition, defaultSpawnScatterRadius);
    }

    public GameObject SpawnWorker(WorkerType type, Vector3 originPosition, float scatterRadius)
    {
        GameObject prefab = GetPrefabFor(type);
        if (prefab == null)
        {
            Debug.LogError($"[WorkerSpawner] Chưa gán prefab cho loại {type} trong Inspector.");
            return null;
        }

        Vector3 spawnPos = ResolveSpawnPosition(originPosition, scatterRadius);
        GameObject worker = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Gắn WorkerHarvest với đúng loại nếu chưa có
        WorkerHarvest harvest = worker.GetComponent<WorkerHarvest>();
        if (harvest == null) harvest = worker.AddComponent<WorkerHarvest>();

        harvest.harvestType = type switch
        {
            WorkerType.Tree  => WorkerHarvest.HarvestType.Wood,
            WorkerType.Rice  => WorkerHarvest.HarvestType.Rice,
            WorkerType.Stone => WorkerHarvest.HarvestType.Stone,
            _                => WorkerHarvest.HarvestType.Wood
        };

        // Đăng ký vào WorkerManager để Save/Load biết có bao nhiêu worker
        if (WorkerManager.Ins != null)
        {
            WorkerManager.Ins.RegisterWorker(worker, type.ToString());
        }

        // Tự xóa khỏi Manager khi bị Destroy
        var notifier = worker.AddComponent<WorkerDestroyNotifier>();
        notifier.workerType = type.ToString();

        return worker;
    }

    // ── Private Helpers ────────────────────────────────────────────────────────

    GameObject GetPrefabFor(WorkerType type)
    {
        return type switch
        {
            WorkerType.Tree  => treeWorkerPrefab,
            WorkerType.Rice  => riceWorkerPrefab,
            WorkerType.Stone => stoneWorkerPrefab,
            _                => null
        };
    }

    Vector3 ResolveSpawnPosition(Vector3 origin, float radius)
    {
        for (int i = 0; i < maxSpawnPositionAttempts; i++)
        {
            var rand = Random.insideUnitCircle * radius;
            Vector3 candidate = origin + new Vector3(rand.x, 0f, rand.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius + 1f, NavMesh.AllAreas))
                return hit.position;
        }
        if (NavMesh.SamplePosition(origin, out NavMeshHit fallbackHit, radius + 1f, NavMesh.AllAreas))
            return fallbackHit.position;
        return origin;
    }
}

// ── Notifier: tự xóa khỏi WorkerManager khi worker bị Destroy ──────────────
public class WorkerDestroyNotifier : MonoBehaviour
{
    public string workerType;
    private bool isQuitting = false;

    void OnApplicationQuit() => isQuitting = true;

    void OnDestroy()
    {
        if (isQuitting) return;
        var manager = FindObjectOfType<WorkerManager>();
        if (manager != null) manager.UnregisterWorker(gameObject);
    }
}