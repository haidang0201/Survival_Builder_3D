using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Kho Lúa — kho CHÍNH, ghi thẳng vào JsonDataManager (nguồn thật duy nhất).
/// Worker gặt lúa nộp vào đây → ghi lên HUD ngay lập tức.
/// Kitchen lấy lúa qua ConsumeRice() — ghi thẳng vào JsonDataManager.
/// Không còn WorkerCarrier / WarehouseStorage làm trung gian.
/// </summary>
public class RiceStorage : MonoBehaviour
{
    // ── Static registry: DayNightManager tự tìm tất cả kho Lúa trong Scene ──
    public static readonly List<RiceStorage> All = new List<RiceStorage>();

    [Header("Storage Settings")]
    public int maxCapacity = 9999;

    [Header("Tài nguyên Lúa theo Wave")]
    [Tooltip("Lúa cộng khi người chơi nhấn Skip (ít hơn vì bỏ qua thời gian).")]
    public int resourcesOnSkip     = 10;
    [Tooltip("Lúa cộng khi để hết thời gian chuẩn bị không Skip (nhiều hơn).")]
    public int resourcesOnFullTime = 15;

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
    public UnityEvent<int, int> onWorkersChanged;
    public UnityEvent<int> onCapacityChanged;

    private int currentLevelIndex = 0;

    void Awake()
    {
        if (maxCapacity < 9999) maxCapacity = 9999;
    }

    void OnEnable()  { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    // ── Kiểm tra kho đã xây xong chưa ──
    /// <summary>
    /// Kiểm tra kho đã xây xong hoàn tất chưa (không đang xây dở, không nâng cấp dở, không bị tàn tích).
    /// </summary>
    public bool IsReadyToProduce()
    {
        var building = GetComponent<UpgradeableBuilding>();
        if (building == null) building = GetComponentInParent<UpgradeableBuilding>();

        if (building != null)
        {
            if (building.IsInitialBuildNeeded || building.IsUpgrading || building.IsRuined)
                return false;
        }

        return true;
    }

    // ── Được gọi bởi DayNightManager ──
    /// <summary>Cộng lúa SKIP — người chơi nhấn Start Wave sớm (chỉ cộng khi đã xây xong).</summary>
    public int GrantSkipResources()
    {
        if (!IsReadyToProduce()) return 0;
        return AddRice(resourcesOnSkip);
    }

    /// <summary>Cộng lúa ĐẦY GIỜ — để hết thời gian chuẩn bị mới vào Wave (chỉ cộng khi đã xây xong).</summary>
    public int GrantFullTimeResources()
    {
        if (!IsReadyToProduce()) return 0;
        return AddRice(resourcesOnFullTime);
    }

    // ===== PROPERTIES — đọc thẳng từ JsonDataManager =====
    public int  CurrentAmount => JsonDataManager.Ins != null ? JsonDataManager.Ins.food  : 0;
    public int  MaxCapacity   => maxCapacity;
    public bool IsFull        => CurrentAmount >= maxCapacity;
    public bool IsEmpty       => CurrentAmount <= 0;
    public int  MaxWorkers    => (maxWorkersLevels != null && currentLevelIndex < maxWorkersLevels.Length)
                                  ? maxWorkersLevels[currentLevelIndex] : 0;

    // ===== SETUP LEVEL =====

    public void SetupLevel(int levelIndex)
    {
        currentLevelIndex = levelIndex;
        if (maxWorkersLevels != null && levelIndex < maxWorkersLevels.Length)
            onWorkersChanged?.Invoke(currentWorkersCount, maxWorkersLevels[levelIndex]);
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
            Instantiate(workerPrefab, point.position, point.rotation);
            currentWorkersCount++;
            Debug.Log($"[RiceStorage Spawn] Tạo worker mới tại Cấp {levelIndex + 1}");
        }
        onWorkersChanged?.Invoke(currentWorkersCount, MaxWorkers);
    }

    // ===== PUBLIC API =====

    public int AddRice(int amount = 1)
    {
        if (IsFull)
        {
            Debug.Log($"[RiceStorage] '{name}' đã đầy! ({CurrentAmount}/{maxCapacity})");
            return 0;
        }
        int canAdd = Mathf.Min(amount, maxCapacity - CurrentAmount);
        SyncToManager(canAdd);
        Debug.Log($"[RiceStorage] '{name}' +{canAdd} lúa → {CurrentAmount}/{maxCapacity}");
        onRiceAdded?.Invoke(CurrentAmount);
        if (IsFull) onStorageFull?.Invoke();
        return canAdd;
    }

    public int TakeRice(int amount = 1)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"[RiceStorage] '{name}' Kho trống!");
            return 0;
        }
        int canTake = Mathf.Min(amount, CurrentAmount);
        SyncToManager(-canTake);
        Debug.Log($"[RiceStorage] '{name}' -{canTake} lúa → {CurrentAmount}/{maxCapacity}");
        return canTake;
    }

    /// <summary>
    /// Kitchen gọi để tiêu thụ lúa nuôi worker.
    /// </summary>
    public int ConsumeRice(int amount = 1) => TakeRice(amount);

    public void ClearStorage()
    {
        SyncToManager(-CurrentAmount);
        Debug.Log($"[RiceStorage] '{name}' Kho đã làm trống.");
    }

    // ===== INTERNAL =====

    private void SyncToManager(int delta)
    {
        if (JsonDataManager.Ins == null)
        {
            Debug.LogError("[RiceStorage] Không tìm thấy JsonDataManager.Ins!");
            return;
        }
        JsonDataManager.Ins.AddFood(delta);
    }

    // ===== GIZMO =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.85f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}