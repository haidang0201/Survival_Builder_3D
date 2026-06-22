using UnityEngine;

/// <summary>
/// Quản lý tài nguyên. Gắn vào GameObject "ResourceManager".
/// </summary>
public class ResourceMa : MonoBehaviour
{
    public static ResourceMa Instance;

    [Header("Tài nguyên")]
    public int wood;
    public int stone;
    public int coin;
    public int wheat;

    [Header("Trạng thái")]
    public bool stoneUnlocked = false; // Tutorial Step 3 sẽ mở

    void Awake() { Instance = this; }

    public void AddWood(int v) { wood += v; }
    public void AddCoin(int v) { coin += v; }
    public void AddWheat(int v) { wheat += v; }

    public void AddStone(int v)
    {
        if (!stoneUnlocked) return; // chặn cho đến khi tutorial mở
        stone += v;
    }

    public void UnlockStone() { stoneUnlocked = true; }
}