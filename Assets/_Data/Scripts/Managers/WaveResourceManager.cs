using UnityEngine;

/*
 * WaveResourceManager.cs
 * Folder: Scripts/Managers/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Tác dụng: Tự động cộng tài nguyên từ các công trình thu thập (Xưởng Gỗ, Mỏ Đá, Nhà Bếp, Nhà Chính...) theo từng Wave/Ngày!
 */

public class WaveResourceManager : MonoBehaviour
{
    private static WaveResourceManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    private void OnEnable()
    {
        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    private void Start()
    {
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        if (DayNightManager.Ins != null)
        {
            DayNightManager.Ins.OnWaveStart -= HandleWaveStart;
            DayNightManager.Ins.OnWaveStart += HandleWaveStart;
        }
    }

    private void UnregisterEvents()
    {
        if (DayNightManager.Ins != null)
        {
            DayNightManager.Ins.OnWaveStart -= HandleWaveStart;
        }
    }

    private void HandleWaveStart(int waveIndex)
    {
        CollectBuildingResourcesForWave(waveIndex);
    }

    /// <summary>
    /// Thu thập tài nguyên từ tất cả các công trình sản xuất trên các Vùng Đất khi bắt đầu Wave mới
    /// </summary>
    public static void CollectBuildingResourcesForWave(int waveIndex)
    {
        if (JsonDataManager.Ins == null) return;

        UpgradeableBuilding[] allBuildings = Object.FindObjectsByType<UpgradeableBuilding>(FindObjectsSortMode.None);
        if (allBuildings == null || allBuildings.Length == 0) return;

        int totalWoodGained = 0;
        int totalStoneGained = 0;
        int totalFoodGained = 0;
        int totalGoldGained = 0;

        foreach (var b in allBuildings)
        {
            if (b == null || !b.gameObject.activeInHierarchy) continue;
            if (b.IsInitialBuildNeeded || b.IsRuined || b.IsUpgrading) continue; // Công trình chưa xây xong hoặc hỏng -> Chưa sinh tài nguyên

            int lvl = b.CurrentLevel; // 0-indexed (0 là Lv1, 1 là Lv2...)

            switch (b.buildingType)
            {
                case BuildingType.WoodCutter:
                    {
                        var ws = b.GetComponentInChildren<WoodStorage>();
                        int workers = (ws != null && ws.maxWorkersLevels != null && lvl < ws.maxWorkersLevels.Length) ? ws.maxWorkersLevels[lvl] : (lvl + 1) * 2;
                        int woodAmount = workers * 15;
                        totalWoodGained += woodAmount;
                    }
                    break;

                case BuildingType.StoneMine:
                case BuildingType.StoneStorage:
                    {
                        var ss = b.GetComponentInChildren<StoneStorage>();
                        int workers = (ss != null && ss.maxWorkersLevels != null && lvl < ss.maxWorkersLevels.Length) ? ss.maxWorkersLevels[lvl] : (lvl + 1) * 2;
                        int stoneAmount = workers * 15;
                        totalStoneGained += stoneAmount;
                    }
                    break;

                case BuildingType.Kitchen:
                case BuildingType.FoodStorage:
                    {
                        var rs = b.GetComponentInChildren<RiceStorage>();
                        var kit = b.GetComponentInChildren<Kitchen>();
                        int workers = (rs != null && rs.maxWorkersLevels != null && lvl < rs.maxWorkersLevels.Length) ? rs.maxWorkersLevels[lvl] : ((kit != null && kit.maxWorkersLevels != null && lvl < kit.maxWorkersLevels.Length) ? kit.maxWorkersLevels[lvl] : (lvl + 1) * 3);
                        int foodAmount = workers * 15;
                        totalFoodGained += foodAmount;
                    }
                    break;

                case BuildingType.House:
                    {
                        int goldAmount = (lvl + 1) * 25;
                        totalGoldGained += goldAmount;
                    }
                    break;
            }
        }

        // Tự động cộng Vàng từ Thủ Đô / Nhà Chính
        totalGoldGained += 50;

        if (totalWoodGained > 0) JsonDataManager.Ins.AddWood(totalWoodGained);
        if (totalStoneGained > 0) JsonDataManager.Ins.AddStone(totalStoneGained);
        if (totalFoodGained > 0) JsonDataManager.Ins.AddFood(totalFoodGained);
        if (totalGoldGained > 0) JsonDataManager.Ins.AddGold(totalGoldGained);

        JsonDataManager.Ins.BroadcastAllResources();

        Debug.Log($"[WaveResourceManager] 🌾 WAVE {waveIndex}: Thu hoạch thành công! +{totalWoodGained} Gỗ, +{totalStoneGained} Đá, +{totalFoodGained} Lương, +{totalGoldGained} Vàng.");

        if (UIManager.Ins != null && (totalWoodGained > 0 || totalStoneGained > 0 || totalFoodGained > 0 || totalGoldGained > 0))
        {
            UIManager.Ins.ShowWarning($"🌾 Wave {waveIndex}: Thu hoạch +{totalWoodGained} Gỗ, +{totalStoneGained} Đá, +{totalFoodGained} Lương, +{totalGoldGained} Vàng!");
        }
    }
}
