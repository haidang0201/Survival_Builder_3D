using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;


public class JsonDataManager : Singleton<JsonDataManager>
{
    public string saveFileName = "savegame/survival/builder.json";

    public event Action<int> OnGoldChanged;
    public event Action<int> OnWoodChanged;
    public event Action<float> OnHPChanged;

    public int gold { get; private set; }
    public int wood { get; private set; }
    public float hp { get; private set; }

    protected override void Awake()
    {
        base.Awake();
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))  // Nhấn phím T để tải game
        {
            GameSaveData loadedData = JsonDataManager.Ins.LoadGame();
            if (loadedData != null)
            {
                Debug.Log($"Loaded game:");
            }

            else
            {
                Debug.LogError("No save file found!");
            }


        }
    }

    // Lưu game vào PersistentDataPath
    public bool SaveGame(GameSaveData save)
    {
        try
        {
            string json = JsonUtility.ToJson(save, true);
            FileIO.SaveToFile(json, saveFileName);
            Debug.Log($"Saved game to {saveFileName}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save game: " + ex.Message);
            return false;
        }
    }

    // Tải game từ PersistentDataPath
    public GameSaveData LoadGame()
    {
        string json = FileIO.LoadFromFile(saveFileName);
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("Save file not found or empty");
            return null;
        }

        try
        {
            GameSaveData save = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log($"Loaded save from {saveFileName}");
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

        // Giả lập việc tải từng bước (có thể thay bằng tải dữ liệu thực tế)
        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.2f;  // Tăng tiến độ dần (có thể thay bằng tiến độ thực tế khi tải)
            onProgress?.Invoke(progress);  // Gửi tiến độ đến UI
            yield return null;
        }
    }


    // Save Data Types
    // ==============================
    [System.Serializable]
    public class GameSaveData
    {
        public string sceneName;
        public List<BuildingData> buildings = new List<BuildingData>();
        public List<ResourceData> resources = new List<ResourceData>();
        public long savedAtUnix;
    }
}