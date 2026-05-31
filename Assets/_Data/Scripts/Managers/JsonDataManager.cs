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

    public int gold { get; private set; }
    public int wood { get; private set; }
    public int stone { get; private set; }
    public int food { get; private set; }

    private BuildingConfigRoot _loadedConfig;

    protected override void Awake()
    {
        base.Awake();
        LoadBuildingConfigs();
    }

    // ──────────────────────────────────────────────
    // THÊM TÀI NGUYÊN  (Cộng dồn vô hạn)
    // ──────────────────────────────────────────────

    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
        OnGoldChanged?.Invoke(gold);
    }

    public void AddWood(int amount)
    {
        wood = Mathf.Max(0, wood + amount);
        OnWoodChanged?.Invoke(wood);
    }

    public void AddStone(int amount)
    {
        stone = Mathf.Max(0, stone + amount);
        OnStoneChanged?.Invoke(stone);
    }

    public void AddFood(int amount)
    {
        food = Mathf.Max(0, food + amount);
        OnFoodChanged?.Invoke(food);
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

    [Serializable] public class GameSaveData { public string sceneName; public long savedAtUnix; public List<BuildingState> buildings; public List<ResourceData> resources; }
    [Serializable] public class ResourceData { public string resourceType; public int amount; }
    [Serializable] public class BuildingConfigRoot { public List<BuildingConfig> buildingConfigs; }
    [Serializable] public class BuildingConfig { public string buildingType; public List<WarehouseLevelData> levelConfigs; }
    [Serializable] public class WarehouseLevelData { public int level; public int woodCapacity; public int stoneCapacity; public int foodCapacity; }
}