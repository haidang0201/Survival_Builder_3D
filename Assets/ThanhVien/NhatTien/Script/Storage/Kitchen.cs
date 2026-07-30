using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// Nhà bếp — nơi Worker vào nghỉ ngơi và tiêu thụ lúa từ WarehouseStorage.
/// - Có slot giới hạn số worker bên trong cùng lúc
/// - Khi vào: tiêu lúa, ẩn model worker
/// - Khi ra: hiện model worker, giải phóng slot
/// - Hết lúa hoặc đầy slot: worker đứng ngoài phục hồi chậm
/// Gán tag "Kitchen" để WorkerStamina tự tìm.
/// </summary>
public class Kitchen : MonoBehaviour
{
    [Header("Capacity")]
    [Tooltip("Số worker tối đa được vào bếp cùng lúc")]
    public int maxCapacity = 3;

    [Header("Penta Dev - Civil Workers Setup")]
    [Tooltip("Cấu hình TỔNG SỐ WORKER được spawn tối đa qua từng level (KHÔNG liên quan đến slot nghỉ trong bếp — xem maxCapacity ở trên)")]
    public int[] maxWorkersLevels = new int[] { 3, 5, 8 };
    [Tooltip("FIX: tách riêng khỏi maxCapacity. maxCapacity = giới hạn slot nghỉ trong bếp; maxWorkerPopulation = giới hạn tổng số worker được spawn theo level.")]
    public int maxWorkerPopulation = 0;
    public int currentWorkersCount = 0;

    [Header("Spawn Settings")]
    public GameObject workerPrefab;
    public Transform spawnPoint;
    [Tooltip("Số lượng đầu bếp / worker sẽ sinh ra khi nâng cấp các level tương ứng")]
    public int[] spawnAmountPerLevel = new int[] { 1, 1, 2 };

    [Header("Food Settings")]
    [Tooltip("Số lúa tiêu thụ mỗi lần worker vào bếp nghỉ")]
    public int foodPerWorkerRest = 1;

    [Header("References")]
    [Tooltip("Kho chính — lấy lúa từ đây. Tự tìm qua Tag 'Warehouse' nếu bỏ trống.")]
    public WarehouseStorage warehouseStorage;

    [Tooltip("Vị trí Cửa bếp để Worker đi tới. Nếu bỏ trống sẽ tự động lấy tâm của Kitchen.")]
    public Transform entrancePoint;

    [Tooltip("Các vị trí đứng bên ngoài bếp (tùy chọn). Chống việc bị kẹt tụ tập một chỗ.")]
    public Transform[] restSlots;

    [Header("Events UI Connection")]
    public UnityEvent<int, int> onWorkersChanged; // truyền số lượng hiện tại, max worker

    private List<WorkerStamina> workersInside = new List<WorkerStamina>();
    private int _nextSlotIndex = 0;
    private int currentLevelIndex = 0;

    public int  WorkerCount      => workersInside.Count;
    public bool IsFull           => workersInside.Count >= maxCapacity;
    public bool HasFood          => GetWarehouse() != null && GetWarehouse().CurrentRice >= foodPerWorkerRest;
    public Vector3 EntrancePosition => entrancePoint != null ? entrancePoint.position : transform.position;

    private WarehouseStorage GetWarehouse()
    {
        if (warehouseStorage == null)
        {
            if (BuildingManager.Ins != null)
            {
                foreach (var b in BuildingManager.Ins.Buildings)
                {
                    if (b.buildingType == BuildingType.Warehouse)
                    {
                        warehouseStorage = b.GetComponent<WarehouseStorage>() ?? b.GetComponentInChildren<WarehouseStorage>();
                        if (warehouseStorage != null) break;
                    }
                }
            }
        }
        return warehouseStorage;
    }

    void Start()
    {
        // KHÔNG tìm warehouse ngay trong Start nữa vì BuildingCtrl có thể chưa kịp đăng ký vào BuildingManager
    }

