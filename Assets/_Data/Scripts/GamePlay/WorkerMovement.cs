using UnityEngine;
using UnityEngine.AI;

public class WorkerMovement : MonoBehaviour
{
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>(); // Lấy NavMeshAgent từ GameObject
    }

    // Đặt mục tiêu di chuyển cho công nhân
    public void SetTarget(Vector3 targetPosition)
    {
        agent.SetDestination(targetPosition);
    }

    // Kiểm tra xem công nhân đã đến đích hay chưa
    public bool IsAtDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }
}