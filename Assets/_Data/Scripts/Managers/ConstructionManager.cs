using UnityEngine;

/*
 * ConstructionManager.cs
 * Folder: Scripts/Managers/
 * Người làm: DŨNG / VŨ
 *
 * Spawn prefab building thật vào scene
 * Singleton – gắn vào Scene Master bởi ĐĂNG
 *
 * PlaceBuilding() → gọi từ GhostBuilding khi người chơi xác nhận đặt
 * SpawnBuilding() → gọi từ BuildingManager.LoadStates() khi load JSON
 */

public class ConstructionManager : Singleton<ConstructionManager>
{
    // ================= INSPECTOR =================

    [Header("Prefab thật – kéo vào đây")]
    public GameObject housePrefab;
    public GameObject forestHutPrefab;
    public GameObject sawmillPrefab;
    public GameObject warehousePrefab;
    public GameObject houseBuilderPrefab;

    // ================= PUBLIC – ĐẶT MỚI =================

    /// <summary>
    /// Gọi từ GhostBuilding.ConfirmPlace() khi người chơi đặt công trình
    /// </summary>
    public void PlaceBuilding(BuildingType type, Vector3 position, Quaternion rotation)
    {
        var spawned = SpawnBuilding(type, position, rotation);

        if (spawned == null)
            Debug.LogError($"[ConstructionManager] Chưa gán prefab cho: {type}");
        else
            Debug.Log($"[ConstructionManager] ✅ Đặt {type} | Pos: {position} | Rot: {rotation.eulerAngles.y}°");
    }

    // ================= PUBLIC – SPAWN (dùng chung) =================

    /// <summary>
    /// Spawn prefab thật → trả về BuildingCtrl
    /// Dùng cho cả PlaceBuilding() và BuildingManager.LoadStates()
    /// </summary>
    public BuildingCtrl SpawnBuilding(BuildingType type, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = GetPrefab(type);

        if (prefab == null) return null;

        GameObject obj = Instantiate(prefab, position, rotation);
        obj.name = type.ToString(); // "House", "Sawmill"... không có "(Clone)"

        return obj.GetComponent<BuildingCtrl>();
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