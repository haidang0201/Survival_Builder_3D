using UnityEngine;

public class BuildingCtrl : MonoBehaviour
{
    public BuildingType buildingType;  // Loại công trình (WorkStation, Home, ...)
    public Transform door;             // Cửa vào của tòa nhà
    public bool isOccupied = false;    // Kiểm tra xem tòa nhà có đang được công nhân sử dụng không
    public float buildProgress = 0f;   // Tiến độ xây dựng của công trình (0 - 1)

    // Trả về loại công trình (WorkStation, Home, ...)
    public BuildingType GetBuildingType()
    {
        return buildingType;
    }

    // Kiểm tra xem công trình có sẵn cho công nhân làm việc không
    public bool IsAvailableForWork()
    {
        return !isOccupied && buildProgress >= 1f;  // Công trình phải hoàn thành và chưa có công nhân làm việc
    }

    // Xử lý công nhân làm việc tại tòa nhà này
    public void AssignWorker(WorkerCtrl workerCtrl)
    {
        if (IsAvailableForWork())
        {
            isOccupied = true;
            workerCtrl.MoveToLocation(door.position);  // Di chuyển công nhân đến cửa tòa nhà
            Debug.Log("Worker assigned to " + buildingType);
        }
    }

    // Công nhân hoàn thành công việc tại tòa nhà
    public void WorkerCompleteTask(WorkerCtrl workerCtrl)
    {
        isOccupied = false;
        workerCtrl.ComeBackToWork();  // Đưa công nhân trở lại làm việc
        Debug.Log("Worker has completed task at " + buildingType);
    }

    // Hàm để xây dựng tòa nhà (sử dụng cho các tòa nhà có tiến độ xây dựng)
    public void Build(float progress)
    {
        buildProgress += progress;
        if (buildProgress > 1f)
        {
            buildProgress = 1f;
        }

        Debug.Log("Building progress: " + (buildProgress * 100) + "%");
    }

    // Hàm hủy bỏ công trình nếu cần thiết
    public void CancelBuilding()
    {
        buildProgress = 0f;
        Debug.Log("Building construction canceled.");
    }
}