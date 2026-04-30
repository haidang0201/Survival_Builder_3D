using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ResourceHolder
{
    public string id;
    public int count;
}

// Attach this to any GameObject in the scene to test saving/loading.
// - Fill the `buildings` list with GameObjects you want positions saved for.
// - Fill the `buildingPrefabs` list with the prefab for each building (so they can be respawned on load).
// - Fill the `resources` list with simple id/count pairs representing resource inventory.
// Run in Play mode: press 'F5' to save, 'F9' to load.
public class SaveLoadTester : MonoBehaviour
{
    private static SaveLoadTester instance;

    public List<GameObject> buildings = new List<GameObject>();
    public List<GameObject> buildingPrefabs = new List<GameObject>();
    public List<ResourceHolder> resources = new List<ResourceHolder>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            Save();
        }

        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            Load();
        }
    }

    public void Save()
    {
        var save = new JsonDataManager.GameSaveData();
        save.sceneName = SceneManager.GetActiveScene().name;

        foreach (var go in buildings)
        {
            if (go == null) continue;

            var bs = new JsonDataManager.BuildingState();
            bs.prefabName = go.name;
            bs.position = new JsonDataManager.SerializableVector3(go.transform.position);
            bs.rotation = new JsonDataManager.SerializableVector3(go.transform.eulerAngles);
            save.buildings.Add(bs);
        }

        foreach (var r in resources)
        {
            if (r == null) continue;

            var rs = new JsonDataManager.ResourceState();
            rs.id = r.id;
            rs.count = r.count;
            save.resources.Add(rs);
        }

        save.savedAtUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        bool ok = JsonDataManager.Ins.SaveGame(save);
        Debug.Log(ok ? "Save successful" : "Save failed");
    }

    public void Load()
    {
        var save = JsonDataManager.Ins.LoadGame();
        if (save == null)
        {
            Debug.LogWarning("No save data found to load.");
            return;
        }

        if (!string.IsNullOrEmpty(save.sceneName) && SceneManager.GetActiveScene().name != save.sceneName)
        {
            StartCoroutine(LoadSceneAndRestore(save));
            return;
        }

        RestoreSave(save);
    }

    private IEnumerator LoadSceneAndRestore(JsonDataManager.GameSaveData save)
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(save.sceneName);
        if (loadOperation != null)
        {
            yield return loadOperation;
        }

        yield return null;

        buildings.Clear();
        RestoreSave(save);
    }

    private void RestoreSave(JsonDataManager.GameSaveData save)
    {
        RestoreBuildings(save);
        RestoreResources(save);
        Debug.Log("Load complete");
    }

    private void RestoreBuildings(JsonDataManager.GameSaveData save)
    {
        for (int i = 0; i < save.buildings.Count; i++)
        {
            JsonDataManager.BuildingState state = save.buildings[i];
            GameObject building = i < buildings.Count ? buildings[i] : null;

            if (building == null)
            {
                building = SpawnBuilding(state.prefabName);
                if (building == null)
                {
                    continue;
                }

                if (i < buildings.Count)
                {
                    buildings[i] = building;
                }
                else
                {
                    buildings.Add(building);
                }
            }

            building.transform.position = state.position.ToVector3();
            building.transform.eulerAngles = state.rotation.ToVector3();
        }
    }

    private GameObject SpawnBuilding(string prefabName)
    {
        foreach (var prefab in buildingPrefabs)
        {
            if (prefab == null) continue;

            if (prefab.name == prefabName)
            {
                return Instantiate(prefab);
            }
        }

        Debug.LogWarning($"No prefab found for building '{prefabName}'.");
        return null;
    }

    private void RestoreResources(JsonDataManager.GameSaveData save)
    {
        foreach (var rs in save.resources)
        {
            foreach (var r in resources)
            {
                if (r == null) continue;

                if (r.id == rs.id)
                {
                    r.count = rs.count;
                    break;
                }
            }
        }
    }
}