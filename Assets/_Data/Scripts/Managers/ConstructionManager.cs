using UnityEngine;
using System.Collections.Generic;

/*
 * ConstructionManager.cs
 * Folder: Scripts/Building/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Người thực hiện: VŨ + ĐĂNG
 * ĐÃ CẬP NHẬT: Đồng bộ trọn bộ 11 loại công trình chuẩn theo Enum và BuildingSystem.
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
    public GameObject stoneStoragePrefab; // MỚI BỔ SUNG: Kho đá thật
    public GameObject warehousePrefab;    // MỚI BỔ SUNG: Nhà kho tổng thật

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

        // LOG KIỂM TRA TÀI NGUYÊN TRƯỚC KHI TRỪ
        if (JsonDataManager.Ins != null)
        {
            Debug.Log($"[CHECK] Tài nguyên TRƯỚC khi xây: Gỗ={JsonDataManager.Ins.wood}, Đá={JsonDataManager.Ins.stone}, Thức ăn={JsonDataManager.Ins.food} | Chi phí xây: Gỗ={cost.woodCost}, Đá={cost.stoneCost}, Thức ăn={cost.foodCost}");
        }

        // 3. Kiểm tra và trừ tài nguyên
        bool canBuild = false;

        if (DialogNPC.Instance != null)
        {
            canBuild = DialogNPC.Instance.Consume(cost.woodCost, cost.foodCost, cost.stoneCost);

            if (canBuild && JsonDataManager.Ins != null)
            {
                JsonDataManager.Ins.BroadcastAllResources();
            }
        }
        else
        {
            if (JsonDataManager.Ins != null)
            {
                if (JsonDataManager.Ins.wood >= cost.woodCost &&
                    JsonDataManager.Ins.food >= cost.foodCost &&
                    JsonDataManager.Ins.stone >= cost.stoneCost)
                {
                    JsonDataManager.Ins.AddWood(-cost.woodCost);
                    JsonDataManager.Ins.AddFood(-cost.foodCost);
                    JsonDataManager.Ins.AddStone(-cost.stoneCost);
                    canBuild = true;
                }
            }
        }

        if (!canBuild)
        {
            Debug.LogWarning("[ConstructionManager] Không đủ tài nguyên để xây " + type);
            return;
        }

        // LOG XÁC NHẬN ĐÃ TRỪ TÀI NGUYÊN THÀNH CÔNG VÀ LÊN HUD
        if (JsonDataManager.Ins != null)
        {
            Debug.Log($"[XÁC NHẬN] Đã trừ tài nguyên! Tài nguyên HIỆN TẠI: Gỗ={JsonDataManager.Ins.wood}, Đá={JsonDataManager.Ins.stone}, Thức ăn={JsonDataManager.Ins.food}");
        }

        // 4. Đủ tiền -> Tiến hành sinh nhà thật
        var spawned = SpawnBuilding(type, position, rotation);

        if (spawned == null)
        {
            Debug.LogError($"[ConstructionManager] Chưa gán prefab cho: {type}");
        }
        else
        {
            JsonDataManager.RegisterStat_BuildingConstructed();
            Debug.Log($"[ConstructionManager] ✅ Đặt {type} thành công | Đã phát Event lên HUD.");
        }
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
            // Nhóm Dân sự
            case BuildingType.House: return housePrefab;
            case BuildingType.WoodCutter: return woodCutterPrefab;
            case BuildingType.StoneMine: return stoneMinePrefab;
            case BuildingType.Kitchen: return kitchenPrefab;
            case BuildingType.FoodStorage: return foodStoragePrefab;
            case BuildingType.StoneStorage: return stoneStoragePrefab; // MỚI BỔ SUNG
            case BuildingType.Warehouse: return warehousePrefab;       // MỚI BỔ SUNG

            // Nhóm Phòng thủ
            case BuildingType.WatchTower: return watchTowerPrefab;
            case BuildingType.ArcherTower: return archerTowerPrefab;
            case BuildingType.Cannon: return cannonPrefab;

            // Nhóm Quân sự
            case BuildingType.BarracksMelee: return barracksMeleePrefab;
            case BuildingType.BarracksArcher: return barracksArcherPrefab;
            case BuildingType.BarracksSpear: return barracksSpearPrefab;

            default:
                Debug.LogWarning($"[ConstructionManager] Không có case cho: {type}");
                return null;
        }
    }
}