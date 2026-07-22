using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * BuildingSystem.cs
 * Folder: Scripts/Building/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện: VŨ (Luồng UI) + DŨNG (Logic Save/Load) + ĐĂNG (Kiến trúc & Tối ưu Ghost)
 *
 * NHIỆM VỤ: Quản lý vòng đời chế độ xây dựng, sinh/hủy Ghost, đồng bộ trạng thái với UI
 * và xử lý luồng di chuyển (Move) công trình đã xây.
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

    // Các biến phục vụ riêng cho tính năng di chuyển nhà
    private UpgradeableBuilding _movingBuilding = null; 
    private bool _isMovingMode = false;

    // Properties đầu ra cho các hệ thống khác check trạng thái bận
    public bool IsPlacing => isPlacing;
    public bool IsMovingMode => _isMovingMode;

    private void Update()
    {
        // Luôn lắng nghe lệnh click đặt hoặc hủy từ người chơi khi ở chế độ di chuyển
        if (_isMovingMode)
        {
            HandlePlacementInput();
        }
    }

    // ================= PUBLIC INTERFACE – UI / GAMEPLAY GỌI =================

    /// <summary>
    /// Bắt đầu chế độ đặt công trình xây mới. Được gọi từ các nút bấm trên UI.
    /// </summary>
    public void StartPlacing(BuildingType type)
    {
        if (type == BuildingType.None) return;

        // Nếu đang di chuyển hoặc đang đặt nhà khác, dọn dẹp trước khi bắt đầu cái mới
        if (_isMovingMode) CancelMoving();
        else CancelPlacing();

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

        // Ép Ghost cập nhật tọa độ theo chuột ngay lập tức tại Frame 0
        currentGhost.InstantSnapToMouse();

        isPlacing = true;

        if (UIManager.Ins != null)
        {
            UIManager.Ins.EnterPlacementMode();
        }
    }

    /// <summary>
    /// Hủy đặt công trình hiện tại một cách chủ động từ code hệ thống (Xây mới).
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
    /// Hàm nhận callback từ GhostBuilding khi người chơi đã click đặt thành công HOẶC bấm hủy (ESC / Chuột phải) lúc xây mới.
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

    public void StartMoving(UpgradeableBuilding building)
    {
        if (building == null) return;

        // Code nguyên bản của Vũ
        if (isPlacing) CancelPlacing();
        if (_isMovingMode) CancelMoving();

        _movingBuilding = building;
        _isMovingMode = true;

        _movingBuilding.gameObject.SetActive(false);

        BuildingType currentType = building.buildingType; 

        GameObject prefab = GetGhostPrefab(currentType);
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, building.transform.position, building.transform.rotation);
            currentGhost = obj.GetComponent<GhostBuilding>();
            if (currentGhost != null)
            {
                currentGhost.buildingType = currentType;
                
                // 🔥 ĐỒNG BỘ CẤP ĐỘ (Ý TƯỞNG CỦA VŨ)
                // Gọi hàm SetGhostLevel và truyền CurrentLevel của nhà thật sang
                currentGhost.SetGhostLevel(building.CurrentLevel);
                
                currentGhost.InstantSnapToMouse();
            }
        }

        if (UIManager.Ins != null)
        {
            UIManager.Ins.EnterPlacementMode();
        }
    }

    /// <summary>
    /// Hàm xử lý phím bấm và kiểm tra vị trí hợp lệ khi ĐẶT NHÀ XUỐNG VỊ TRÍ MỚI
    /// </summary>
    private void HandlePlacementInput()
    {
        // 1. CLICK CHUỘT TRÁI -> XÁC NHẬN ĐẶT NHÀ VÀO VỊ TRÍ MỚI
        if (Input.GetMouseButtonDown(0))
        {
            if (currentGhost == null || _movingBuilding == null) return;

            bool isValidPosition = true; 

            if (isValidPosition)
            {
                // Lấy tọa độ chuột đã được Snap Grid hoặc căn chỉnh từ Ghost đang kéo
                Vector3 newPosition = currentGhost.transform.position;
                Quaternion newRotation = currentGhost.transform.rotation;

                // Cập nhật vị trí và góc xoay mới cho công trình gốc
                _movingBuilding.transform.position = newPosition;
                _movingBuilding.transform.rotation = newRotation;

                // Hiện lại công trình thực tế tại vị trí mới
                _movingBuilding.gameObject.SetActive(true);

                // Dọn dẹp Ghost kéo đường
                if (currentGhost != null)
                {
                    Destroy(currentGhost.gameObject);
                    currentGhost = null;
                }

                // Tự động kích hoạt lưu lại vị trí mới vào Slot 1 mặc định
                SaveBuildingsToSlot(1);

                // Thoát hoàn toàn chế độ di chuyển
                EndMovingMode();
                Debug.Log($"[BuildingSystem] Đã dịch chuyển thành công [{_movingBuilding.buildingName}] đến vị trí mới.");
            }
            else
            {
                if (UIManager.Ins != null) 
                    UIManager.Ins.ShowWarning("Vị trí mới bị cản trở bởi vật thể khác, không thể đặt nhà!");
            }
        }

        // 2. CLICK CHUỘT PHẢI HOẶC BẤM ESC -> HỦY DI CHUYỂN, HOÀN TRẢ VỊ TRÍ CŨ
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelMoving();
        }
    }

    private void EndMovingMode()
    {
        _isMovingMode = false;
        _movingBuilding = null;
        currentGhost = null;

        if (UIManager.Ins != null)
        {
            UIManager.Ins.ExitPlacementMode(false); // Kết thúc dọn dẹp UI
        }
    }

    private void CancelMoving()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost.gameObject);
            currentGhost = null;
        }

        if (_movingBuilding != null)
        {
            // Bật lại nhà ở vị trí cũ ban đầu, giữ nguyên cấu trúc đồ họa
            _movingBuilding.gameObject.SetActive(true);
        }

        EndMovingMode();
        Debug.Log("[BuildingSystem] Người chơi đã hủy lệnh dời nhà. Đã hoàn trả về vị trí cũ.");
    }

    // ================= PUBLIC – LOGIC SAVE / LOAD THEO SLOT =================

    /// <summary>
    /// Hàm lưu mặc định (Tự động chọn Slot 1)
    /// </summary>
    public void SaveBuildings()
    {
        SaveBuildingsToSlot(1);
    }

    /// <summary>
    /// Hàm tải mặc định (Tự động chọn Slot 1)
    /// </summary>
    public void LoadBuildings()
    {
        LoadBuildingsFromSlot(1);
    }

    /// <summary>
    /// Lưu trạng thái công trình và tài nguyên vào Slot chọn sẵn (1, 2, 3...)
    /// </summary>
    public void SaveBuildingsToSlot(int slotIndex)
    {
        if (BuildingManager.Ins == null) return;
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

        bool result = JsonDataManager.Ins.SaveGame(slotIndex, saveData);
        Debug.Log(result ? $"[BuildingSystem] ✅ Đã lưu {states.Count} công trình vào Slot {slotIndex}." : $"[BuildingSystem] ❌ Lưu dữ liệu vào Slot {slotIndex} thất bại!");
    }

    /// <summary>
    /// Khôi phục trạng thái công trình và tài nguyên từ Slot chọn sẵn (1, 2, 3...)
    /// </summary>
    public void LoadBuildingsFromSlot(int slotIndex)
    {
        if (JsonDataManager.Ins == null || BuildingManager.Ins == null) return;
        var saveData = JsonDataManager.Ins.LoadGame(slotIndex);

        if (saveData == null || saveData.buildings == null || saveData.buildings.Count == 0)
        {
            Debug.LogWarning($"[BuildingSystem] Slot {slotIndex} không có dữ liệu hoặc file trống!");
            return;
        }

        BuildingManager.Ins.LoadStates(saveData.buildings);
        Debug.Log($"[BuildingSystem] ✅ Đã phục hồi {saveData.buildings.Count} công trình từ Slot {slotIndex}.");
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