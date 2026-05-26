using UnityEngine;

/*
 * ConstructionManager.cs
 * Đã cập nhật: Đồng bộ danh sách 11 công trình thật phục vụ sinh nhà chính thức và Load Game.
 */

public class ConstructionManager : Singleton<ConstructionManager>
{
    // ================= INSPECTOR =================

    [Header("Prefab thật - Dân sự")]
    public GameObject housePrefab;
    public GameObject woodCutterPrefab;
    public GameObject stoneMinePrefab;
    public GameObject kitchenPrefab;
    public GameObject foodStoragePrefab;

    [Header("Prefab thật - Phòng thủ")]
    public GameObject watchTowerPrefab;
    public GameObject archerTowerPrefab;
    public GameObject cannonPrefab;

    [Header("Prefab thật - Quân sự (Nhà lính)")]
    public GameObject barracksMeleePrefab;
    public GameObject barracksArcherPrefab;
    public GameObject barracksSpearPrefab;

    // ================= PUBLIC – ĐẶT MỚI =================

    public void PlaceBuilding(BuildingType type, Vector3 position, Quaternion rotation)
    {
        if (!BuildingManager.Ins.CanBuild(position, type))
        {
            Debug.LogWarning($"[ConstructionManager] Không thể xây dựng tại vị trí này vì có sự chồng lấn.");
            return;
        }

        var spawned = SpawnBuilding(type, position, rotation);

        if (spawned == null)
            Debug.LogError($"[ConstructionManager] Chưa gán prefab cho: {type}");
        else
            Debug.Log($"[ConstructionManager] ✅ Đặt {type} | Pos: {position} | Rot: {rotation.eulerAngles.y}°");
    }

    // ================= PUBLIC – SPAWN =================

    public BuildingCtrl SpawnBuilding(BuildingType type, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = GetPrefab(type);

        if (prefab == null) return null;

        GameObject obj = Instantiate(prefab, position, rotation);
        obj.name = type.ToString();

        return obj.GetComponent<BuildingCtrl>();
    }

    // ================= PRIVATE =================

    private GameObject GetPrefab(BuildingType type)
    {
        switch (type)
        {
            case BuildingType.House: return housePrefab;
            case BuildingType.WoodCutter: return woodCutterPrefab;
            case BuildingType.StoneMine: return stoneMinePrefab;
            case BuildingType.Kitchen: return kitchenPrefab;
            case BuildingType.FoodStorage: return foodStoragePrefab;

            case BuildingType.WatchTower: return watchTowerPrefab;
            case BuildingType.ArcherTower: return archerTowerPrefab;
            case BuildingType.Cannon: return cannonPrefab;

            case BuildingType.BarracksMelee: return barracksMeleePrefab;
            case BuildingType.BarracksArcher: return barracksArcherPrefab;
            case BuildingType.BarracksSpear: return barracksSpearPrefab;

            default:
                Debug.LogWarning($"[ConstructionManager] Không có case cho: {type}");
                return null;
        }
    }
}