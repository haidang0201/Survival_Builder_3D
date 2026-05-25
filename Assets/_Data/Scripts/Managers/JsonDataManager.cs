using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/*
 * JsonDataManager.cs
 * Folder: Scripts/Managers/
 * Đã tích hợp: Nâng cấp sức chứa Kho bãi (Capacity) qua building_config.json
 * Tương thích 100% với TestBuildingPlacement.cs và FileIO hiện tại của team
 */

public class JsonDataManager : Singleton<JsonDataManager>
{
    [Header("File Settings")]
    public string saveFileName = "builder.json"; // File lưu game của TestBuildingPlacement
    public string configFileName = "building_config.json"; // File cấu hình sức chứa tĩnh

    // ================= EVENTS (Dành cho UI lắng nghe) =================
    public event Action<int> OnGoldChanged;
    public event Action<int, int> OnWoodChanged;   // (Current, MaxCapacity)
    public event Action<int, int> OnStoneChanged;
    public event Action<int, int> OnFoodChanged;
    public event Action<float> OnHPChanged;

    // ================= DATA TÀI NGUYÊN =================
    public int gold { get; private set; }
    public int wood { get; private set; }
    public int stone { get; private set; }
    public int food { get; private set; }
    public float hp { get; private set; }

    // Thông số sức chứa tối đa hiện tại (Nạp từ JSON)
    public int maxWood { get; private set; } = 500;
    public int maxStone { get; private set; } = 500;
    public int maxFood { get; private set; } = 500;

    private BuildingConfigRoot _loadedConfig;

    protected override void Awake()
    {
        base.Awake();
        LoadBuildingConfigs(); // Nạp cấu hình sức chứa ngay khi game khởi động
    }

    // ================= LOGIC TÀI NGUYÊN & NÂNG CẤP KHO =================

    public void UpdateCapacities(int warehouseLevel)
    {
        if (_loadedConfig == null || _loadedConfig.buildingConfigs == null) return;

        var config = _loadedConfig.buildingConfigs.Find(c => c.buildingType == "Warehouse");
        if (config != null)
        {
            var levelData = config.levelConfigs.Find(l => l.level == warehouseLevel);
            if (levelData != null)
            {
                maxWood = levelData.woodCapacity;
                maxStone = levelData.stoneCapacity;
                maxFood = levelData.foodCapacity;

                // Cập nhật lại UI ngay khi kho mở rộng
                OnWoodChanged?.Invoke(wood, maxWood);
                OnStoneChanged?.Invoke(stone, maxStone);
                OnFoodChanged?.Invoke(food, maxFood);

                Debug.Log($"[JsonDataManager] Kho đã nâng lên Lvl {warehouseLevel}. Sức chứa Max: {maxWood}");
            }
        }
    }

