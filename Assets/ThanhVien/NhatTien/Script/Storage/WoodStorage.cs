using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kho tạm — nơi worker chặt cây nộp gỗ vào.
/// KHÔNG sync lên UI / JsonDataManager.
/// WorkerCarrier sẽ lấy từ đây và mang về WarehouseStorage (mới cộng UI).
/// Gán tag "Storage" để WorkerCarrier tự tìm.
/// </summary>
public class WoodStorage : MonoBehaviour
{
    [Header("Storage Settings")]
    public int maxCapacity = 20;

    [Header("Events")]
    public UnityEvent      onStorageFull;
    public UnityEvent<int> onWoodAdded; // truyền currentAmount

    private int currentAmount = 0;

    // ===== PROPERTIES =====
    public int  CurrentAmount => currentAmount;
    public int  MaxCapacity   => maxCapacity;
    public bool IsFull        => currentAmount >= maxCapacity;
    public bool IsEmpty       => currentAmount <= 0;

    // ===== PUBLIC API =====

    public int AddWood(int amount = 1)
    {
        if (IsFull)
        {
            Debug.Log($"[WoodStorage] '{name}' đã đầy! ({currentAmount}/{maxCapacity})");
            return 0;
        }

        int canAdd     = Mathf.Min(amount, maxCapacity - currentAmount);
        currentAmount += canAdd;

        Debug.Log($"[WoodStorage] '{name}' +{canAdd} gỗ → {currentAmount}/{maxCapacity}");

        onWoodAdded?.Invoke(currentAmount);

        if (IsFull)
        {
            Debug.Log($"[WoodStorage] '{name}' ✅ Kho đầy!");
            onStorageFull?.Invoke();
        }

        return canAdd;
    }

    public int TakeWood(int amount = 1)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"[WoodStorage] '{name}' Kho trống!");
            return 0;
        }

        int canTake    = Mathf.Min(amount, currentAmount);
        currentAmount -= canTake;

        Debug.Log($"[WoodStorage] '{name}' -{canTake} gỗ → {currentAmount}/{maxCapacity}");

        return canTake;
    }

    public void ClearStorage()
    {
        currentAmount = 0;
        Debug.Log($"[WoodStorage] '{name}' Kho đã làm trống.");
    }

    // ===== GIZMO =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = IsFull ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 1.5f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Gỗ tạm: {currentAmount}/{maxCapacity}"
        );
#endif
    }
}