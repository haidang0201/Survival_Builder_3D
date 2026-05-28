using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Kho tạm lúa — nơi worker gặt lúa nộp vào.
/// KHÔNG sync lên UI / JsonDataManager.
/// WorkerCarrier sẽ lấy từ đây và mang về WarehouseStorage.
/// Gán tag "RiceStorage" để WorkerCarrier tự tìm.
/// </summary>
public class RiceStorage : MonoBehaviour
{
    [Header("Storage Settings")]
    public int maxCapacity = 20;

    [Header("Events")]
    public UnityEvent      onStorageFull;
    public UnityEvent<int> onRiceAdded;

    private int currentAmount = 0;

    // ===== PROPERTIES =====
    public int  CurrentAmount => currentAmount;
    public int  MaxCapacity   => maxCapacity;
    public bool IsFull        => currentAmount >= maxCapacity;
    public bool IsEmpty       => currentAmount <= 0;

    // ===== PUBLIC API =====

    public int AddRice(int amount = 1)
    {
        if (IsFull)
        {
            Debug.Log($"[RiceStorage] '{name}' đã đầy! ({currentAmount}/{maxCapacity})");
            return 0;
        }

        int canAdd     = Mathf.Min(amount, maxCapacity - currentAmount);
        currentAmount += canAdd;

        Debug.Log($"[RiceStorage] '{name}' +{canAdd} lúa → {currentAmount}/{maxCapacity}");

        onRiceAdded?.Invoke(currentAmount);

        if (IsFull)
        {
            Debug.Log($"[RiceStorage] '{name}' ✅ Kho đầy!");
            onStorageFull?.Invoke();
        }

        return canAdd;
    }

    public int TakeRice(int amount = 1)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"[RiceStorage] '{name}' Kho trống!");
            return 0;
        }

        int canTake    = Mathf.Min(amount, currentAmount);
        currentAmount -= canTake;

        Debug.Log($"[RiceStorage] '{name}' -{canTake} lúa → {currentAmount}/{maxCapacity}");

        return canTake;
    }

    public void ClearStorage()
    {
        currentAmount = 0;
        Debug.Log($"[RiceStorage] '{name}' Kho đã làm trống.");
    }

    // ===== GIZMO =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = IsFull ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Lúa tạm: {currentAmount}/{maxCapacity}"
        );
#endif
    }
}