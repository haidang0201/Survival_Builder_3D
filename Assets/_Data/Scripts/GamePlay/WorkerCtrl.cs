using UnityEngine;

public class WorkerCtrl : MonoBehaviour
{
    public WorkerMovement workerMovement;  // Quản lý di chuyển của công nhân
    public WorkerTasks workerTasks;        // Quản lý các tác vụ của công nhân
    public WorkerModel workerModel;        // Mô hình công nhân để ẩn hoặc hiển thị

    private void Awake()
    {
        workerMovement = GetComponent<WorkerMovement>();
        workerTasks = GetComponent<WorkerTasks>();
        workerModel = GetComponent<WorkerModel>();
    }

    // Di chuyển công nhân tới công việc hoặc nhà
    public void MoveToLocation(Vector3 targetPosition)
    {
        workerMovement.SetTarget(targetPosition);
    }

    // Công nhân trở về nhà
    public void ReturnHome()
    {
        BuildingCtrl homeBuilding = BuildingManager.Ins.GetHomeBuilding();
        if (homeBuilding != null)
        {
            MoveToLocation(homeBuilding.transform.position);
        }
    }

    // Hiển thị công nhân khi quay lại làm việc
    public void ComeBackToWork()
    {
        workerModel.SetActive(true);  // Hiển thị công nhân khi làm việc
    }

    // Ẩn công nhân khi vào nhà
    public void GoHome()
    {
        workerModel.SetActive(false);  // Ẩn công nhân khi vào nhà
        Debug.Log("Worker has returned home.");
    }
}