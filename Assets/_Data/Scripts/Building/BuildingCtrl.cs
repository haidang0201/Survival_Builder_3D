using UnityEngine;

/*
 * BuildingCtrl.cs
 * Folder: Scripts/Building/
 * Dự án: KHẨN HOANG (PENTA DEV)
 */

public class BuildingCtrl : MonoBehaviour
{
    [Header("Config")]
    public BuildingType buildingType;

    [Header("References")]
    public Transform door;          

    [Header("State – chỉ xem, không sửa tay")]
    [SerializeField] private float buildProgress = 0f;
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private int currentWorkers = 0;
    [SerializeField] private int maxWorkers = 4;
    internal string type;

    public bool IsBuilt => buildProgress >= 1f;
    public bool IsOccupied => isOccupied;
    public bool IsAvailable => IsBuilt && !isOccupied;
    public int CurrentWorkers => currentWorkers;
    public int MaxWorkers => maxWorkers;

    public float CurrentYRotation => NormalizeAngle(transform.eulerAngles.y);

    private void Start()
    {
        if (BuildingManager.Ins != null)
        {
            BuildingManager.Ins.AddBuilding(this);
        }
    }

    private void OnDestroy()
    {
        if (BuildingManager.Ins != null)
        {
            BuildingManager.Ins.RemoveBuilding(this);
        }
    }

    public void AssignWorker(WorkerCtrl worker)
    {
        if (!IsAvailable) return;

        if (currentWorkers < maxWorkers)
        {
            currentWorkers++;
        }
        isOccupied = true;
        worker.MoveToLocation(door.position);
    }

    public void ReleaseWorker(WorkerCtrl worker)
    {
        if (currentWorkers > 0)
        {
            currentWorkers--;
        }
        isOccupied = false;
        worker.ComeBackToWork();
    }

    public void SetWorkerState(int current, int max)
    {
        maxWorkers = Mathf.Max(0, max);
        currentWorkers = Mathf.Clamp(current, 0, maxWorkers);
    }

    public void AddProgress(float amount)
    {
        if (IsBuilt) return;

        buildProgress = Mathf.Clamp01(buildProgress + amount);

        if (IsBuilt) OnBuildComplete();
    }

    public void CancelBuild()
    {
        buildProgress = 0f;
        isOccupied = false;
    }

    public void RotateStep()
    {
        float newY = (CurrentYRotation + 90f) % 360f;
        transform.rotation = Quaternion.Euler(0f, newY, 0f);
    }

    public void SetRotation(float yDegrees)
    {
        float snapped = SnapRotation(yDegrees);
        transform.rotation = Quaternion.Euler(0f, snapped, 0f);
    }

    public BuildingState ToState()
    {
        return new BuildingState
        {
            buildingType = buildingType,
            prefabName = gameObject.name,
            position = new SerializableVector3(transform.position),
            rotation = new SerializableVector3(transform.eulerAngles),
            buildProgress = buildProgress,
            isBuilt = IsBuilt,
            isOccupied = isOccupied,
            currentWorkers = currentWorkers,
            maxWorkers = maxWorkers,
            level = 0
        };
    }

    public void FromState(BuildingState state)
    {
        buildingType = state.buildingType;
        buildProgress = state.buildProgress;
        isOccupied = state.isOccupied;
        maxWorkers = Mathf.Max(0, state.maxWorkers);
        currentWorkers = Mathf.Clamp(state.currentWorkers, 0, maxWorkers);

        transform.position = state.position.ToVector3();
        transform.eulerAngles = state.rotation.ToVector3();
    }

    private void OnBuildComplete()
    {
        // 🔥 CẬP NHẬT TUTORIAL: Thông báo công trình ĐÃ XÂY XONG HOÀN TOÀN (Progress = 100%)
        CampaignTutorialManager.Ins?.OnBuildingConstructionFinished(buildingType);
    }

    private float SnapRotation(float angle)
    {
        return Mathf.Round(angle / 90f) * 90f % 360f;
    }

    private float NormalizeAngle(float angle)
    {
        return (angle % 360f + 360f) % 360f;
    }
}