using UnityEngine;
using System.Collections.Generic;

/*
 * TestBuildingPlacement.cs
 * Folder: Scripts/Building/
 * Người làm: DŨNG (test)
 *
 * Điều khiển:
 *   1~4         → Spawn ghost loại tương ứng
 *   R           → Xoay 90°
 *   Click trái  → Đặt công trình
 *   Click phải  → Huỷ
 *   SPACE       → Lưu JSON (chỉ lưu khi nhấn)
 *   L           → Tải JSON (xóa hết hiện tại, load về trạng thái đã lưu)
 *   C           → Xóa file save
 *
 * Nguyên tắc:
 *   - Chưa nhấn SPACE → chưa lưu → nhấn L sẽ báo chưa có save, KHÔNG xóa nhà
 *   - Đã nhấn SPACE → nhấn L → xóa hết nhà hiện tại → load đúng save
 */

public class TestBuildingPlacement : MonoBehaviour
{
    // ================= INSPECTOR =================

    [Header("Ghost Prefabs")]
    public GameObject ghostHousePrefab;
    public GameObject ghostForestHutPrefab;
    public GameObject ghostSawmillPrefab;
    public GameObject ghostWarehousePrefab;

    // ================= PRIVATE =================

    private GhostBuilding currentGhost;

    // ================= LIFECYCLE =================

    void Start() => PrintGuide();

    void Update()
    {
        HandleSpawnInput();
        HandleSaveLoadInput();
    }

    // ================= SPAWN =================

    private void HandleSpawnInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SpawnGhost(BuildingType.House, ghostHousePrefab);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SpawnGhost(BuildingType.ForestHut, ghostForestHutPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SpawnGhost(BuildingType.Sawmill, ghostSawmillPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SpawnGhost(BuildingType.Warehouse, ghostWarehousePrefab);
    }

    private void SpawnGhost(BuildingType type, GameObject prefab)
    {
        if (currentGhost != null)
            Destroy(currentGhost.gameObject);

        if (prefab == null)
        {
            Debug.LogWarning($"[Test] Chưa gán ghost prefab: {type}");
            return;
        }

        var obj = Instantiate(prefab);
        currentGhost = obj.GetComponent<GhostBuilding>();

        if (currentGhost == null)
        {
            Debug.LogError("[Test] Ghost prefab thiếu GhostBuilding.cs!");
            Destroy(obj);
            return;
        }

        currentGhost.buildingType = type;
        Debug.Log($"[Test] 👻 {type} | R=xoay | Click trái=đặt | Click phải=huỷ");
    }

    // ================= SAVE / LOAD =================

    private void HandleSaveLoadInput()
    {
        if (Input.GetKeyDown(KeyCode.Space)) TestSave();
        if (Input.GetKeyDown(KeyCode.L)) TestLoad();
        if (Input.GetKeyDown(KeyCode.C)) TestClear();
    }

    private void TestSave()
    {
        List<BuildingState> states = BuildingManager.Ins.GetAllStates();

        if (states.Count == 0)
        {
            Debug.LogWarning("[Test] ⚠️ Không có công trình nào để lưu!");
            return;
        }

        var saveData = new JsonDataManager.GameSaveData
        {
            sceneName = "MainGame",
            savedAtUnix = System.DateTimeOffset.Now.ToUnixTimeSeconds(),
            buildings = states,
            resources = new List<ResourceData>()
        };

        bool result = JsonDataManager.Ins.SaveGame(saveData);

        if (result)
        {
            Debug.Log("===========================================");
            Debug.Log("[Test] ✅ LƯU THÀNH CÔNG!");
            Debug.Log($"  Số công trình đã lưu: {states.Count}");
            foreach (var s in states)
                Debug.Log($"  🏠 {s.buildingType,-15} | Rot Y: {s.rotation.y}°");
            Debug.Log("===========================================");
        }
        else
        {
            Debug.LogError("[Test] ❌ LƯU THẤT BẠI!");
        }
    }

    private void TestLoad()
    {
        // ✅ Load trước – kiểm tra có file hợp lệ không
        JsonDataManager.GameSaveData loaded = JsonDataManager.Ins.LoadGame();

        // ✅ Nếu chưa có save → KHÔNG xóa nhà hiện tại
        if (loaded == null || loaded.buildings == null || loaded.buildings.Count == 0)
        {
            Debug.LogWarning("[Test] ⚠️ Chưa có file save! Nhấn SPACE để lưu trước.");
            return;
        }

        // ✅ Có save → xóa hết nhà hiện tại (kể cả chưa lưu) → load về save
        BuildingManager.Ins.LoadStates(loaded.buildings);

        Debug.Log("===========================================");
        Debug.Log("[Test] ✅ TẢI THÀNH CÔNG! Nhà chưa lưu đã bị xóa.");
        Debug.Log($"  Số công trình: {loaded.buildings.Count}");
        foreach (var s in loaded.buildings)
            Debug.Log($"  🏠 {s.buildingType,-15} | Rot Y: {s.rotation.y}° | Built: {s.isBuilt}");
        Debug.Log("===========================================");
    }

    private void TestClear()
    {
        bool result = JsonDataManager.Ins.DeleteSave();
        Debug.Log(result
            ? "[Test] 🗑️ XÓA FILE SAVE THÀNH CÔNG!"
            : "[Test] ⚠️ Chưa có file save để xóa!");
    }

    // ================= LOG =================

    private void PrintGuide()
    {
        Debug.Log("===========================================");
        Debug.Log("[TestBuildingPlacement] Hướng dẫn:");
        Debug.Log("  1~4   → Chọn loại công trình");
        Debug.Log("  R     → Xoay 90°");
        Debug.Log("  Click trái  → Đặt | Click phải → Huỷ");
        Debug.Log("  SPACE → Lưu | L → Load về save | C → Xóa save");
        Debug.Log("===========================================");
    }
}