using UnityEngine;
using System.Collections.Generic;
using TMPro; // ĐÃ THÊM: Thư viện TextMeshPro

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

        [Header("UI Text Hiển Thị Giá Riêng (Kéo thả TextMeshPro vào đây)")]
        public TextMeshProUGUI uiWoodText;  // Thay đổi thành TextMeshProUGUI
        public TextMeshProUGUI uiStoneText; // Thay đổi thành TextMeshProUGUI
        public TextMeshProUGUI uiFoodText;  // Thay đổi thành TextMeshProUGUI
    }

    // ================= INSPECTOR =================

    [Header("Cấu hình chi phí xây dựng nhà")]
    public List<BuildingCost> constructionCosts = new List<BuildingCost>();

    [Header("Cấu hình tăng trưởng giá")]
    [Range(0f, 100f)] public float costIncreasePercentage = 10f; // Số phần trăm tăng thêm (Ví dụ: 10 nghĩa là +10% mỗi nhà)

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

    private Dictionary<BuildingType, int> buildingCounts = new Dictionary<BuildingType, int>();

    private void Start()
    {
        BuildingCtrl[] existingBuildings = FindObjectsOfType<BuildingCtrl>();
        foreach (BuildingCtrl building in existingBuildings)
        {
            BuildingType type = building.buildingType;
            if (!buildingCounts.ContainsKey(type))
            {
                buildingCounts[type] = 0;
            }
            buildingCounts[type]++;
        }

        // Tự động cập nhật hiển thị giá cho toàn bộ các Text UI ngay khi vào game
        UpdateAllCostUI();
    }

    public void ResetBuildingCounts()
    {
        buildingCounts.Clear();
    }

    // Hàm phụ trợ để lấy nhanh chi phí của một loại nhà (Đã tính toán +% tăng dần)
    public BuildingCost GetBuildingCost(BuildingType type)
    {
        BuildingCost baseCost = new BuildingCost { buildingType = type, woodCost = 0, stoneCost = 0, foodCost = 0 };
        foreach (var cost in constructionCosts)
        {
            if (cost.buildingType == type)
            {
                baseCost = cost;
                break;
            }
        }
        int count = 0;
        if (buildingCounts.ContainsKey(type))
        {
            count = buildingCounts[type];
        }
        if (count > 0)
        {
            float rate = costIncreasePercentage / 100f;
            float multiplier = 1f + (rate * count);
            baseCost.woodCost = Mathf.RoundToInt(baseCost.woodCost * multiplier);
            baseCost.stoneCost = Mathf.RoundToInt(baseCost.stoneCost * multiplier);
            baseCost.foodCost = Mathf.RoundToInt(baseCost.foodCost * multiplier);
        }
        return baseCost;
    }

    // CẬP NHẬT UI TEXT THEO TỪNG CÔNG TRÌNH CỤ THỂ
    public void UpdateCostUI(BuildingType type)
    {
        // Lấy chi phí thực tế sau khi đã tăng %
        BuildingCost realCost = GetBuildingCost(type);

        // Tìm phần tử trong list cấu hình gốc để lấy đúng các biến UI Text đã kéo thả
        for (int i = 0; i < constructionCosts.Count; i++)
        {
            if (constructionCosts[i].buildingType == type)
            {
                if (constructionCosts[i].uiWoodText != null) 
                    constructionCosts[i].uiWoodText.text = realCost.woodCost.ToString();

                if (constructionCosts[i].uiStoneText != null) 
                    constructionCosts[i].uiStoneText.text = realCost.stoneCost.ToString();

                if (constructionCosts[i].uiFoodText != null) 
                    constructionCosts[i].uiFoodText.text = realCost.foodCost.ToString();
                
                break;
            }
        }
    }

    // CẬP NHẬT TOÀN BỘ CÁC TEXT UI TRÊN MÀN HÌNH
    public void UpdateAllCostUI()
    {
        foreach (var cost in constructionCosts)
        {
            UpdateCostUI(cost.buildingType);
        }
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
            if (!buildingCounts.ContainsKey(type))
            {
                buildingCounts[type] = 0;
            }
            buildingCounts[type]++;

            // Cập nhật lại UI hiển thị của riêng loại nhà này ngay sau khi xây xong (vì giá đã tăng lên 10%)
            UpdateCostUI(type);

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