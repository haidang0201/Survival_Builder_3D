using UnityEngine;

/*
 * BuildingCtrl.cs
 * Folder: Scripts/Building/
 * Người làm: DŨNG / TIẾN
 *
 * Controller gắn lên prefab công trình
 * Quản lý: xây dựng, worker, save/load trạng thái, xoay 90°
 *
 * Luồng save:  ToState()   → BuildingState → JsonDataManager
 * Luồng load:  FromState() ← BuildingState ← JsonDataManager
 */

public class BuildingCtrl : MonoBehaviour
{
    // ================= INSPECTOR =================

    [Header("Config")]
    public BuildingType buildingType;

    [Header("References")]
    public Transform door;              // Vị trí worker đứng làm việc

    [Header("State – chỉ xem, không sửa tay")]
    [SerializeField] private float buildProgress = 0f;
    [SerializeField] private bool isOccupied = false;

    // ================= PROPERTIES =================

    public bool IsBuilt => buildProgress >= 1f;
    public bool IsAvailable => IsBuilt && !isOccupied;

    /// <summary>Góc Y hiện tại (luôn là bội số 90°)</summary>
    public float CurrentYRotation => NormalizeAngle(transform.eulerAngles.y);

    // ================= LIFECYCLE =================

    private void Start()
    {
        BuildingManager.Ins.AddBuilding(this);
    }

    private void OnDestroy()
    {
        BuildingManager.Ins?.RemoveBuilding(this);
    }

    // ================= PUBLIC – WORKER =================

    public void AssignWorker(WorkerCtrl worker)
    {
        if (!IsAvailable)
        {
            Debug.LogWarning($"[BuildingCtrl] {buildingType} không sẵn sàng nhận worker!");
            return;
        }

        isOccupied = true;
        worker.MoveToLocation(door.position);
        Debug.Log($"[BuildingCtrl] Worker được giao đến {buildingType}");
    }

    public void ReleaseWorker(WorkerCtrl worker)
    {
        isOccupied = false;
        worker.ComeBackToWork();
        Debug.Log($"[BuildingCtrl] Worker hoàn thành tại {buildingType}");
    }

    // ================= PUBLIC – BUILD =================

    public void AddProgress(float amount)
    {
        if (IsBuilt) return;

        buildProgress = Mathf.Clamp01(buildProgress + amount);
        Debug.Log($"[BuildingCtrl] {buildingType} tiến độ: {buildProgress * 100:F0}%");

        if (IsBuilt) OnBuildComplete();
    }

    public void CancelBuild()
    {
        buildProgress = 0f;
        isOccupied = false;
        Debug.Log($"[BuildingCtrl] Hủy xây dựng {buildingType}");
    }

    // ================= PUBLIC – ROTATION =================

    /// <summary>Xoay thêm 90° theo chiều Y (gọi từ GhostBuilding)</summary>
    public void RotateStep()
    {
        float newY = (CurrentYRotation + 90f) % 360f;
        transform.rotation = Quaternion.Euler(0f, newY, 0f);
        Debug.Log($"[BuildingCtrl] Xoay: {newY}°");
    }

    /// <summary>Set góc xoay cụ thể (dùng khi load từ save)</summary>
    public void SetRotation(float yDegrees)
    {
        float snapped = SnapRotation(yDegrees);
        transform.rotation = Quaternion.Euler(0f, snapped, 0f);
    }

    // ================= PUBLIC – SAVE / LOAD =================

    /// <summary>Export trạng thái hiện tại → BuildingState để lưu JSON</summary>
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
            level = 0
        };
    }

    /// <summary>Import BuildingState → restore trạng thái sau khi load JSON</summary>
    public void FromState(BuildingState state)
    {
        buildingType = state.buildingType;
        buildProgress = state.buildProgress;
        isOccupied = state.isOccupied;

        transform.position = state.position.ToVector3();
        transform.eulerAngles = state.rotation.ToVector3();

        Debug.Log($"[BuildingCtrl] Loaded: {buildingType} | Xoay: {CurrentYRotation}° | Built: {IsBuilt}");
    }

    // ================= PRIVATE =================

    private void OnBuildComplete()
    {
        Debug.Log($"[BuildingCtrl] ✅ {buildingType} xây xong!");
        // TODO: VFXManager.Ins.Play("BuildComplete", transform.position);
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