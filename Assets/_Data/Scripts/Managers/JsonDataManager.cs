using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class JsonDataManager : Singleton<JsonDataManager>
{
    public string saveFileName = "savegame.json";

    protected override void Awake()
    {
        MakeSingleton(false);
    }

    // Load a JSON file from StreamingAssets. On Android this uses UnityWebRequest.
    public IEnumerator LoadJsonFromStreamingAssetsCoroutine<T>(string fileName, System.Action<T> onLoaded)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (path.Contains("jar:file://") || Application.platform == RuntimePlatform.Android)
        {
            using (UnityWebRequest uwr = UnityWebRequest.Get(path))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Failed to read streaming asset: " + uwr.error);
                    onLoaded?.Invoke(default);
                    yield break;
                }

                string json = uwr.downloadHandler.text;
                T data = JsonUtility.FromJson<T>(json);
                onLoaded?.Invoke(data);
            }
        }
        else
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning("Streaming asset not found: " + path);
                onLoaded?.Invoke(default);
                yield break;
            }

            string json = File.ReadAllText(path);
            T data = JsonUtility.FromJson<T>(json);
            onLoaded?.Invoke(data);
            yield break;
        }
    }

    // Synchronous helper for platforms where StreamingAssets is a normal filesystem
    public T LoadJsonFromStreamingAssets<T>(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning("Streaming asset not found: " + path);
            return default;
        }

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<T>(json);
    }

    // Save game state to persistentDataPath
    public bool SaveGame(GameSaveData save)
    {
        try
        {
            string json = JsonUtility.ToJson(save, true);
            string path = Path.Combine(Application.persistentDataPath, saveFileName);
            File.WriteAllText(path, json);
            Debug.Log($"Saved game to {path}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to save game: " + ex.Message);
            return false;
        }
    }

    // Load game state from persistentDataPath
    public GameSaveData LoadGame()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning("Save file not found: " + path);
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            GameSaveData save = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log($"Loaded save from {path}");
            return save;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to load save: " + ex.Message);
            return null;
        }
    }

    // --- Save data types ---
    [System.Serializable]
    public class GameSaveData
    {
        public string sceneName;
        public List<BuildingState> buildings = new List<BuildingState>();
        public List<ResourceState> resources = new List<ResourceState>();
        public long savedAtUnix;
    }

    [System.Serializable]
    public class BuildingState
    {
        public string prefabName;
        public SerializableVector3 position;
        public SerializableVector3 rotation;
    }

    [System.Serializable]
    public class ResourceState
    {
        public string id;
        public int count;
    }

    [System.Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x; this.y = y; this.z = z;
        }

        public SerializableVector3(Vector3 v)
        {
            x = v.x; y = v.y; z = v.z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }
}
