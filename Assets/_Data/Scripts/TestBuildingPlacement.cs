using UnityEngine;

/*
 * TestBuildingPlacement.cs
 * Folder: Scripts/Testing/
 * Người làm: VŨ
 *
 * Script test nhanh toàn bộ loại công trình bằng phím tắt.
 * CHỈ dùng trong Editor / môi trường test – KHÔNG đưa vào build cuối.
 *
 * Hướng dẫn:
 *   [1] Nhà Dân        [2] Trại Mộc      [3] Mỏ Đá
 *   [4] Nhà Bếp        [5] Kho Lúa       [6] Kho Đá
 *   [7] Tháp Canh      [8] Tháp Cung     [9] Pháo
 *   [Q] Lính Cận Chiến [W] Lính Cung     [E] Lính Giáo
 *   R           → Xoay 90°
 *   Chuột Trái  → Đặt công trình
 *   Chuột Phải  → Huỷ thao tác
 */

public class TestBuildingPlacement : MonoBehaviour
{
    // ================= INSPECTOR =================

    [Header("Ghost Prefabs – Dân sự")]
    public GameObject ghostHousePrefab;
    public GameObject ghostWoodCutterPrefab;
    public GameObject ghostStoneMinePrefab;
    public GameObject ghostKitchenPrefab;
    public GameObject ghostFoodStoragePrefab;
    public GameObject ghostStoneStoragePrefab;

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

    // ================= LIFECYCLE =================

    private void Start() => PrintGuide();

    private void Update() => HandleSpawnInput();

    // ================= SPAWN INPUT =================

    private void HandleSpawnInput()
    {
        // Dân sự
        if (Input.GetKeyDown(KeyCode.Alpha1)) SpawnGhost(BuildingType.House, ghostHousePrefab);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SpawnGhost(BuildingType.WoodCutter, ghostWoodCutterPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SpawnGhost(BuildingType.StoneMine, ghostStoneMinePrefab);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SpawnGhost(BuildingType.Kitchen, ghostKitchenPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SpawnGhost(BuildingType.FoodStorage, ghostFoodStoragePrefab);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SpawnGhost(BuildingType.StoneStorage, ghostStoneStoragePrefab);

        // Phòng thủ
        if (Input.GetKeyDown(KeyCode.Alpha7)) SpawnGhost(BuildingType.WatchTower, ghostWatchTowerPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SpawnGhost(BuildingType.ArcherTower, ghostArcherTowerPrefab);
        if (Input.GetKeyDown(KeyCode.Alpha9)) SpawnGhost(BuildingType.Cannon, ghostCannonPrefab);

        // Quân sự
        if (Input.GetKeyDown(KeyCode.Q)) SpawnGhost(BuildingType.BarracksMelee, ghostBarracksMeleePrefab);
        if (Input.GetKeyDown(KeyCode.W)) SpawnGhost(BuildingType.BarracksArcher, ghostBarracksArcherPrefab);
        if (Input.GetKeyDown(KeyCode.E)) SpawnGhost(BuildingType.BarracksSpear, ghostBarracksSpearPrefab);
    }

    private void SpawnGhost(BuildingType type, GameObject prefab)
    {
        // Huỷ ghost đang có
        if (currentGhost != null)
            Destroy(currentGhost.gameObject);

        if (prefab == null)
        {
            Debug.LogError($"[TestPlacement] ❌ Chưa gán Ghost Prefab cho {type} trong Inspector!");
            return;
        }

        GameObject obj = Instantiate(prefab);
        currentGhost = obj.GetComponent<GhostBuilding>();

        if (currentGhost == null)
        {
            Debug.LogError($"[TestPlacement] ❌ Prefab {type} thiếu component GhostBuilding!");
            Destroy(obj);
            return;
        }

        currentGhost.buildingType = type;
        currentGhost.Show();
        Debug.Log($"[TestPlacement] 🔨 Đang chọn xây: {type}");
    }

    // ================= LOG =================

    private void PrintGuide()
    {
        Debug.Log("===========================================");
        Debug.Log("[TestBuildingPlacement] Hướng dẫn Test:");
        Debug.Log("  [1] Nhà Dân        | [2] Trại Mộc    | [3] Mỏ Đá");
        Debug.Log("  [4] Nhà Bếp        | [5] Kho Lúa     | [6] Kho Đá");
        Debug.Log("  [7] Tháp Canh      | [8] Tháp Cung   | [9] Pháo");
        Debug.Log("  [Q] Lính Cận Chiến | [W] Lính Cung   | [E] Lính Giáo");
        Debug.Log("-------------------------------------------");
        Debug.Log("  R          → Xoay 90°");
        Debug.Log("  Chuột Trái → Đặt công trình");
        Debug.Log("  Chuột Phải → Huỷ thao tác");
        Debug.Log("===========================================");
    }
}