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
 * Quản lý tài nguyên runtime và Save/Load JSON.
 *
 * EVENT SIGNATURES (thống nhất toàn dự án):
 *   OnGoldChanged  → Action<int>        (gold, không có max cap)
 *   OnWoodChanged  → Action<int, int>   (current, max)
 *   OnStoneChanged → Action<int, int>   (current, max)
 *   OnFoodChanged  → Action<int, int>   (current, max)
 *
 * Lắng nghe ở: UIResourceObserver → HUDController
 */

public class JsonDataManager : Singleton<JsonDataManager>
{
    // ──────────────────────────────────────────────
    // INSPECTOR
    // ──────────────────────────────────────────────

    [Header("File Settings")]
    public string saveFileName = "builder.json";
    public string configFileName = "building_config.json";

    // ──────────────────────────────────────────────
    // EVENTS  (UIResourceObserver lắng nghe)
    // ──────────────────────────────────────────────

    public event Action<int> OnGoldChanged;
    public event Action<int, int> OnWoodChanged;   // (current, max)
    public event Action<int, int> OnStoneChanged;  // (current, max)
    public event Action<int, int> OnFoodChanged;   // (current, max)

    // ──────────────────────────────────────────────
    // TÀI NGUYÊN RUNTIME
    // ──────────────────────────────────────────────

    public int gold { get; private set; }
    public int wood { get; private set; }
    public int stone { get; private set; }
    public int food { get; private set; }

    // Sức chứa tối đa (nạp từ building_config.json)
    public int maxWood { get; private set; } = 500;
    public int maxStone { get; private set; } = 500;
    public int maxFood { get; private set; } = 500;

    // ──────────────────────────────────────────────
    // PRIVATE
    // ──────────────────────────────────────────────

    private BuildingConfigRoot _loadedConfig;

    // ──────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        LoadBuildingConfigs();
    }

    // ──────────────────────────────────────────────
    // THÊM TÀI NGUYÊN  (có clamp theo max)
    // ──────────────────────────────────────────────

    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
        OnGoldChanged?.Invoke(gold);
    }

    public void AddWood(int amount)
    {
        wood = Mathf.Clamp(wood + amount, 0, maxWood);
        OnWoodChanged?.Invoke(wood, maxWood);
    }

    public void AddStone(int amount)
    {
        stone = Mathf.Clamp(stone + amount, 0, maxStone);
        OnStoneChanged?.Invoke(stone, maxStone);
    }

    public void AddFood(int amount)
    {
        food = Mathf.Clamp(food + amount, 0, maxFood);
        OnFoodChanged?.Invoke(food, maxFood);
    }

    // ──────────────────────────────────────────────
    // NÂNG CẤP SỨC CHỨA KHO
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gọi khi Warehouse được nâng cấp để cập nhật max capacity.
    /// </summary>
    public void UpdateCapacities(int warehouseLevel)
    {
        if (_loadedConfig?.buildingConfigs == null) return;

        var config = _loadedConfig.buildingConfigs.Find(c => c.buildingType == "Warehouse");
        if (config == null) return;

        var levelData = config.levelConfigs.Find(l => l.level == warehouseLevel);
        if (levelData == null) return;

        maxWood = levelData.woodCapacity;
        maxStone = levelData.stoneCapacity;
        maxFood = levelData.foodCapacity;

        // Clamp tài nguyên hiện tại nếu vượt max mới
        wood = Mathf.Min(wood, maxWood);
        stone = Mathf.Min(stone, maxStone);
        food = Mathf.Min(food, maxFood);

        // Cập nhật UI
        OnWoodChanged?.Invoke(wood, maxWood);
        OnStoneChanged?.Invoke(stone, maxStone);
        OnFoodChanged?.Invoke(food, maxFood);

        Debug.Log($"[JsonDataManager] Kho Lvl {warehouseLevel} → maxWood={maxWood}, maxStone={maxStone}, maxFood={maxFood}");
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
                        case "Wood": wood = Mathf.Min(res.amount, maxWood); break;
                        case "Stone": stone = Mathf.Min(res.amount, maxStone); break;
                        case "Food": food = Mathf.Min(res.amount, maxFood); break;
                    }
                }
            }

            // Đẩy toàn bộ lên UI
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

    /// <summary>Phát toàn bộ event để UI cập nhật sau khi Load hoặc khởi động.</summary>
    public void BroadcastAllResources()
    {
        OnGoldChanged?.Invoke(gold);
        OnWoodChanged?.Invoke(wood, maxWood);
        OnStoneChanged?.Invoke(stone, maxStone);
        OnFoodChanged?.Invoke(food, maxFood);
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
    // BUILDING CONFIG (sức chứa tĩnh)
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
            UpdateCapacities(1); // Nạp sức chứa mặc định cấp 1
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

        Debug.Log($"[JsonDataManager] Tạo config mặc định tại: {path}");
    }

    // ──────────────────────────────────────────────
    // DATA CLASSES
    // ──────────────────────────────────────────────

    [Serializable]
    public class GameSaveData
    {
        public string sceneName;
        public long savedAtUnix;
        public List<BuildingState> buildings;
        public List<ResourceData> resources;
    }

    [Serializable]
    public class ResourceData
    {
        public string resourceType;
        public int amount;
    }

    [Serializable]
    public class BuildingConfigRoot
    {
        public List<BuildingConfig> buildingConfigs;
    }

    [Serializable]
    public class BuildingConfig
    {
        public string buildingType;
        public List<WarehouseLevelData> levelConfigs;
    }

    [Serializable]
    public class WarehouseLevelData
    {
        public int level;
        public int woodCapacity;
        public int stoneCapacity;
        public int foodCapacity;
    }
}