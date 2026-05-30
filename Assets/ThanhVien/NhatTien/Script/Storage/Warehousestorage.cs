using UnityEngine;

public class WarehouseStorage : MonoBehaviour
{
    private int currentWood  = 0;
    private int currentRice  = 0;
    private int currentStone = 0;

    public int  CurrentWood  => currentWood;
    public int  CurrentRice  => currentRice;
    public int  CurrentStone => currentStone;

    public int AddWood(int amount = 1)
    {
        currentWood += amount;
        SyncWoodToManager(amount);
        return amount;
    }

    public int AddRice(int amount = 1)
    {
        currentRice += amount;
        SyncRiceToManager(amount);
        return amount;
    }

    public int AddStone(int amount = 1)
    {
        currentStone += amount;
        SyncStoneToManager(amount);
        return amount;
    }

    void SyncWoodToManager(int amount)
    {
        if (JsonDataManager.Ins != null) JsonDataManager.Ins.AddWood(amount);
    }

    void SyncRiceToManager(int amount)
    {
        if (JsonDataManager.Ins != null) JsonDataManager.Ins.AddFood(amount); 
    }

    void SyncStoneToManager(int amount)
    {
        if (JsonDataManager.Ins != null) JsonDataManager.Ins.AddStone(amount);
    }

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