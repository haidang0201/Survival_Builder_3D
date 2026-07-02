using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
 * JsonDataManager.cs
 * Folder: Scripts/Managers/
 * Người làm: DŨNG / ĐĂNG
 *
 * Quản lý tài nguyên runtime và Save/Load JSON. (ĐÃ BỎ MAX CAPACITY)
 *
 * EVENT SIGNATURES:
 * OnGoldChanged  → Action<int>
 * OnWoodChanged  → Action<int>
 * OnStoneChanged → Action<int>
 * OnFoodChanged  → Action<int>
 */

public class JsonDataManager : Singleton<JsonDataManager>
{
    [Header("File Settings")]
    public string saveFileName = "builder.json";
    public string configFileName = "building_config.json";

    // ──────────────────────────────────────────────
    // EVENTS  (UIResourceObserver lắng nghe)
    // ──────────────────────────────────────────────

    public event Action<int> OnGoldChanged;
    public event Action<int> OnWoodChanged;
    public event Action<int> OnStoneChanged;
    public event Action<int> OnFoodChanged;

    // ──────────────────────────────────────────────
    // TÀI NGUYÊN RUNTIME
    // ──────────────────────────────────────────────

    public int gold { get; private set; } = 50;
    public int wood { get; private set; } = 50;
    public int stone { get; private set; } = 50;
    public int food { get; private set; } = 50;

    // ──────────────────────────────────────────────
    // BỔ SUNG: TÀI NGUYÊN TÍCH LŨY SUỐT TRẬN ĐẤU (Phục vụ EndGameUI)
    // ──────────────────────────────────────────────
    public int TotalWoodCollected { get; private set; }
    public int TotalStoneCollected { get; private set; }
    public int TotalFoodCollected { get; private set; }
    public int TotalGoldCollected { get; private set; }

    private BuildingConfigRoot _loadedConfig;

    protected override void Awake()
    {
        base.Awake();
        LoadBuildingConfigs();
    }

    // ──────────────────────────────────────────────
    // THÊM TÀI NGUYÊN  (Cộng dồn vô hạn)
    // ──────────────────────────────────────────────

    public void AddWood(int amount)
    {
        wood += amount;
        if (amount > 0) TotalWoodCollected += amount; // Cộng dồn tích lũy khi nhặt được
        OnWoodChanged?.Invoke(wood);
    }

    public void AddStone(int amount)
    {
        stone += amount;
        if (amount > 0) TotalStoneCollected += amount; // Cộng dồn tích lũy khi nhặt được
        OnStoneChanged?.Invoke(stone);
    }

    public void AddFood(int amount)
    {
        food += amount;
        if (amount > 0) TotalFoodCollected += amount; // Cộng dồn tích lũy khi nhặt được
        OnFoodChanged?.Invoke(food);
    }

    public void AddGold(int amount)
    {
        gold += amount;
        if (amount > 0) TotalGoldCollected += amount; // Cộng dồn tích lũy khi nhặt được
        OnGoldChanged?.Invoke(gold);
    }
    // ──────────────────────────────────────────────
    // NÂNG CẤP SỨC CHỨA KHO (Đã bỏ logic Max, giữ hàm để không lỗi hệ thống khác)
    // ──────────────────────────────────────────────
    public void UpdateCapacities(int warehouseLevel)
    {
        // Hệ thống không còn dùng Max Capacity, hàm này giữ lại để 
        // các script gọi nâng cấp kho không bị báo lỗi reference.
        Debug.Log($"[JsonDataManager] Kho Lvl {warehouseLevel} upgraded (Max limits removed).");
    }

    // ──────────────────────────────────────────────
    // SAVE / LOAD
    // ──────────────────────────────────────────────

    public bool SaveGame(GameSaveData save)
    {
        try
        {
            save.resources = new List<ResourceData>
            {
                new ResourceData { resourceType = "Gold",  amount = gold  },
                new ResourceData { resourceType = "Wood",  amount = wood  },
                new ResourceData { resourceType = "Stone", amount = stone },
                new ResourceData { resourceType = "Food",  amount = food  },
            };

            string json = JsonUtility.ToJson(save, true);
            FileIO.SaveToFile(json, saveFileName);
            Debug.Log($"[JsonDataManager] ✅ Saved → {saveFileName}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[JsonDataManager] ❌ Save failed: " + ex.Message);
            return false;
        }
    }

    public GameSaveData LoadGame()
    {
        string json = FileIO.LoadFromFile(saveFileName);
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[JsonDataManager] Chưa có save file.");
            return null;
        }

