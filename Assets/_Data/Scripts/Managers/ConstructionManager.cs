using UnityEngine;
using System.Collections.Generic;

/*
 * ConstructionManager.cs
 * Đã cập nhật: Tích hợp cấu hình chi phí và khấu trừ tài nguyên khi xây 11 loại nhà.
 */

public class ConstructionManager : Singleton<ConstructionManager>
{
    [System.Serializable]
    public struct BuildingCost
    {
        public BuildingType buildingType;
        public int woodCost;
        public int stoneCost;
        public int foodCost; // Đồng bộ tên food giống JsonDataManager
    }

    // ================= INSPECTOR =================

    [Header("Cấu hình chi phí xây dựng nhà")]
    public List<BuildingCost> constructionCosts = new List<BuildingCost>();

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

    // Hàm phụ trợ để lấy nhanh chi phí của một loại nhà
    public BuildingCost GetBuildingCost(BuildingType type)
    {
        foreach (var cost in constructionCosts)
        {
            if (cost.buildingType == type) return cost;
        }
        return new BuildingCost { buildingType = type, woodCost = 0, stoneCost = 0, foodCost = 0 };
    }

    // ================= PUBLIC – ĐẶT MỚI =================

    public void PlaceBuilding(BuildingType type, Vector3 position, Quaternion rotation)
    {
        // 1. Kiểm tra vị trí xem có trống không
        if (!BuildingManager.Ins.CanBuild(position, type))
        {
            Debug.LogWarning($"[ConstructionManager] Không thể xây dựng tại vị trí này vì có sự chồng lấn.");
            return;
        }

        // 2. Lấy cấu hình chi phí của nhà này
        BuildingCost cost = GetBuildingCost(type);

        // 3. Gọi ResourceManager để kiểm tra và trừ tài nguyên tài khoản
        if (ResourceManager.Instance != null)
        {
            // Truyền Wood, Rice (Food), Stone vào hàm Consume
            if (!ResourceManager.Instance.Consume(cost.woodCost, cost.foodCost, cost.stoneCost))
            {
                // Nếu không đủ tiền, hàm Consume tự báo Log và dừng đặt nhà tại đây
                return; 
            }
        }

        // 4. Đủ tiền -> Tiến hành sinh nhà thật
        var spawned = SpawnBuilding(type, position, rotation);

        if (spawned == null)
            Debug.LogError($"[ConstructionManager] Chưa gán prefab cho: {type}");
        else
            Debug.Log($"[ConstructionManager] ✅ Đặt {type} thành công | Đã trừ tài nguyên.");
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