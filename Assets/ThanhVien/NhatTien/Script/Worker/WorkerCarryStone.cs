using UnityEngine;
using UnityEngine.AI;

public class WorkerCarryStone : MonoBehaviour
{
    public Transform    handPoint;
    public NavMeshAgent agent;
    
    // Đã bỏ [HideInInspector] để bạn dễ dàng theo dõi Nợm (Worker) đang nhắm đi đâu
    public Transform stoneStoragePoint;   

    private StonePickup  currentStone;
    private StoneStorage stoneStorage;

    void Start()
    {
        stoneStorage = FindStoneStorage();
    }

    void OnDisable()
    {
        if (currentStone != null)
        {
            ObjectPool pool = currentStone.pool;
            if (pool != null && currentStone.gameObject.activeInHierarchy) 
                pool.ReturnObject(currentStone.gameObject);
            else 
                Destroy(currentStone.gameObject);
            
            currentStone = null;
        }
    }

    StoneStorage FindStoneStorage()
    {
        // 1. Kiểm tra điểm gán tay
        if (stoneStoragePoint != null)
        {
            StoneStorage ss = stoneStoragePoint.GetComponent<StoneStorage>() ?? stoneStoragePoint.GetComponentInParent<StoneStorage>() ?? stoneStoragePoint.GetComponentInChildren<StoneStorage>();
            if (ss != null) return ss;
        }

        // 2. Tìm tự động bằng Tag "StoneStorage"
        GameObject obj = GameObject.FindWithTag("StoneStorage");
        if (obj != null)
        {
            StoneStorage ss = obj.GetComponent<StoneStorage>() ?? obj.GetComponentInChildren<StoneStorage>();
            if (ss != null)
            {
                stoneStoragePoint = obj.transform;
                return ss;
            }
        }

        // 3. Quét toàn map
        StoneStorage fallback = FindObjectOfType<StoneStorage>();
        if (fallback != null)
        {
            stoneStoragePoint = fallback.transform;
            return fallback;
        }
        
        // FIX "HỐ ĐEN": Xóa trắng điểm đến nếu không tìm thấy kho, tránh việc AI chạy bậy bạ
        stoneStoragePoint = null;
        return null;
    }

    public bool IsCarrying() => currentStone != null;

    public void PickupStone(StonePickup stone)
    {
        if (stone == null || stone.IsTaken()) return;
        stone.MarkTaken();
        currentStone = stone;
        currentStone.Pickup(handPoint);
        agent.ResetPath();
    }

    public bool MoveToStorage()
    {
        if (currentStone == null || stoneStoragePoint == null || !agent.isOnNavMesh) return false;
        agent.isStopped = false;
        agent.SetDestination(stoneStoragePoint.position);
        return true;
    }

    public bool TryDeposit()
    {
        if (currentStone == null) return false;
        
        // FIX "HỐ ĐEN": Chặn ngay lập tức, bắt AI đứng ôm đá chờ đợi thay vì vứt phi tang!
        if (stoneStorage == null) 
        {
            Debug.LogError($"[WorkerCarryStone] {name} KHÔNG tìm thấy StoneStorage (Kho tạm đá) trên Map. Hãy kiểm tra Tag!");
            return false; 
        }

        if (stoneStorage.IsFull) return false;

        ObjectPool pool = currentStone.pool;
        if (pool != null) pool.ReturnObject(currentStone.gameObject);
        else currentStone.gameObject.SetActive(false);

        currentStone = null;
        stoneStorage.AddStone(1);
        return true;
    }
}