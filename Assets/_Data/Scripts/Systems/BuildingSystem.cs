using UnityEngine;

/*
 * BuildingSystem.cs
 * Folder: Scripts/Building/
 * Người làm: VŨ (UI gọi) + DŨNG (logic)
 *
 * Hệ thống đặt công trình chính thức trong Gameplay
 * Thay thế hoàn toàn TestBuildingPlacement
 *
 * Cách dùng từ UI (Vũ):
 *   BuildingSystem.Ins.StartPlacing(BuildingType.House);
 *   BuildingSystem.Ins.CancelPlacing();
 *
 * Cách dùng Save/Load (Dũng):
 *   BuildingSystem.Ins.SaveBuildings();
 *   BuildingSystem.Ins.LoadBuildings();
 */

public class BuildingSystem : Singleton<BuildingSystem>
{
    // ================= INSPECTOR =================

    [Header("Ghost Prefabs – kéo vào đây")]
    public GameObject ghostHousePrefab;
    public GameObject ghostForestHutPrefab;
    public GameObject ghostSawmillPrefab;
    public GameObject ghostWarehousePrefab;
    public GameObject ghostHouseBuilderPrefab;

    // ================= PRIVATE =================

    private GhostBuilding currentGhost;
    private bool isPlacing = false;

    // ================= PROPERTIES =================

    public bool IsPlacing => isPlacing;

    // ================= LIFECYCLE =================

    void Update()
    {
        if (!isPlacing) return;

        // Huỷ bằng ESC khi đang đặt
        if (Input.GetKeyDown(KeyCode.Escape))
            CancelPlacing();
    }

    // ================= PUBLIC – UI GỌI =================

    /// <summary>
    /// Bắt đầu đặt công trình – UI button gọi hàm này
    /// VD: BuildingSystem.Ins.StartPlacing(BuildingType.House);
    /// </summary>
    public void StartPlacing(BuildingType type)
    {
        // Huỷ ghost cũ nếu đang có
        CancelPlacing();

        // Kiểm tra xem vị trí có hợp lệ không
        if (!BuildingManager.Ins.CanBuild(currentGhost.transform.position, type))
        {
            Debug.LogWarning("[BuildingSystem] Không thể đặt công trình ở vị trí này vì có sự chồng lấn.");
            return;
        }

        GameObject prefab = GetGhostPrefab(type);

        if (prefab == null)
        {
            Debug.LogWarning($"[BuildingSystem] Chưa gán ghost prefab: {type}");
            return;
        }

        GameObject obj = Instantiate(prefab);
        currentGhost = obj.GetComponent<GhostBuilding>();

        if (currentGhost == null)
        {
            Debug.LogWarning($"[BuildingSystem] Không tìm thấy GhostBuilding cho prefab: {type}");
            return;
        }

        currentGhost.Show();
        isPlacing = true;

        // Thông báo UIManager ẩn menu building
        // UIManager.Ins?.HideBuildingMenu();
    }

    /// <summary>Huỷ đặt công trình hiện tại</summary>
    public void CancelPlacing()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost.gameObject);
            currentGhost = null;
        }

        isPlacing = false;
    }

    /// <summary>Gọi từ GhostBuilding khi đặt xong</summary>
    public void OnPlacingCompleted()
    {
        currentGhost = null;
        isPlacing = false;

        // Thông báo UIManager hiện lại HUD
        // UIManager.Ins?.ShowHUD();
    }

    // ================= PUBLIC – SAVE / LOAD =================

    /// <summary>Lưu toàn bộ building → gọi khi người chơi save game</summary>
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
            ? $"[BuildingSystem] ✅ Lưu {states.Count} công trình thành công!"
            : "[BuildingSystem] ❌ Lưu thất bại!");
    }

    /// <summary>Tải building từ save → gọi khi vào game</summary>
    public void LoadBuildings()
    {
        var saveData = JsonDataManager.Ins.LoadGame();

        if (saveData == null || saveData.buildings == null || saveData.buildings.Count == 0)
        {
            Debug.Log("[BuildingSystem] Chưa có save hoặc không có công trình.");
            return;
        }

        BuildingManager.Ins.LoadStates(saveData.buildings);
        Debug.Log($"[BuildingSystem] ✅ Tải {saveData.buildings.Count} công trình thành công!");
    }

    // ================= PRIVATE =================

    private GameObject GetGhostPrefab(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.House: return ghostHousePrefab;
            case BuildingType.ForestHut: return ghostForestHutPrefab;
            case BuildingType.Sawmill: return ghostSawmillPrefab;
            case BuildingType.Warehouse: return ghostWarehousePrefab;
            case BuildingType.HouseBuilder: return ghostHouseBuilderPrefab;
            default: return null;
        }
    }
}