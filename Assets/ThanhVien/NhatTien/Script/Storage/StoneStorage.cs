using UnityEngine;
using UnityEngine.Events;

public class StoneStorage : MonoBehaviour
{
    [Header("Storage Settings")]
    public int maxCapacity = 20;

    [Header("Events")]
    public UnityEvent      onStorageFull;
    public UnityEvent<int> onStoneAdded; 

    private int currentAmount = 0;

    public int  CurrentAmount => currentAmount;
    public int  MaxCapacity   => maxCapacity;
    public bool IsFull        => currentAmount >= maxCapacity;
    public bool IsEmpty       => currentAmount <= 0;

    public int AddStone(int amount = 1)
    {
        if (IsFull) return 0;

        int canAdd     = Mathf.Min(amount, maxCapacity - currentAmount);
        currentAmount += canAdd;

        Debug.Log($"[StoneStorage] '{name}' +{canAdd} đá → {currentAmount}/{maxCapacity}");

        onStoneAdded?.Invoke(currentAmount);
        if (IsFull) onStorageFull?.Invoke();

        return canAdd;
    }

    public int TakeStone(int amount = 1)
    {
        if (IsEmpty) 
        {
            Debug.LogWarning($"[StoneStorage] '{name}' Kho trống!");
            return 0;
        }

        int canTake    = Mathf.Min(amount, currentAmount);
        currentAmount -= canTake;

        Debug.Log($"[StoneStorage] '{name}' -{canTake} đá → {currentAmount}/{maxCapacity}");

        return canTake;
    }

    public void ClearStorage()
    {
        currentAmount = 0;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = IsFull ? Color.red : Color.gray;
        Gizmos.DrawWireSphere(transform.position, 1.5f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Đá tạm: {currentAmount}/{maxCapacity}"
        );
#endif
    }
}