    /// <summary>
    /// Hàm nhận diện nâng cấp đồng bộ cấu trúc cho công trình dân sự từ UpgradeableBuilding
    /// FIX: maxWorkersLevels chỉ dùng để giới hạn TỔNG SỐ WORKER SPAWN (maxWorkerPopulation).
    /// Trước đây nó ghi đè lên maxCapacity (giới hạn slot nghỉ trong bếp), khiến số lượng
    /// worker được phép vào nghỉ nhảy loạn theo level nâng cấp dù restSlots không đổi.
    /// </summary>
    public void SetupLevel(int levelIndex)
    {
        currentLevelIndex = levelIndex;

        if (maxWorkersLevels != null && levelIndex < maxWorkersLevels.Length)
        {
            maxWorkerPopulation = maxWorkersLevels[levelIndex];
            onWorkersChanged?.Invoke(currentWorkersCount, maxWorkerPopulation);
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
            // FIX: kiểm tra giới hạn tổng số worker (maxWorkerPopulation), KHÔNG phải maxCapacity (slot nghỉ)
            if (currentWorkersCount >= maxWorkerPopulation) break;

            GameObject newWorker = Instantiate(workerPrefab, point.position, point.rotation);
            currentWorkersCount++;

            Debug.Log($"[Kitchen Spawn] Đã tạo thành công worker mới: {newWorker.name} tại Cấp {levelIndex + 1}");
        }

        onWorkersChanged?.Invoke(currentWorkersCount, maxWorkerPopulation);
    }

    /// <summary>
    /// FIX: gọi hàm này khi 1 worker bị destroy/chết để currentWorkersCount không tăng ảo
    /// vĩnh viễn. Hiện tại chưa có hệ thống worker chết nên chưa nơi nào gọi tới — để sẵn hook.
    /// </summary>
    public void NotifyWorkerRemoved()
    {
        currentWorkersCount = Mathf.Max(0, currentWorkersCount - 1);
        onWorkersChanged?.Invoke(currentWorkersCount, maxWorkerPopulation);
    }

    /// <summary>
    /// Worker xin vào bếp.
    /// FIX: thêm out consumedFood để WorkerStamina biết chính xác worker có ăn được không,
    /// tránh bug kiểm tra HasFood SAU KHI đã ConsumeRice (kết quả sai khi lúa vừa hết).
    /// </summary>
    public bool Enter(WorkerStamina worker, out bool consumedFood)
    {
        consumedFood = false;
        if (worker == null) return false;
        if (workersInside.Contains(worker)) return true;
        if (IsFull) return false;

        workersInside.Add(worker);

        if (HasFood)
        {
            GetWarehouse().ConsumeRice(foodPerWorkerRest);
            consumedFood = true;
            Debug.Log($"[Kitchen] {worker.name} vào bếp nghỉ ngơi (đã ăn lúa). " +
                      $"Slot: {workersInside.Count}/{maxCapacity}");
        }
        else
        {
            Debug.Log($"[Kitchen] {worker.name} vào nhà trú ẩn nhưng nhịn đói " +
                      $"(hồi stamina chậm). Kho lúa không đủ.");
        }

        return true;
    }

    public void Exit(WorkerStamina worker)
    {
        if (worker == null) return;

        if (workersInside.Remove(worker))
            Debug.Log($"[Kitchen] {worker.name} no nê đi làm. " +
                      $"Slot còn trống: {maxCapacity - workersInside.Count}/{maxCapacity}");
    }

    /// <summary>
    /// Round-robin — tránh nhiều worker chồng lên cùng 1 slot.
    /// FIX: fallback về EntrancePosition (cửa bếp) thay vì transform.position (tâm bếp).
    /// Trước đây khi restSlots rỗng, worker chỉ cần lảng vảng trong interactionRadius quanh
    /// TÂM Kitchen (thường nằm giữa building, xa cửa thật) là đã bị tính "đã tới nơi" và
    /// kitchen.Enter() được gọi sớm, khiến model bị ẩn dù chưa thực sự đi tới cửa bếp.
    /// </summary>
    public Vector3 GetRestPosition()
    {
        if (restSlots != null && restSlots.Length > 0)
        {
            for (int i = 0; i < restSlots.Length; i++)
            {
                int idx = (_nextSlotIndex + i) % restSlots.Length;
                if (restSlots[idx] != null)
                {
                    _nextSlotIndex = (idx + 1) % restSlots.Length;
                    return restSlots[idx].position;
                }
            }
        }
        return EntrancePosition;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(EntrancePosition, 2f);
    }
}