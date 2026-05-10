using UnityEngine;
using UnityEngine.Events;

public class WoodStorage : MonoBehaviour
{
    [Header("Storage Settings")]
    public int maxCapacity = 20;

    [Header("Events")]
    public UnityEvent onStorageFull;
    public UnityEvent<int> onWoodAdded; // truyền số lượng hiện tại

    private int currentAmount = 0;

    // ===== PROPERTIES =====
    public int CurrentAmount => currentAmount;
    public int MaxCapacity   => maxCapacity;
    public bool IsFull       => currentAmount >= maxCapacity;
    public bool IsEmpty      => currentAmount <= 0;

    // ===== PUBLIC API =====

    /// <summary>
    /// Thêm gỗ vào kho. Trả về số gỗ thực tế đã thêm được.
    /// </summary>
    public int AddWood(int amount = 1)
    {
        if (IsFull)
        {
            Debug.LogWarning($"[WoodStorage] Kho đã đầy! ({currentAmount}/{maxCapacity}) — Không thể thêm gỗ.");
            return 0;
        }

        int canAdd    = Mathf.Min(amount, maxCapacity - currentAmount);
        currentAmount += canAdd;

        Debug.Log($"[WoodStorage] +{canAdd} gỗ → Kho: {currentAmount}/{maxCapacity}");

        onWoodAdded?.Invoke(currentAmount);

        if (IsFull)
        {
            Debug.Log($"[WoodStorage] ✅ Kho đã đầy! ({currentAmount}/{maxCapacity})");
            onStorageFull?.Invoke();
        }

        return canAdd;
    }

    /// <summary>
    /// Lấy gỗ ra khỏi kho. Trả về số gỗ thực tế đã lấy được.
    /// </summary>
    public int TakeWood(int amount = 1)
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"[WoodStorage] Kho trống! Không thể lấy gỗ.");
            return 0;
        }

        int canTake    = Mathf.Min(amount, currentAmount);
        currentAmount -= canTake;

        Debug.Log($"[WoodStorage] -{canTake} gỗ → Kho: {currentAmount}/{maxCapacity}");

        return canTake;
    }

    public void ClearStorage()
    {
        currentAmount = 0;
        Debug.Log($"[WoodStorage] Kho đã được làm trống.");
    }

    // ===== GIZMO DEBUG =====
    void OnDrawGizmosSelected()
    {
        // Hiển thị vòng tròn phạm vi kho trong Scene view
        Gizmos.color = IsFull ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 1.5f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Wood: {currentAmount}/{maxCapacity}"
        );
#endif
    }
}