using UnityEngine;

/*
 * ConstructionManager.cs
 * Folder: Scripts/Managers/
 * Người làm: DŨNG / VŨ
 *
 * Nhận lệnh từ GhostBuilding → spawn prefab building thật vào scene
 * Singleton – gắn vào Scene Master bởi ĐĂNG
 *
 * Luồng:
 *   TestBuildingPlacement (nhấn phím) → GhostBuilding (preview + xoay)
 *   → ConfirmPlace() → ConstructionManager.PlaceBuilding() → Instantiate prefab
 *   → BuildingCtrl.Start() → BuildingManager.AddBuilding()
 */

public class ConstructionManager : Singleton<ConstructionManager>
{
    // ================= INSPECTOR =================

    [Header("Prefab References – kéo prefab vào đây")]
    public GameObject housePrefab;
    public GameObject forestHutPrefab;
    public GameObject sawmillPrefab;
    public GameObject warehousePrefab;
    public GameObject houseBuilderPrefab;

    // ================= PUBLIC =================

    /// <summary>
    /// Spawn building thật vào scene
    /// Gọi từ GhostBuilding.ConfirmPlace()
    /// </summary>
    public void PlaceBuilding(BuildingType type, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = GetPrefab(type);

        if (prefab == null)
        {
            Debug.LogError($"[ConstructionManager] Chưa gán prefab cho: {type}");
            return;
        }

        GameObject obj = Instantiate(prefab, position, rotation);
        obj.name = type.ToString(); // đặt tên để BuildingManager tìm khi load save

        Debug.Log($"[ConstructionManager] ✅ Đã đặt {type} | Pos: {position} | Rot: {rotation.eulerAngles.y}°");
    }

    // ================= PRIVATE =================

    private GameObject GetPrefab(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.House: return housePrefab;
            case BuildingType.ForestHut: return forestHutPrefab;
            case BuildingType.Sawmill: return sawmillPrefab;
            case BuildingType.Warehouse: return warehousePrefab;
            case BuildingType.HouseBuilder: return houseBuilderPrefab;
            default:
                Debug.LogWarning($"[ConstructionManager] Không có case cho: {type}");
                return null;
        }
    }
}