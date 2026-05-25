using UnityEngine;
using UnityEngine.Events;

public class RiceStorage : MonoBehaviour
{
    [Header("Storage Settings")]
    public int maxCapacity = 30;

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

    /// <summary>Thêm lúa vào kho. Trả về số thực tế đã thêm.</summary>
    public int AddRice(int amount = 1)
    {
        if (IsFull)
        {
            Debug.Log($"[RiceStorage] Kho đã đầy! ({currentAmount}/{maxCapacity}) — Không thể thêm lúa.");
            return 0;
        }

        int canAdd     = Mathf.Min(amount, maxCapacity - currentAmount);
        currentAmount += canAdd;

        Debug.Log($"[RiceStorage] +{canAdd} lúa → Kho: {currentAmount}/{maxCapacity}");

        onRiceAdded?.Invoke(currentAmount);

        if (IsFull)
        {
            Debug.Log($"[RiceStorage] ✅ Kho lúa đã đầy! ({currentAmount}/{maxCapacity})");
            onStorageFull?.Invoke();
        }

        return canAdd;
    }

    /// <summary>Lấy lúa ra khỏi kho. Trả về số thực tế đã lấy.</summary>
    public int TakeRice(int amount = 1)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"[RiceStorage] Kho trống! Không thể lấy lúa.");
            return 0;
        }

        int canTake    = Mathf.Min(amount, currentAmount);
        currentAmount -= canTake;

        Debug.Log($"[RiceStorage] -{canTake} lúa → Kho: {currentAmount}/{maxCapacity}");

        return canTake;
    }

    public void ClearStorage()
    {
        currentAmount = 0;
        Debug.Log($"[RiceStorage] Kho lúa đã được làm trống.");
    }

    // ===== GIZMO DEBUG =====
    void OnDrawGizmosSelected()
    {
        Gizmos.color = IsFull ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Rice: {currentAmount}/{maxCapacity}"
        );
#endif
    }
}