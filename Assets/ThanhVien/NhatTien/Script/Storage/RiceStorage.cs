using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kho tạm lúa — nơi worker gặt lúa nộp vào.
/// KHÔNG sync lên UI / JsonDataManager.
/// WorkerCarrier sẽ lấy từ đây và mang về WarehouseStorage.
/// Gán tag "RiceStorage" để WorkerCarrier tự tìm.
/// </summary>
public class RiceStorage : MonoBehaviour
{
    [Header("Storage Settings")]
    public int maxCapacity = 20;

    [Header("Penta Dev - Civil Workers Setup")]
    [Tooltip("Cấu hình số lượng worker tối đa qua từng level")]
    public int[] maxWorkersLevels = new int[] { 2, 4, 6 };
    public int currentWorkersCount = 0;

    [Header("Spawn Settings")]
    public GameObject workerPrefab;
    public Transform spawnPoint;
    [Tooltip("Số worker sẽ spawn tự động tương ứng khi lên từng level")]
    public int[] spawnAmountPerLevel = new int[] { 1, 1, 2 };

    [Header("Events")]
    public UnityEvent      onStorageFull;
    public UnityEvent<int> onRiceAdded;
    public UnityEvent<int, int> onWorkersChanged; // truyền (current, max)
    public UnityEvent<int> onCapacityChanged; // truyền maxCapacity mới

    private int currentAmount = 0;
    private int currentLevelIndex = 0;

    // ===== PROPERTIES =====
    public int  CurrentAmount => currentAmount;
    public int  MaxCapacity   => maxCapacity;
    public bool IsFull        => currentAmount >= maxCapacity;
    public bool IsEmpty       => currentAmount <= 0;
    public int  MaxWorkers    => (maxWorkersLevels != null && currentLevelIndex < maxWorkersLevels.Length) ? maxWorkersLevels[currentLevelIndex] : 0;

    // ===== PUBLIC API =====

    /// <summary>
    /// Hàm nhận diện nâng cấp từ UpgradeableBuilding để đồng bộ chỉ số dân sự
    /// </summary>
    public void SetupLevel(int levelIndex)
    {
        currentLevelIndex = levelIndex;

        if (maxWorkersLevels != null && levelIndex < maxWorkersLevels.Length)
        {
            onWorkersChanged?.Invoke(currentWorkersCount, maxWorkersLevels[levelIndex]);
        }

        SpawnWorkersForLevel(levelIndex);
    }

    private void SpawnWorkersForLevel(int levelIndex)
    {
        if (workerPrefab == null || spawnAmountPerLevel == null || levelIndex >= spawnAmountPerLevel.Length) return;

        int amountToSpawn = spawnAmountPerLevel[levelIndex];
        Transform point = spawnPoint != null ? spawnPoint : transform;

        for (int i = 0; i < amountToSpawn; i++)
        {
            if (currentWorkersCount >= MaxWorkers) break;

            GameObject newWorker = Instantiate(workerPrefab, point.position, point.rotation);
            currentWorkersCount++;
            
            Debug.Log($"[RiceStorage Spawn] Đã tạo thành công worker mới: {newWorker.name} tại Cấp {levelIndex + 1}");
        }

        onWorkersChanged?.Invoke(currentWorkersCount, MaxWorkers);
    }

    public int AddRice(int amount = 1)
    {
        if (IsFull)
        {
            Debug.Log($"[RiceStorage] '{name}' đã đầy! ({currentAmount}/{maxCapacity})");
            return 0;
        }

        int canAdd     = Mathf.Min(amount, maxCapacity - currentAmount);
        currentAmount += canAdd;

        Debug.Log($"[RiceStorage] '{name}' +{canAdd} lúa → {currentAmount}/{maxCapacity}");

        onRiceAdded?.Invoke(currentAmount);

        if (IsFull)
        {
            Debug.Log($"[RiceStorage] '{name}' ✅ Kho đầy!");
            onStorageFull?.Invoke();
        }

        return canAdd;
    }

    public int TakeRice(int amount = 1)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"[RiceStorage] '{name}' Kho trống!");
            return 0;
        }

        int canTake    = Mathf.Min(amount, currentAmount);
        currentAmount -= canTake;

        Debug.Log($"[RiceStorage] '{name}' -{canTake} lúa → {currentAmount}/{maxCapacity}");

        return canTake;
    }

    public void ClearStorage()
    {
        currentAmount = 0;
        Debug.Log($"[RiceStorage] '{name}' Kho đã làm trống.");
    }

    // ===== GIZMO =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}