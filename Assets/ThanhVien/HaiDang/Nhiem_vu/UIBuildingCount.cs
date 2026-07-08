using UnityEngine;

public class UIBuildingCount : MonoBehaviour
{
    public static UIBuildingCount Instance;

    [Header("Tag công trình")]
    public string buildingTag = "Building";

    public int buildingCount;


    void Awake()
    {
        Instance = this;
    }


    void Update()
    {
        buildingCount = CountBuildings();
    }


    public int GetBuildingCount()
    {
        return buildingCount;
    }


    public int CountBuildings()
    {
        GameObject[] buildings =
            GameObject.FindGameObjectsWithTag(buildingTag);

        return buildings.Length;
    }
}