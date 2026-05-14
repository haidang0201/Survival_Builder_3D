using System.Collections;
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
        CancelPlacing();

        // CHÚ Ý: Đã xoá đoạn if (!BuildingManager.Ins.CanBuild(currentGhost...)) ở đây.
        // Vì lúc này currentGhost chưa được tạo (bằng null), gọi .transform sẽ văng lỗi đỏ.
        // Việc check va chạm đã được GhostBuilding lo liệu rất tốt rồi.

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
        currentGhost.SetInitialRotation(0);
    }
    // THÊM MỚI: Thay thế cho PlaceBuildingWithDelay
    public void StartConstruction(BuildingType type, Vector3 position, Quaternion rotation, GameObject ghostObj)
    {
        // Nhả currentGhost ra để người chơi có thể tiếp tục bấm UI xây thêm căn nhà khác
        // trong lúc căn nhà này đang đếm ngược 10s.
        if (currentGhost != null && currentGhost.gameObject == ghostObj)
        {
            currentGhost = null;
            isPlacing = false;
        }

        StartCoroutine(ConstructionRoutine(type, position, rotation, ghostObj));
    }

    private IEnumerator ConstructionRoutine(BuildingType type, Vector3 position, Quaternion rotation, GameObject ghostObj)
    {
        yield return new WaitForSeconds(5f); // Đợi 10 giây

        // Hết 10s: Xoá bản preview mờ
        if (ghostObj != null)
        {
            Destroy(ghostObj);
        }

        // Gọi logic của Dũng để spawn nhà thật (hiện rõ) vào scene
        ConstructionManager.Ins.PlaceBuilding(type, position, rotation);
    }
    public void PlaceBuildingWithDelay(BuildingType type, Vector3 position, Quaternion rotation)
    {
        StartCoroutine(PlaceBuildingAfterDelay(type, position, rotation));
    }

    private IEnumerator PlaceBuildingAfterDelay(BuildingType type, Vector3 position, Quaternion rotation)
    {
        yield return new WaitForSeconds(5f); // Đợi 10 giây

        ConstructionManager.Ins.PlaceBuilding(type, position, rotation);
        currentGhost = null; // Đặt lại currentGhost
        isPlacing = false; // Cập nhật trạng thái đặt
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
            resources = new System.Collections.Generic.List<ResourceData>()
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