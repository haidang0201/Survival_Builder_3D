using UnityEngine;

public abstract class WorkerTask : MonoBehaviour
{
    protected WorkerCtrl workerCtrl;  // Điều khiển công nhân
    [SerializeField] protected float workDistance = 2f;  // Khoảng cách làm việc
    protected bool isWorking = false;  // Trạng thái làm việc

    protected virtual void Awake()
    {
        workerCtrl = GetComponent<WorkerCtrl>();  // Lấy WorkerCtrl từ GameObject
    }

    // Cập nhật trạng thái công nhân và tìm công việc hoặc thực hiện công việc
    protected virtual void FixedUpdate()
    {
        if (!workerCtrl.workerTasks.readyForTask)
        {
            FindWork();  // Tìm công việc nếu công nhân chưa sẵn sàng
        }
        else
        {
            PerformWork();  // Nếu sẵn sàng, thực hiện công việc
        }
    }

    // Tìm công việc cho công nhân (phải được implement trong các class con)
    protected abstract void FindWork();

    // Thực hiện công việc khi công nhân đã đến nơi (phải được implement trong các class con)
    protected abstract void PerformWork();
}