using UnityEngine;

/*
 * BuildingSystem.cs
 * Folder: Scripts/Building/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện: VŨ (Luồng UI) + DŨNG (Logic Save/Load) + ĐĂNG (Kiến trúc & Tối ưu Ghost)
 *
 * NHIỆM VỤ: Quản lý vòng đời chế độ xây dựng, sinh/hủy Ghost và đồng bộ trạng thái với UI.
 */

public class BuildingSystem : Singleton<BuildingSystem>
{
    // ================= INSPECTOR (GIỮ NGUYÊN ĐỂ KHÔNG MẤT FILE KÉO THẢ) =================

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

    // ================= PRIVATE STATE =================

    private GhostBuilding currentGhost;
    private bool isPlacing = false;

    public bool IsPlacing => isPlacing;

    // ================= PUBLIC INTERFACE – UI / GAMEPLAY GỌI =================

    /// <summary>
    /// Bắt đầu chế độ đặt công trình. Được gọi từ các nút bấm trên UI.
    /// </summary>
    public void StartPlacing(BuildingType type)
    {
        if (type == BuildingType.None) return;

        CancelPlacing();

        GameObject prefab = GetGhostPrefab(type);
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab);
        currentGhost = obj.GetComponent<GhostBuilding>();

        if (currentGhost == null)
        {
            Destroy(obj);
            return;
        }

        currentGhost.buildingType = type;

        // [THÊM MỚI TẠI ĐÂY]: Ép Ghost cập nhật tọa độ theo chuột ngay lập tức tại Frame 0
        currentGhost.InstantSnapToMouse();

        isPlacing = true;

        if (UIManager.Ins != null)
        {
            UIManager.Ins.EnterPlacementMode();
        }
    }

    /// <summary>
    /// Hủy đặt công trình hiện tại một cách chủ động từ code hệ thống.
    /// </summary>
    public void CancelPlacing()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost.gameObject);
            currentGhost = null;
        }

        isPlacing = false;

        if (UIManager.Ins != null)
        {
            UIManager.Ins.ExitPlacementMode(true); // Mở lại menu khi bị hủy từ xa
        }
    }

    /// <summary>
    /// Hàm nhận callback từ GhostBuilding khi người chơi đã click đặt thành công HOẶC bấm hủy (ESC / Chuột phải).
    /// </summary>
    public void OnPlacingCompleted(bool shouldReopenMenu)
    {
        currentGhost = null;
        isPlacing = false;

        // Truyền trạng thái đóng/mở menu sang cho UIManager
        if (UIManager.Ins != null)
        {
            UIManager.Ins.ExitPlacementMode(shouldReopenMenu);
        }
    }

    // ================= PUBLIC – LOGIC SAVE / LOAD (DŨNG CHUẨN HÓA) =================

    public void SaveBuildings()
    {
        var states = BuildingManager.Ins.GetAllStates();

        if (states.Count == 0)
        {
            Debug.LogWarning("[BuildingSystem] Không có công trình nào trong màn chơi để lưu!");
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
        Debug.Log(result ? $"[BuildingSystem] ✅ Đã lưu {states.Count} công trình thành công." : "[BuildingSystem] ❌ Lưu dữ liệu thất bại!");
    }

    public void LoadBuildings()
    {
        var saveData = JsonDataManager.Ins.LoadGame();

        if (saveData == null || saveData.buildings == null || saveData.buildings.Count == 0)
        {
            Debug.Log("[BuildingSystem] Không tìm thấy dữ liệu cũ hoặc không có công trình nào được lưu.");
            return;
        }

        BuildingManager.Ins.LoadStates(saveData.buildings);
        Debug.Log($"[BuildingSystem] ✅ Đã phục hồi {saveData.buildings.Count} công trình từ File Save.");
    }

    // ================= MAPPER PREFAB (HỖ TRỢ TRỌN BỘ 11 LOẠI NHÀ ĐÚNG ENUM) =================

    private GameObject GetGhostPrefab(BuildingType type)
    {
        switch (type)
        {
            // Nhóm Dân sự
            case BuildingType.House: return ghostHousePrefab;
            case BuildingType.WoodCutter: return ghostWoodCutterPrefab;
            case BuildingType.StoneMine: return ghostStoneMinePrefab;
            case BuildingType.Kitchen: return ghostKitchenPrefab;
            case BuildingType.FoodStorage: return ghostFoodStoragePrefab;
            case BuildingType.StoneStorage: return ghostStoneStoragePrefab;
            case BuildingType.Warehouse: return ghostWarehousePrefab;

            // Nhóm Phòng thủ
            case BuildingType.WatchTower: return ghostWatchTowerPrefab;
            case BuildingType.ArcherTower: return ghostArcherTowerPrefab;
            case BuildingType.Cannon: return ghostCannonPrefab;

            // Nhóm Quân sự
            case BuildingType.BarracksMelee: return ghostBarracksMeleePrefab;
            case BuildingType.BarracksArcher: return ghostBarracksArcherPrefab;
            case BuildingType.BarracksSpear: return ghostBarracksSpearPrefab;

            default:
                return null;
        }
    }
}