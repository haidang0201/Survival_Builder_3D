using UnityEngine;

[System.Serializable]
public class BaseData
{
    public string id;
    public string name;
}

[System.Serializable]
public class BuildingData : BaseData
{
    public string prefabName;
    public Vector3 defaultPosition;
}

[System.Serializable]
public class ResourceData : BaseData
{
    public string resourceType;
    public int amount;
}
