using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kho chính — chứa Gỗ + Lúa + Đá.
/// Mỗi lần AddWood/AddRice/AddStone sẽ tự gọi JsonDataManager để cộng UI.
/// Gán tag "Warehouse" để các worker tự tìm.
/// </summary>
public class WarehouseStorage : MonoBehaviour
{
    [Header("Capacity")]
    public int maxWood  = 999;
    public int maxRice  = 999;
    public int maxStone = 999;

    [Header("Events")]
    public UnityEvent onWarehouseFull;

    private int currentWood  = 0;
    private int currentRice  = 0;
    private int currentStone = 0;

    // ===== PROPERTIES =====
    public int  CurrentWood  => currentWood;
    public int  CurrentRice  => currentRice;
    public int  CurrentStone => currentStone;

    public bool IsWoodFull  => currentWood  >= maxWood;
    public bool IsRiceFull  => currentRice  >= maxRice;
    public bool IsStoneFull => currentStone >= maxStone;

    // ===== PUBLIC API =====

    /// <summary>Worker carrier giao gỗ đến đây. Tự cộng lên JsonDataManager.</summary>
    public int AddWood(int amount = 1)
    {
        if (IsWoodFull)
        {
            Debug.Log($"[WarehouseStorage] Kho gỗ đầy! ({currentWood}/{maxWood})");
            return 0;
        }

        int canAdd    = Mathf.Min(amount, maxWood - currentWood);
        currentWood  += canAdd;

        Debug.Log($"[WarehouseStorage] +{canAdd} gỗ → {currentWood}/{maxWood}");

        SyncWoodToManager(canAdd);
        CheckAllFull();

        return canAdd;
    }

    /// <summary>WorkerCarryRice giao lúa đến đây. Tự cộng lên JsonDataManager.</summary>
    public int AddRice(int amount = 1)
    {
        if (IsRiceFull)
        {
            Debug.Log($"[WarehouseStorage] Kho lúa đầy! ({currentRice}/{maxRice})");
            return 0;
        }

        int canAdd   = Mathf.Min(amount, maxRice - currentRice);
        currentRice += canAdd;

        Debug.Log($"[WarehouseStorage] +{canAdd} lúa → {currentRice}/{maxRice}");

        SyncRiceToManager(canAdd);
        CheckAllFull();

        return canAdd;
    }

    /// <summary>WorkerCarryStone giao đá đến đây. Tự cộng lên JsonDataManager.</summary>
    public int AddStone(int amount = 1)
    {
        if (IsStoneFull)
        {
            Debug.Log($"[WarehouseStorage] Kho đá đầy! ({currentStone}/{maxStone})");
            return 0;
        }

        int canAdd    = Mathf.Min(amount, maxStone - currentStone);
        currentStone += canAdd;

        Debug.Log($"[WarehouseStorage] +{canAdd} đá → {currentStone}/{maxStone}");

        SyncStoneToManager(canAdd);
        CheckAllFull();

        return canAdd;
    }

    // ===== SYNC TO JSONDATA MANAGER =====

    void SyncWoodToManager(int amount)
    {
        if (JsonDataManager.Ins == null)
        {
            Debug.LogWarning("[WarehouseStorage] JsonDataManager.Ins chưa tồn tại!");
            return;
        }

        JsonDataManager.Ins.AddWood(amount);
    }

    void SyncRiceToManager(int amount)
    {
        if (JsonDataManager.Ins == null)
        {
            Debug.LogWarning("[WarehouseStorage] JsonDataManager.Ins chưa tồn tại!");
            return;
        }

        // Lúa map sang Food trong JsonDataManager
        JsonDataManager.Ins.AddFood(amount);
    }

    void SyncStoneToManager(int amount)
    {
        if (JsonDataManager.Ins == null)
        {
            Debug.LogWarning("[WarehouseStorage] JsonDataManager.Ins chưa tồn tại!");
            return;
        }

        JsonDataManager.Ins.AddStone(amount);
    }

    void CheckAllFull()
    {
        if (IsWoodFull && IsRiceFull && IsStoneFull)
        {
            Debug.Log("[WarehouseStorage] ✅ Kho chính đầy hoàn toàn!");
            onWarehouseFull?.Invoke();
        }
    }

    public void ClearAll()
    {
        currentWood  = 0;
        currentRice  = 0;
        currentStone = 0;
        Debug.Log("[WarehouseStorage] Kho chính đã làm trống.");
    }

    // ===== GIZMO =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"Kho chính\n" +
            $"Gỗ:  {currentWood}/{maxWood}\n"  +
            $"Lúa: {currentRice}/{maxRice}\n"  +
            $"Đá:  {currentStone}/{maxStone}"
        );
#endif
    }
}