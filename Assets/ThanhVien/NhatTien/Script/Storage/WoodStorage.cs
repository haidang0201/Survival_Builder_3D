using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Kho Gỗ — kho CHÍNH, ghi thẳng vào JsonDataManager.
/// Tài nguyên gỗ được cộng tự động bởi DayNightManager khi kết thúc mỗi Wave chuẩn bị.
/// - resourcesOnSkip:    Gỗ nhận được khi người chơi nhấn Skip.
/// - resourcesOnFullTime: Gỗ nhận được khi để hết thời gian (nhiều hơn).
/// </summary>
public class WoodStorage : MonoBehaviour
{
    // ── Static registry: DayNightManager tự tìm tất cả kho Gỗ trong Scene ──
    public static readonly List<WoodStorage> All = new List<WoodStorage>();

    [Header("Storage Settings")]
    public int maxCapacity = 9999;

    [Header("Tài nguyên Gỗ theo Wave")]
    [Tooltip("Gỗ cộng khi người chơi nhấn Skip (ít hơn vì bỏ qua thời gian).")]
    public int resourcesOnSkip     = 10;
    [Tooltip("Gỗ cộng khi để hết thời gian chuẩn bị không Skip (nhiều hơn).")]
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
    public UnityEvent<int> onWoodAdded;           // truyền currentAmount
    public UnityEvent<int, int> onWorkersChanged; // truyền (current, max)
    public UnityEvent<int> onCapacityChanged;     // truyền maxCapacity mới

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
    /// <summary>Cộng gỗ SKIP — người chơi nhấn Start Wave sớm (chỉ cộng khi đã xây xong).</summary>
    public int GrantSkipResources()
    {
        if (!IsReadyToProduce()) return 0;
        return AddWood(resourcesOnSkip);
    }

    /// <summary>Cộng gỗ ĐẦY GIỜ — để hết thời gian chuẩn bị mới vào Wave (chỉ cộng khi đã xây xong).</summary>
    public int GrantFullTimeResources()
    {
        if (!IsReadyToProduce()) return 0;
        return AddWood(resourcesOnFullTime);
    }

    // ===== PROPERTIES — đọc thẳng từ JsonDataManager =====
    public int  CurrentAmount => JsonDataManager.Ins != null ? JsonDataManager.Ins.wood : 0;
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
            Debug.Log($"[WoodStorage Spawn] Tạo worker mới tại Cấp {levelIndex + 1}");
        }
        onWorkersChanged?.Invoke(currentWorkersCount, MaxWorkers);
    }

    // ===== PUBLIC API =====

    public int AddWood(int amount = 1)
    {
        if (IsFull)
        {
            Debug.Log($"[WoodStorage] '{name}' đã đầy! ({CurrentAmount}/{maxCapacity})");
            return 0;
        }

        int canAdd = Mathf.Min(amount, maxCapacity - CurrentAmount);
        SyncToManager(canAdd);

        Debug.Log($"[WoodStorage] '{name}' +{canAdd} gỗ → {CurrentAmount}/{maxCapacity}");
        onWoodAdded?.Invoke(CurrentAmount);
        if (IsFull) onStorageFull?.Invoke();
        return canAdd;
    }

    /// <summary>
    /// Lấy gỗ ra (dùng cho hệ thống xây dựng, nâng cấp, v.v.)
    /// </summary>
    public int TakeWood(int amount = 1)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"[WoodStorage] '{name}' Kho trống!");
            return 0;
        }
        int canTake = Mathf.Min(amount, CurrentAmount);
        SyncToManager(-canTake);
        Debug.Log($"[WoodStorage] '{name}' -{canTake} gỗ → {CurrentAmount}/{maxCapacity}");
        return canTake;
    }

    public void ClearStorage()
    {
        SyncToManager(-CurrentAmount);
        Debug.Log($"[WoodStorage] '{name}' Kho đã làm trống.");
    }

    // ===== INTERNAL =====

    private void SyncToManager(int delta)
    {
        if (JsonDataManager.Ins == null)
        {
            Debug.LogError("[WoodStorage] Không tìm thấy JsonDataManager.Ins!");
            return;
        }
        JsonDataManager.Ins.AddWood(delta);
    }

    // ===== GIZMO =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.35f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}