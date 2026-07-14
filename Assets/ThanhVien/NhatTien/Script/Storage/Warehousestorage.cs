using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kho chính — chứa Gỗ + Lúa + Đá.
/// FIX: KHÔNG còn giữ số riêng (currentWood/currentRice/currentStone) nữa.
/// JsonDataManager.Ins.wood/food/stone giờ là NGUỒN THẬT DUY NHẤT — WarehouseStorage chỉ
/// đọc/ghi thẳng vào đó, đóng vai trò "cửa vào kho có kiểm tra giới hạn max".
/// Trước đây currentRice/currentWood/currentStone khởi tạo = 0 độc lập với JsonDataManager,
/// nên tài nguyên có sẵn lúc đầu game (JsonDataManager.food = 500 mặc định) không được
/// WarehouseStorage/Kitchen nhận ra (Kitchen tưởng kho trống dù HUD báo còn 500 lúa).
/// Add* để thêm vào, Consume* để lấy ra (Kitchen dùng).
/// Cả hai đều sync UI qua JsonDataManager (đã là nguồn thật nên sync = ghi trực tiếp).
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

    // ===== PROPERTIES (đọc thẳng từ JsonDataManager - nguồn thật) =====
    public int CurrentWood  => JsonDataManager.Ins != null ? JsonDataManager.Ins.wood  : 0;
    public int CurrentRice  => JsonDataManager.Ins != null ? JsonDataManager.Ins.food  : 0;
    public int CurrentStone => JsonDataManager.Ins != null ? JsonDataManager.Ins.stone : 0;

    public bool IsWoodFull  => CurrentWood  >= maxWood;
    public bool IsRiceFull  => CurrentRice  >= maxRice;
    public bool IsStoneFull => CurrentStone >= maxStone;

    public bool IsWoodEmpty  => CurrentWood  <= 0;
    public bool IsRiceEmpty  => CurrentRice  <= 0;
    public bool IsStoneEmpty => CurrentStone <= 0;

    // ===== ADD (worker nộp vào) =====

    public int AddWood(int amount = 1)
    {
        if (IsWoodFull)
        {
            Debug.Log($"[WarehouseStorage] Kho gỗ đầy! ({CurrentWood}/{maxWood})");
            return 0;
        }

        int canAdd = Mathf.Min(amount, maxWood - CurrentWood);
        SyncWoodToManager(canAdd);
        onWoodChanged?.Invoke(CurrentWood);
        CheckAllFull();

        Debug.Log($"[WarehouseStorage] +{canAdd} gỗ → {CurrentWood}/{maxWood}");
        return canAdd;
    }

    public int AddRice(int amount = 1)
    {
        if (IsRiceFull)
        {
            Debug.Log($"[WarehouseStorage] Kho lúa đầy! ({CurrentRice}/{maxRice})");
            return 0;
        }

        int canAdd = Mathf.Min(amount, maxRice - CurrentRice);
        SyncRiceToManager(canAdd);
        onRiceChanged?.Invoke(CurrentRice);
        CheckAllFull();

        Debug.Log($"[WarehouseStorage] +{canAdd} lúa → {CurrentRice}/{maxRice}");
        return canAdd;
    }

    public int AddStone(int amount = 1)
    {
        if (IsStoneFull)
        {
            Debug.Log($"[WarehouseStorage] Kho đá đầy! ({CurrentStone}/{maxStone})");
            return 0;
        }

        int canAdd = Mathf.Min(amount, maxStone - CurrentStone);
        SyncStoneToManager(canAdd);
        onStoneChanged?.Invoke(CurrentStone);
        CheckAllFull();

        Debug.Log($"[WarehouseStorage] +{canAdd} đá → {CurrentStone}/{maxStone}");
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

        int canTake = Mathf.Min(amount, CurrentRice);
        SyncRiceToManager(-canTake);
        onRiceChanged?.Invoke(CurrentRice);
        Debug.Log($"[WarehouseStorage] -{canTake} lúa (tiêu thụ) → {CurrentRice}/{maxRice}");
        return canTake;
    }

    public int ConsumeWood(int amount = 1)
    {
        if (IsWoodEmpty)
        {
            Debug.LogWarning($"[WarehouseStorage] Hết gỗ!");
            return 0;
        }

        int canTake = Mathf.Min(amount, CurrentWood);
        SyncWoodToManager(-canTake);
        onWoodChanged?.Invoke(CurrentWood);
        Debug.Log($"[WarehouseStorage] -{canTake} gỗ (tiêu thụ) → {CurrentWood}/{maxWood}");
        return canTake;
    }

    public int ConsumeStone(int amount = 1)
    {
        if (IsStoneEmpty)
        {
            Debug.LogWarning($"[WarehouseStorage] Hết đá!");
            return 0;
        }

        int canTake = Mathf.Min(amount, CurrentStone);
        SyncStoneToManager(-canTake);
        onStoneChanged?.Invoke(CurrentStone);
        Debug.Log($"[WarehouseStorage] -{canTake} đá (tiêu thụ) → {CurrentStone}/{maxStone}");
        return canTake;
    }

    // ===== SYNC (ghi trực tiếp vào nguồn thật JsonDataManager) =====

    void SyncWoodToManager(int delta)
    {
        if (JsonDataManager.Ins == null)
        {
            Debug.LogError("[WarehouseStorage] Không tìm thấy JsonDataManager.Ins — không thể cập nhật gỗ!");
            return;
        }
        JsonDataManager.Ins.AddWood(delta);
    }

    void SyncRiceToManager(int delta)
    {
        if (JsonDataManager.Ins == null)
        {
            Debug.LogError("[WarehouseStorage] Không tìm thấy JsonDataManager.Ins — không thể cập nhật lúa!");
            return;
        }
        JsonDataManager.Ins.AddFood(delta);
    }

    void SyncStoneToManager(int delta)
    {
        if (JsonDataManager.Ins == null)
        {
            Debug.LogError("[WarehouseStorage] Không tìm thấy JsonDataManager.Ins — không thể cập nhật đá!");
            return;
        }
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
        SyncWoodToManager(-CurrentWood);
        SyncRiceToManager(-CurrentRice);
        SyncStoneToManager(-CurrentStone);

        onWoodChanged?.Invoke(CurrentWood);
        onRiceChanged?.Invoke(CurrentRice);
        onStoneChanged?.Invoke(CurrentStone);

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
            $"Gỗ:  {CurrentWood}/{maxWood}\n"  +
            $"Lúa: {CurrentRice}/{maxRice}\n"  +
            $"Đá:  {CurrentStone}/{maxStone}"
        );
#endif
    }
}