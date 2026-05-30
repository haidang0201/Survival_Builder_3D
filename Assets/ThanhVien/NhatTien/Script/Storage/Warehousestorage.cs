using UnityEngine;
using UnityEngine.Events;

public class WarehouseStorage : MonoBehaviour
{
    [Header("Capacity")]
    public int maxWood  = 500;
    public int maxRice  = 500;
    public int maxStone = 500;

    [Header("Events")]
    public UnityEvent onWarehouseFull;

    private int currentWood  = 0;
    private int currentRice  = 0;
    private int currentStone = 0;

    public int  CurrentWood  => currentWood;
    public int  CurrentRice  => currentRice;
    public int  CurrentStone => currentStone;

    public bool IsWoodFull  => currentWood  >= maxWood;
    public bool IsRiceFull  => currentRice  >= maxRice;
    public bool IsStoneFull => currentStone >= maxStone;

    public int AddWood(int amount = 1)
    {
        if (IsWoodFull) return 0;
        int canAdd = Mathf.Min(amount, maxWood - currentWood);
        currentWood += canAdd;
        SyncWoodToManager(canAdd);
        CheckAllFull();
        return canAdd;
    }

    public int AddRice(int amount = 1)
    {
        if (IsRiceFull) return 0;
        int canAdd = Mathf.Min(amount, maxRice - currentRice);
        currentRice += canAdd;
        SyncRiceToManager(canAdd);
        CheckAllFull();
        return canAdd;
    }

    public int AddStone(int amount = 1)
    {
        if (IsStoneFull) return 0;
        int canAdd = Mathf.Min(amount, maxStone - currentStone);
        currentStone += canAdd;
        SyncStoneToManager(canAdd);
        CheckAllFull();
        return canAdd;
    }

    void SyncWoodToManager(int amount)
    {
        if (JsonDataManager.Ins != null) JsonDataManager.Ins.AddWood(amount);
    }

    void SyncRiceToManager(int amount)
    {
        if (JsonDataManager.Ins != null) JsonDataManager.Ins.AddFood(amount); // Lúa map sang Food
    }

    void SyncStoneToManager(int amount)
    {
        if (JsonDataManager.Ins != null) JsonDataManager.Ins.AddStone(amount);
    }

    void CheckAllFull()
    {
        if (IsWoodFull && IsRiceFull && IsStoneFull)
        {
            onWarehouseFull?.Invoke();
        }
    }

    // FIX: Đồng bộ trừ dữ liệu trong JsonDataManager khi clear kho chính, tránh lỗi Dupe UI
    public void ClearAll()
    {
        if (JsonDataManager.Ins != null)
        {
            JsonDataManager.Ins.AddWood(-currentWood);
            JsonDataManager.Ins.AddFood(-currentRice);
            JsonDataManager.Ins.AddStone(-currentStone);
        }
        currentWood  = 0;
        currentRice  = 0;
        currentStone = 0;
        Debug.Log("[WarehouseStorage] Kho chính đã được làm trống và đồng bộ UI thành công.");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}