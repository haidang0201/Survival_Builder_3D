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
        GameObject[] towers =
            GameObject.FindGameObjectsWithTag(watchTowerTag);

        return towers.Length;
    }
}