    // Các hàm Add có sử dụng Mathf.Clamp để chặn không cho tài nguyên vượt sức chứa
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

    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
        OnGoldChanged?.Invoke(gold);
    }

    // ================= HỆ THỐNG SAVE / LOAD (Khớp với TestBuildingPlacement.cs) =================

    public bool SaveGame(GameSaveData save)
    {
        try
        {
            // Tự động đóng gói tài nguyên hiện tại vào list resources để tương thích code Test
            save.resources = new List<ResourceData>
            {
                new ResourceData { resourceType = "Gold", amount = gold },
                new ResourceData { resourceType = "Wood", amount = wood },
                new ResourceData { resourceType = "Stone", amount = stone },
                new ResourceData { resourceType = "Food", amount = food }
            };

            string json = JsonUtility.ToJson(save, true);
            FileIO.SaveToFile(json, saveFileName); // Dùng lại FileIO chuẩn của nhóm
            Debug.Log($"[JsonDataManager] Saved game to {saveFileName}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save game: " + ex.Message);
            return false;
        }
    }

    public bool DeleteSave()
    {
        return FileIO.Delete(saveFileName);
    }

    public GameSaveData LoadGame()
    {
        string json = FileIO.LoadFromFile(saveFileName);
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("Save file not found or empty");
            return null;
        }

        try
        {
            GameSaveData save = JsonUtility.FromJson<GameSaveData>(json);

            // Giải nén tài nguyên từ file Save
            if (save.resources != null)
            {
                foreach (var res in save.resources)
                {
                    if (res.resourceType == "Gold") gold = res.amount;
                    if (res.resourceType == "Wood") wood = res.amount;
                    if (res.resourceType == "Stone") stone = res.amount;
                    if (res.resourceType == "Food") food = res.amount;
                }
            }

            // Gọi Event update lên UI
            OnGoldChanged?.Invoke(gold);
            OnWoodChanged?.Invoke(wood, maxWood);
            OnStoneChanged?.Invoke(stone, maxStone);
            OnFoodChanged?.Invoke(food, maxFood);

            Debug.Log($"[JsonDataManager] Loaded save from {saveFileName}");
            return save;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load save: " + ex.Message);
            return null;
        }
    }

    public IEnumerator LoadData(Action<float> onProgress)
    {
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.2f;
            onProgress?.Invoke(progress);
            yield return null;
        }
    }

    // ================= HỆ THỐNG CONFIG SỨC CHỨA (ĐỘC LẬP VỚI SAVE GAME) =================

    private void LoadBuildingConfigs()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, configFileName);

        // Tự tạo file config mẫu nếu chưa có
        if (!File.Exists(filePath))
        {
            GenerateDefaultConfigFile(filePath);
        }

        try
        {
            string json = File.ReadAllText(filePath);
            _loadedConfig = JsonUtility.FromJson<BuildingConfigRoot>(json);

            // Mặc định nạp sức chứa của Warehouse cấp 1 khi game chạy
            UpdateCapacities(1);
        }
        catch (Exception ex)
        {
            Debug.LogError("[JsonDataManager] Lỗi đọc file config sức chứa JSON: " + ex.Message);
        }
    }

    private void GenerateDefaultConfigFile(string path)
    {
        BuildingConfigRoot root = new BuildingConfigRoot();
        root.buildingConfigs = new List<BuildingConfig>
        {
            new BuildingConfig
            {
                buildingType = "Warehouse",
                levelConfigs = new List<WarehouseLevelData>
                {
                    new WarehouseLevelData { level = 1, woodCapacity = 500, stoneCapacity = 500, foodCapacity = 500 },
                    new WarehouseLevelData { level = 2, woodCapacity = 1200, stoneCapacity = 1200, foodCapacity = 1200 },
                    new WarehouseLevelData { level = 3, woodCapacity = 3000, stoneCapacity = 3000, foodCapacity = 3000 }
                }
            }
        };

        string json = JsonUtility.ToJson(root, true);
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        File.WriteAllText(path, json);
    }

    // ================= ĐỊNH NGHĨA LỚP DỮ LIỆU (DATA TYPES) =================

    [System.Serializable]
    public class GameSaveData
    {
        public string sceneName;
        public long savedAtUnix;
        public List<BuildingState> buildings; // Giữ nguyên BuildingState lấy từ BuildingManager
        public List<ResourceData> resources;  // Giữ nguyên kiểu list cho TestBuildingPlacement
    }

    [System.Serializable]
    public class ResourceData
    {
        public string resourceType;
        public int amount;
    }

    [System.Serializable]
    public class BuildingConfigRoot
    {
        public List<BuildingConfig> buildingConfigs;
    }

    [System.Serializable]
    public class BuildingConfig
    {
        public string buildingType;
        public List<WarehouseLevelData> levelConfigs;
    }

    [System.Serializable]
    public class WarehouseLevelData
    {
        public int level;
        public int woodCapacity;
        public int stoneCapacity;
        public int foodCapacity;
    }
}