        try
        {
            GameSaveData save = JsonUtility.FromJson<GameSaveData>(json);

            if (save.resources != null)
            {
                foreach (var res in save.resources)
                {
                    switch (res.resourceType)
                    {
                        case "Gold": gold = res.amount; break;
                        case "Wood": wood = res.amount; break;
                        case "Stone": stone = res.amount; break;
                        case "Food": food = res.amount; break;
                    }
                }
            }

            BroadcastAllResources();
            Debug.Log($"[JsonDataManager] ✅ Loaded ← {saveFileName}");
            return save;
        }
        catch (Exception ex)
        {
            Debug.LogError("[JsonDataManager] ❌ Load failed: " + ex.Message);
            return null;
        }
    }

    public bool DeleteSave() => FileIO.Delete(saveFileName);

    public void BroadcastAllResources()
    {
        OnGoldChanged?.Invoke(gold);
        OnWoodChanged?.Invoke(wood);
        OnStoneChanged?.Invoke(stone);
        OnFoodChanged?.Invoke(food);
    }

    public IEnumerator LoadData(Action<float> onProgress)
    {
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.2f;
            onProgress?.Invoke(Mathf.Min(progress, 1f));
            yield return null;
        }
    }

    // ──────────────────────────────────────────────
    // BUILDING CONFIG (Giữ nguyên class data để không lỗi JSON cũ)
    // ──────────────────────────────────────────────

    private void LoadBuildingConfigs()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, configFileName);

        if (!File.Exists(filePath))
            GenerateDefaultConfigFile(filePath);

        try
        {
            string json = File.ReadAllText(filePath);
            _loadedConfig = JsonUtility.FromJson<BuildingConfigRoot>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("[JsonDataManager] ❌ Lỗi đọc config: " + ex.Message);
        }
    }

    private void GenerateDefaultConfigFile(string path)
    {
        var root = new BuildingConfigRoot
        {
            buildingConfigs = new List<BuildingConfig>
            {
                new BuildingConfig
                {
                    buildingType = "Warehouse",
                    levelConfigs = new List<WarehouseLevelData>
                    {
                        new WarehouseLevelData { level = 1, woodCapacity = 500,  stoneCapacity = 500,  foodCapacity = 500  },
                        new WarehouseLevelData { level = 2, woodCapacity = 1200, stoneCapacity = 1200, foodCapacity = 1200 },
                        new WarehouseLevelData { level = 3, woodCapacity = 3000, stoneCapacity = 3000, foodCapacity = 3000 },
                    }
                }
            }
        };

        string json = JsonUtility.ToJson(root, true);
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(path, json);
    }
    // Thêm vào cuối class JsonDataManager.cs để các script khác dễ dàng ghi nhận thành tích
    public static void RegisterStat_ResourceCollected(string resourceType, int amount)
    {
        if (amount <= 0) return;
        string key = "Stat_Total_" + resourceType; // Ví dụ: Stat_Total_Wood
        int currentTotal = PlayerPrefs.GetInt(key, 0);
        PlayerPrefs.SetInt(key, currentTotal + amount);
        PlayerPrefs.Save();
    }

    public static void RegisterStat_BuildingConstructed()
    {
        int currentTotal = PlayerPrefs.GetInt("Stat_Total_Buildings", 0);
        PlayerPrefs.SetInt("Stat_Total_Buildings", currentTotal + 1);
        PlayerPrefs.Save();
    }

    public static void RegisterStat_DaysSurvived(int days)
    {
        // Cập nhật số ngày sống sót cao nhất hoặc hiện tại
        PlayerPrefs.SetInt("Stat_Survival_Days", days);
        PlayerPrefs.Save();
    }

    // Hàm dọn dẹp data cũ khi bấm nút Chơi Lại (Restart)
    public static void ResetEndGameStats()
    {
        PlayerPrefs.DeleteKey("Stat_Total_Wood");
        PlayerPrefs.DeleteKey("Stat_Total_Stone");
        PlayerPrefs.DeleteKey("Stat_Total_Food");
        PlayerPrefs.DeleteKey("Stat_Total_Gold");
        PlayerPrefs.DeleteKey("Stat_Total_Buildings");
        PlayerPrefs.DeleteKey("Stat_Survival_Days");
        PlayerPrefs.Save();
    }

    [Serializable] public class GameSaveData { public string sceneName; public long savedAtUnix; public List<BuildingState> buildings; public List<ResourceData> resources; }
    [Serializable] public class ResourceData { public string resourceType; public int amount; }
    [Serializable] public class BuildingConfigRoot { public List<BuildingConfig> buildingConfigs; }
    [Serializable] public class BuildingConfig { public string buildingType; public List<WarehouseLevelData> levelConfigs; }
    [Serializable] public class WarehouseLevelData { public int level; public int woodCapacity; public int stoneCapacity; public int foodCapacity; }
}