using UnityEngine;

/*
 * BuildingCtrl.cs
 * Folder: Scripts/Building/
 * Người làm: DŨNG / TIẾN
 *
 * Controller gắn lên prefab công trình.
 * Quản lý: xây dựng, worker, save/load trạng thái, xoay 90°.
 *
 * Luồng save:  ToState()   → BuildingState → JsonDataManager
 * Luồng load:  FromState() ← BuildingState ← JsonDataManager
 *
 * API chuẩn (các class khác phải dùng đúng tên này):
 *   ToState()   – export sang BuildingState
 *   FromState() – import từ BuildingState
 */

public class BuildingCtrl : MonoBehaviour
{
    // ================= INSPECTOR =================

    [Header("Config")]
    public BuildingType buildingType;

    [Header("References")]
    public Transform door;          // Vị trí worker đứng làm việc

    [Header("State – chỉ xem, không sửa tay")]
    [SerializeField] private float buildProgress = 0f;
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private int currentWorkers = 0;
    [SerializeField] private int maxWorkers = 4;
    internal string type;

    // // Thêm vào file BuildingCtrl.cs của bạn
    // public float currentHealth = 100f; 
    // public float maxHealth = 100f;

    // // Thêm giả lập số lính/thợ hiện tại để UI lấy dữ liệu test
    // public int currentWorkers = 1;
    // public int maxWorkers = 4;
    // public int currentSoldiers = 0;
    // public int maxSoldiers = 5;

    // ================= PROPERTIES =================

    public bool IsBuilt => buildProgress >= 1f;
    public bool IsOccupied => isOccupied;
    public bool IsAvailable => IsBuilt && !isOccupied;
    public int CurrentWorkers => currentWorkers;
    public int MaxWorkers => maxWorkers;

    /// <summary>Góc Y hiện tại (luôn là bội số 90°)</summary>
    public float CurrentYRotation => NormalizeAngle(transform.eulerAngles.y);

    // ================= LIFECYCLE =================

    private void Start()
    {
        // Khi nhà thật xuất hiện, lập tức ghi danh vào danh sách quản lý của Dũng
        if (BuildingManager.Ins != null)
        {
            BuildingManager.Ins.AddBuilding(this);
        }
    }

    private void OnDestroy()
    {
        // Khi nhà bị quái đánh sập hoặc bị bán, xóa tên khỏi danh sách để đất trống xây lại được
        if (BuildingManager.Ins != null)
        {
            BuildingManager.Ins.RemoveBuilding(this);
        }
    }

    // ================= PUBLIC – WORKER =================

    public void AssignWorker(WorkerCtrl worker)
    {
        if (!IsAvailable)
        {
            Debug.LogWarning($"[BuildingCtrl] {buildingType} không available, không thể gán worker.");
            return;
        }

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

    // ================= PUBLIC – BUILD =================

    public void AddProgress(float amount)
    {
        if (IsBuilt) return;

        buildProgress = Mathf.Clamp01(buildProgress + amount);
        Debug.Log($"[BuildingCtrl] {buildingType} buildProgress = {buildProgress}"); // thêm dòng này

        if (IsBuilt) OnBuildComplete();
    }

    public void CancelBuild()
    {
        buildProgress = 0f;
        isOccupied = false;
    }

    // ================= PUBLIC – ROTATION =================

    /// <summary>Xoay thêm 90° theo chiều Y (gọi từ GhostBuilding hoặc UI)</summary>
    public void RotateStep()
    {
        float newY = (CurrentYRotation + 90f) % 360f;
        transform.rotation = Quaternion.Euler(0f, newY, 0f);
    }

    /// <summary>Set góc xoay cụ thể – dùng khi load từ save</summary>
    public void SetRotation(float yDegrees)
    {
        float snapped = SnapRotation(yDegrees);
        transform.rotation = Quaternion.Euler(0f, snapped, 0f);
    }

    // ================= PUBLIC – SAVE / LOAD =================

    /// <summary>
    /// Export trạng thái hiện tại → BuildingState để lưu JSON.
    /// Tên chuẩn: ToState() – không đổi tên, các class khác phụ thuộc vào tên này.
    /// </summary>
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

    /// <summary>
    /// Import BuildingState → restore trạng thái sau khi load JSON.
    /// Gọi NGAY SAU khi SpawnBuilding() để tránh Start() đăng ký hai lần.
    /// </summary>
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

    // ================= PRIVATE =================

    private void OnBuildComplete()
    {
        // Đổi lời gọi cũ sang gọi CampaignTutorialManager
        if (buildingType == BuildingType.WatchTower)
        {
            CampaignTutorialManager.Ins?.OnWatchTowerPlaced();
        }
        
        if (buildingType == BuildingType.WoodCutter || buildingType == BuildingType.StoneStorage)
        {
            CampaignTutorialManager.Ins?.OnCivilBuildingPlaced(buildingType);
        }
    }

    /// <summary>Snap góc về bội số 90° gần nhất</summary>
    private float SnapRotation(float angle)
    {
        return Mathf.Round(angle / 90f) * 90f % 360f;
    }

    private float NormalizeAngle(float angle)
    {
        return (angle % 360f + 360f) % 360f;
    }
}