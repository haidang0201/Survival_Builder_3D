using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kho chính — chứa Gỗ + Lúa + Đá.
/// Add* để thêm vào, Consume* để lấy ra (Kitchen dùng).
/// Cả hai đều sync UI qua JsonDataManager.
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

    [Header("UI Events")]
    public UnityEvent<int> onWoodChanged;
    public UnityEvent<int> onRiceChanged;
    public UnityEvent<int> onStoneChanged;

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

    public bool IsWoodEmpty  => currentWood  <= 0;
    public bool IsRiceEmpty  => currentRice  <= 0;
    public bool IsStoneEmpty => currentStone <= 0;

    // ===== ADD (worker nộp vào) =====

    public int AddWood(int amount = 1)
    {
        if (IsWoodFull)
        {
            Debug.Log($"[WarehouseStorage] Kho gỗ đầy! ({currentWood}/{maxWood})");
            return 0;
        }

        int canAdd   = Mathf.Min(amount, maxWood - currentWood);
        currentWood += canAdd;

        SyncWoodToManager(canAdd);
        onWoodChanged?.Invoke(currentWood);
        CheckAllFull();

        Debug.Log($"[WarehouseStorage] +{canAdd} gỗ → {currentWood}/{maxWood}");
        return canAdd;
    }

    public int AddRice(int amount = 1)
    {
        if (IsRiceFull)
        {
            Debug.Log($"[WarehouseStorage] Kho lúa đầy! ({currentRice}/{maxRice})");
            return 0;
        }

        int canAdd   = Mathf.Min(amount, maxRice - currentRice);
        currentRice += canAdd;

        SyncRiceToManager(canAdd);
        onRiceChanged?.Invoke(currentRice);
        CheckAllFull();

        Debug.Log($"[WarehouseStorage] +{canAdd} lúa → {currentRice}/{maxRice}");
        return canAdd;
    }

    public int AddStone(int amount = 1)
    {
        if (IsStoneFull)
        {
            Debug.Log($"[WarehouseStorage] Kho đá đầy! ({currentStone}/{maxStone})");
            return 0;
        }

        int canAdd    = Mathf.Min(amount, maxStone - currentStone);
        currentStone += canAdd;

        SyncStoneToManager(canAdd);
        onStoneChanged?.Invoke(currentStone);
        CheckAllFull();

        Debug.Log($"[WarehouseStorage] +{canAdd} đá → {currentStone}/{maxStone}");
        return canAdd;
    }

    // ===== CONSUME (Kitchen lấy ra, xây dựng tốn nguyên liệu, v.v.) =====

    public int ConsumeRice(int amount = 1)
    {
        if (IsRiceEmpty)
        {
            Debug.LogWarning($"[WarehouseStorage] Hết lúa!");
            return 0;
        }

        int canTake  = Mathf.Min(amount, currentRice);
        currentRice -= canTake;

        SyncRiceToManager(-canTake);
        onRiceChanged?.Invoke(currentRice);
        Debug.Log($"[WarehouseStorage] -{canTake} lúa (tiêu thụ) → {currentRice}/{maxRice}");
        return canTake;
    }

    public int ConsumeWood(int amount = 1)
    {
        if (IsWoodEmpty)
        {
            Debug.LogWarning($"[WarehouseStorage] Hết gỗ!");
            return 0;
        }

        int canTake  = Mathf.Min(amount, currentWood);
        currentWood -= canTake;

        SyncWoodToManager(-canTake);
        onWoodChanged?.Invoke(currentWood);
        Debug.Log($"[WarehouseStorage] -{canTake} gỗ (tiêu thụ) → {currentWood}/{maxWood}");
        return canTake;
    }

    public int ConsumeStone(int amount = 1)
    {
        if (IsStoneEmpty)
        {
            Debug.LogWarning($"[WarehouseStorage] Hết đá!");
            return 0;
        }

        int canTake   = Mathf.Min(amount, currentStone);
        currentStone -= canTake;

        SyncStoneToManager(-canTake);
        onStoneChanged?.Invoke(currentStone);
        Debug.Log($"[WarehouseStorage] -{canTake} đá (tiêu thụ) → {currentStone}/{maxStone}");
        return canTake;
    }

    // ===== SYNC =====

    void SyncWoodToManager(int delta)
    {
        if (JsonDataManager.Ins == null) return;
        JsonDataManager.Ins.AddWood(delta);
    }

    void SyncRiceToManager(int delta)
    {
        if (JsonDataManager.Ins == null) return;
        JsonDataManager.Ins.AddFood(delta);
    }

    void SyncStoneToManager(int delta)
    {
        if (JsonDataManager.Ins == null) return;
        JsonDataManager.Ins.AddStone(delta);
    }

    void CheckAllFull()
    {
        if (IsWoodFull && IsRiceFull && IsStoneFull)
        {
            Debug.Log("[WarehouseStorage] ✅ Kho chính đầy hoàn toàn!");
            onWarehouseFull?.Invoke();
        }
    }

    // ===== CLEAR =====

    public void ClearAll()
    {
        SyncWoodToManager(-currentWood);
        SyncRiceToManager(-currentRice);
        SyncStoneToManager(-currentStone);

        currentWood  = 0;
        currentRice  = 0;
        currentStone = 0;

        onWoodChanged?.Invoke(currentWood);
        onRiceChanged?.Invoke(currentRice);
        onStoneChanged?.Invoke(currentStone);

        Debug.Log("[WarehouseStorage] Kho chính đã được làm trống và đồng bộ UI.");
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