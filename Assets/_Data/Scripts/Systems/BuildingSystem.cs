using UnityEngine;

/*
 * BuildingSystem.cs
 * Folder: Scripts/Building/
 * Người làm: VŨ (UI gọi) + DŨNG (logic)
 *
 * Hệ thống đặt công trình chính thức trong Gameplay.
 * Thay thế hoàn toàn TestBuildingPlacement.
 *
 * Cách dùng từ UI (Vũ):
 *   BuildingSystem.Ins.StartPlacing(BuildingType.House);
 *   BuildingSystem.Ins.CancelPlacing();
 *
 * Cách dùng Save/Load (Dũng):
 *   BuildingSystem.Ins.SaveBuildings();
 *   BuildingSystem.Ins.LoadBuildings();
 *
 * Lưu ý khi thêm BuildingType mới:
 *   1. Thêm vào BuildingType.cs
 *   2. Khai báo ghostXxxPrefab ở đây
 *   3. Thêm case vào GetGhostPrefab()
 */

public class BuildingSystem : Singleton<BuildingSystem>
{
    // ================= INSPECTOR =================

    [Header("Ghost Prefabs – Dân sự")]
    public GameObject ghostHousePrefab;
    public GameObject ghostWoodCutterPrefab;
    public GameObject ghostStoneMinePrefab;
    public GameObject ghostKitchenPrefab;
    public GameObject ghostFoodStoragePrefab;
    public GameObject ghostStoneStoragePrefab;
    public GameObject ghostWarehousePrefab;

    [Header("Ghost Prefabs – Phòng thủ")]
    public GameObject ghostWatchTowerPrefab;
    public GameObject ghostArcherTowerPrefab;
    public GameObject ghostCannonPrefab;

    [Header("Ghost Prefabs – Quân sự (Nhà lính)")]
    public GameObject ghostBarracksMeleePrefab;
    public GameObject ghostBarracksArcherPrefab;
    public GameObject ghostBarracksSpearPrefab;

    // ================= PRIVATE =================

    private GhostBuilding currentGhost;
    private bool isPlacing = false;

    public bool IsPlacing => isPlacing;

    // ================= LIFECYCLE =================

    private void Update()
    {
        if (!isPlacing) return;

        if (Input.GetKeyDown(KeyCode.Escape))
            CancelPlacing();
    }

    // ================= PUBLIC – UI GỌI =================

    /// <summary>
    /// Bắt đầu chế độ đặt công trình – UI button gọi hàm này.
    /// VD: BuildingSystem.Ins.StartPlacing(BuildingType.House);
    /// </summary>
    public void StartPlacing(BuildingType type)
    {
        if (type == BuildingType.None)
        {
            Debug.LogWarning("[BuildingSystem] Không thể đặt BuildingType.None.");
            return;
        }

        // Huỷ ghost cũ nếu đang có
        CancelPlacing();

        GameObject prefab = GetGhostPrefab(type);
        if (prefab == null)
        {
            Debug.LogWarning($"[BuildingSystem] Chưa gán ghost prefab cho: {type}");
            return;
        }

        GameObject obj = Instantiate(prefab);
        currentGhost = obj.GetComponent<GhostBuilding>();

        if (currentGhost == null)
        {
            Debug.LogError($"[BuildingSystem] Prefab {type} thiếu component GhostBuilding!");
            Destroy(obj);
            return;
        }

        currentGhost.buildingType = type;
        currentGhost.Show();
        isPlacing = true;

        // UIManager.Ins?.HideBuildingMenu();
    }

    /// <summary>Huỷ đặt công trình hiện tại – gọi khi nhấn ESC hoặc nút Cancel UI.</summary>
    public void CancelPlacing()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost.gameObject);
            currentGhost = null;
        }

        isPlacing = false;
    }

    /// <summary>
    /// Gọi từ GhostBuilding khi đặt xong (Confirm) hoặc huỷ (Cancel).
    /// GhostBuilding tự Destroy() trước khi gọi hàm này.
    /// </summary>
    public void OnPlacingCompleted()
    {
        currentGhost = null;
        isPlacing = false;

        // UIManager.Ins?.ShowHUD();
    }

    // ================= PUBLIC – SAVE / LOAD =================

    /// <summary>Lưu toàn bộ building – gọi khi người chơi save game.</summary>
    public void SaveBuildings()
    {
        var states = BuildingManager.Ins.GetAllStates();

        if (states.Count == 0)
        {
            Debug.LogWarning("[BuildingSystem] Không có công trình nào để lưu!");
            return;
        }

        var saveData = new JsonDataManager.GameSaveData
        {
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            savedAtUnix = System.DateTimeOffset.Now.ToUnixTimeSeconds(),
            buildings = states,
            resources = new System.Collections.Generic.List<JsonDataManager.ResourceData>()
        };

        bool result = JsonDataManager.Ins.SaveGame(saveData);
        Debug.Log(result
            ? $"[BuildingSystem] ✅ Đã lưu {states.Count} công trình."
            : "[BuildingSystem] ❌ Lưu thất bại!");
    }

    /// <summary>Tải building từ save – gọi khi vào game hoặc load scene.</summary>
    public void LoadBuildings()
    {
        var saveData = JsonDataManager.Ins.LoadGame();

        if (saveData == null || saveData.buildings == null || saveData.buildings.Count == 0)
        {
            Debug.Log("[BuildingSystem] Chưa có save hoặc không có công trình.");
            return;
        }

        BuildingManager.Ins.LoadStates(saveData.buildings);
        Debug.Log($"[BuildingSystem] ✅ Đã tải {saveData.buildings.Count} công trình.");
    }

    // ================= PRIVATE =================

    private GameObject GetGhostPrefab(BuildingType type)
    {
        switch (type)
        {
            // Dân sự
            case BuildingType.House: return ghostHousePrefab;
            case BuildingType.WoodCutter: return ghostWoodCutterPrefab;
            case BuildingType.StoneMine: return ghostStoneMinePrefab;
            case BuildingType.Kitchen: return ghostKitchenPrefab;
            case BuildingType.FoodStorage: return ghostFoodStoragePrefab;
            case BuildingType.StoneStorage: return ghostStoneStoragePrefab;
            case BuildingType.Warehouse: return ghostWarehousePrefab;

            // Phòng thủ
            case BuildingType.WatchTower: return ghostWatchTowerPrefab;
            case BuildingType.ArcherTower: return ghostArcherTowerPrefab;
            case BuildingType.Cannon: return ghostCannonPrefab;

            // Quân sự
            case BuildingType.BarracksMelee: return ghostBarracksMeleePrefab;
            case BuildingType.BarracksArcher: return ghostBarracksArcherPrefab;
            case BuildingType.BarracksSpear: return ghostBarracksSpearPrefab;

            default:
                Debug.LogWarning($"[BuildingSystem] Chưa có ghost prefab cho: {type}");
                return null;
        }
    }
}