using UnityEngine;

public static class ResourceManager
{
    private const string PATH_BUILDINGS = "Buildings/";
    private const string PATH_RESOURCES = "Drops/";

    public static GameObject GetBuilding(string name)
    {
        // Khớp với folder Resources/Buildings/ của dự án
        GameObject prefab = Resources.Load<GameObject>(PATH_BUILDINGS + name);
        if (prefab == null) Debug.LogError($"[Resource] Không tìm thấy nhà: {name}");
        return prefab;
    }

    public static GameObject GetDropItem(string name)
    {
        // Khớp với task của Nhân (Gỗ, Ván rơi ra)
        return Resources.Load<GameObject>(PATH_RESOURCES + name);
    }
}