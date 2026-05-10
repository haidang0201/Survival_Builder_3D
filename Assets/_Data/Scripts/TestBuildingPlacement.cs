using UnityEngine;
using System.Collections.Generic;

/*
 * TestBuildingPlacement.cs
 * Folder: Scripts/Building/
 * Người làm: DŨNG (test)
 *
 * Test toàn bộ luồng: đặt công trình → xoay → save → load
 *
 * Điều khiển:
 *   1~4         → Spawn ghost loại tương ứng
 *   R           → Xoay 90° (khi đang giữ ghost)
 *   Click trái  → Đặt công trình
 *   Click phải  → Huỷ
 *   SPACE       → Lưu JSON
 *   L           → Tải JSON
 *   C           → Xóa save
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
        // List<BuildingState> đồng bộ với JsonDataManager.GameSaveData.buildings
        List<BuildingState> states = BuildingManager.Ins.GetAllStates();

        var saveData = new JsonDataManager.GameSaveData
        {
            sceneName = "MainGame",
            savedAtUnix = System.DateTimeOffset.Now.ToUnixTimeSeconds(),
            buildings = states,                       // List<BuildingState> ✅
            resources = new List<ResourceData>()
        };

        bool result = JsonDataManager.Ins.SaveGame(saveData);

        if (result)
        {
            Debug.Log("===========================================");
            Debug.Log("[Test] ✅ LƯU THÀNH CÔNG!");
            Debug.Log($"  Số công trình: {states.Count}");
            foreach (var s in states)
                Debug.Log($"  🏠 {s.buildingType,-15} | Rot Y: {s.rotation.y}° | Built: {s.isBuilt}");
            Debug.Log("===========================================");
        }
        else
        {
            Debug.LogError("[Test] ❌ LƯU THẤT BẠI!");
        }
    }

    private void TestLoad()
    {
        JsonDataManager.GameSaveData loaded = JsonDataManager.Ins.LoadGame();

        if (loaded == null)
        {
            Debug.LogError("[Test] ❌ TẢI THẤT BẠI! Chưa có file save.");
            return;
        }

        BuildingManager.Ins.LoadStates(loaded.buildings); // List<BuildingState> ✅

        Debug.Log("===========================================");
        Debug.Log("[Test] ✅ TẢI THÀNH CÔNG!");
        Debug.Log($"  Số công trình: {loaded.buildings.Count}");
        foreach (var s in loaded.buildings)
            Debug.Log($"  🏠 {s.buildingType,-15} | Rot Y: {s.rotation.y}° | Built: {s.isBuilt}");
        Debug.Log("===========================================");
    }

    private void TestClear()
    {
        bool result = JsonDataManager.Ins.DeleteSave();
        Debug.Log(result
            ? "[Test] 🗑️ XÓA SAVE THÀNH CÔNG!"
            : "[Test] ❌ Chưa có file save!");
    }

    // ================= LOG =================

    private void PrintGuide()
    {
        Debug.Log("===========================================");
        Debug.Log("[TestBuildingPlacement] Hướng dẫn:");
        Debug.Log("  1 → House | 2 → ForestHut | 3 → Sawmill | 4 → Warehouse");
        Debug.Log("  R           → Xoay 90°");
        Debug.Log("  Click trái  → Đặt");
        Debug.Log("  Click phải  → Huỷ");
        Debug.Log("  SPACE → Lưu | L → Tải | C → Xóa save");
        Debug.Log("===========================================");
    }
}