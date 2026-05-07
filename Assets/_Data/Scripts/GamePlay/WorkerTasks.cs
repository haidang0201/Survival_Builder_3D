using UnityEngine;

public class WorkerTasks : MonoBehaviour
{
    public bool readyForTask = false;  // Khi công nhân sẵn sàng làm việc
    public bool inHouse = false;       // Công nhân có đang ở trong nhà không?

    private WorkerCtrl workerCtrl;

    private void Awake()
    {
        workerCtrl = GetComponent<WorkerCtrl>(); // Lấy WorkerCtrl từ GameObject
    }

    // Thực hiện tác vụ của công nhân (Đi làm hoặc về nhà)
    public void PerformTask()
    {
        if (readyForTask)
        {
            workerCtrl.MoveToLocation(GetWorkStationPosition());  // Di chuyển công nhân đến công việc
        }
        else
        {
            workerCtrl.ReturnHome();  // Di chuyển công nhân về nhà
        }
    }

    // Lấy vị trí công trình làm việc của công nhân
    private Vector3 GetWorkStationPosition()
    {
        // Logic để lấy vị trí công trình WorkStation
        return Vector3.zero;
    }

    // Lấy vị trí nhà của công nhân
    private Vector3 GetHomePosition()
    {
        // Logic để lấy vị trí nhà công nhân
        return Vector3.zero;
    }
}