using UnityEngine;

public class UIThapCanh : MonoBehaviour
{
    public static UIThapCanh Instance;

    [Header("Tag")]
    public string watchTowerTag = "WatchTower";

    public int towerCount;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        towerCount = CountWatchTower();
    }

    public int GetWatchTowerCount()
    {
        return towerCount;
    }

    public int CountWatchTower()
    {
        int count = 0;

        // 1. Đếm qua WatchTowerAI component (chính xác 100%)
        WatchTowerAI[] aiTowers = Object.FindObjectsByType<WatchTowerAI>(FindObjectsSortMode.None);
        if (aiTowers != null && aiTowers.Length > 0)
        {
            return aiTowers.Length;
        }

        // 2. Bọc try-catch để tránh UnityException khi Tag chưa được tạo trong TagManager
        try
        {
            GameObject[] towers = GameObject.FindGameObjectsWithTag(watchTowerTag);
            if (towers != null)
            {
                count = towers.Length;
            }
        }
        catch (System.Exception)
        {
            // Fallback: Tìm các GameObject có tên chứa "watchtower" hoặc "thapcanh"
            GameObject[] allObjs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjs)
            {
                if (obj != null && (obj.name.ToLower().Contains("watchtower") || obj.name.ToLower().Contains("thapcanh")))
                {
                    count++;
                }
            }
        }

        return count;
    }
}