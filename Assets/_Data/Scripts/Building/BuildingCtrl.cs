using UnityEngine;

/*
 * BuildingCtrl.cs
 * Folder: Scripts/Building/
 * Người làm: DŨNG / TIẾN
 *
 * Controller gắn lên prefab công trình
 * Quản lý trạng thái: xây dựng, worker, hoàn thành
 */

public class BuildingCtrl : MonoBehaviour
{
    // ================= INSPECTOR =================

    [Header("Config")]
    public BuildingType buildingType;

    [Header("References")]
    public Transform door;              // Vị trí worker đến làm việc

    [Header("State")]
    public float buildProgress = 0f;   // 0.0 → 1.0
    public bool isOccupied = false;

    // ================= PROPERTIES =================

    public bool IsBuilt => buildProgress >= 1f;
    public bool IsAvailable => IsBuilt && !isOccupied;

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

        if (IsBuilt)
            OnBuildComplete();
    }

    public void CancelBuild()
    {
        buildProgress = 0f;
        isOccupied = false;
        Debug.Log($"[BuildingCtrl] Hủy xây dựng {buildingType}");
    }

    // ================= PUBLIC – DATA =================

    public BuildingData GetData()
    {
        return new BuildingData
        {
            buildingType = buildingType,
            prefabName = gameObject.name,
            defaultPosition = new SerializableVector3(transform.position),
            defaultRotation = new SerializableVector3(transform.eulerAngles),
            isBuilt = IsBuilt,
            level = 0
        };
    }

    // ================= PRIVATE =================

    private void OnBuildComplete()
    {
        Debug.Log($"[BuildingCtrl] ✅ {buildingType} xây xong!");
        // TODO: play VFX, notify BuildingManager nếu cần
    }
}