using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Gắn vào prefab Nhà / Building để tự động tạo NavMeshObstacle khi đặt lúc runtime,
/// giúp AI worker KHÔNG đi xuyên qua tòa nhà nữa.
///
/// CÁCH DÙNG:
///   1. Gắn script này vào prefab House (hoặc bất kỳ building nào).
///   2. Không cần làm gì thêm — script tự đọc Collider và setup NavMeshObstacle.
///   3. Nếu muốn tinh chỉnh kích thước thủ công, bật "Override Size" trong Inspector.
///
/// LƯU Ý:
///   - NavMesh Carve hoạt động runtime mà KHÔNG cần rebake NavMesh.
///   - Yêu cầu NavMeshAgent trên worker đặt "Auto Repath" = true (mặc định của Unity).
///   - Nếu dùng NavMesh Surface package, cần đảm bảo NavMeshSurface tồn tại trong scene.
/// </summary>
[DisallowMultipleComponent]
public class BuildingNavMeshObstacle : MonoBehaviour
{
    [Header("Override thủ công (để trống = tự tính từ Collider)")]
    [Tooltip("Bật để tự nhập kích thước thay vì tính tự động từ Collider")]
    public bool overrideSize = false;

    [Tooltip("Kích thước vùng cản (chỉ dùng khi Override Size = true)")]
    public Vector3 manualSize = new Vector3(4f, 3f, 4f);

    [Tooltip("Offset tâm so với pivot (chỉ dùng khi Override Size = true)")]
    public Vector3 manualCenter = Vector3.zero;

    [Header("Carve Settings")]
    [Tooltip("Khi true: NavMesh sẽ có lỗ hổng đúng hình nhà — worker sẽ đi vòng quanh")]
    public bool carve = true;

    [Tooltip("Thời gian chờ trước khi carve (giây). Để 0 nếu nhà đặt cố định, > 0 nếu nhà có thể di chuyển)")]
    public float carveOnlyStationary = 0.5f;

    [Tooltip("Khoảng cách tối thiểu di chuyển mới tính là 'đang chuyển động' (để carve đúng lúc dừng)")]
    public float movementThreshold = 0.1f;

    // ─── Internal ───────────────────────────────────────────────────────────────

    private NavMeshObstacle obstacle;

    void Awake()
    {
        SetupObstacle();
    }

    void SetupObstacle()
    {
        // Tránh thêm trùng
        obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
            obstacle = gameObject.AddComponent<NavMeshObstacle>();

        obstacle.carving              = carve;
        obstacle.carveOnlyStationary  = carveOnlyStationary > 0f;
        obstacle.carvingMoveThreshold = movementThreshold;
        obstacle.carvingTimeToStationary = carveOnlyStationary;

        if (overrideSize)
        {
            obstacle.shape  = NavMeshObstacleShape.Box;
            obstacle.size   = manualSize;
            obstacle.center = manualCenter;
        }
        else
        {
            AutoFitFromCollider();
        }
    }

    /// <summary>
    /// Đọc Collider (Box/Capsule/Sphere/Mesh) rồi áp kích thước tương ứng lên NavMeshObstacle.
    /// </summary>
    void AutoFitFromCollider()
    {
        // Ưu tiên BoxCollider vì dễ map nhất
        BoxCollider box = GetComponent<BoxCollider>() ?? GetComponentInChildren<BoxCollider>();
        if (box != null)
        {
            obstacle.shape  = NavMeshObstacleShape.Box;
            obstacle.size   = box.size;
            obstacle.center = box.center;
            return;
        }

        CapsuleCollider cap = GetComponent<CapsuleCollider>() ?? GetComponentInChildren<CapsuleCollider>();
        if (cap != null)
        {
            obstacle.shape  = NavMeshObstacleShape.Capsule;
            obstacle.radius = cap.radius;
            obstacle.height = cap.height;
            obstacle.center = cap.center;
            return;
        }

        // MeshCollider / SphereCollider / fallback: dùng Bounds tổng của toàn object
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            // Chuyển bounds từ world space về local space
            Vector3 worldSize   = bounds.size;
            Vector3 localSize   = new Vector3(
                worldSize.x / transform.lossyScale.x,
                worldSize.y / transform.lossyScale.y,
                worldSize.z / transform.lossyScale.z
            );
            Vector3 localCenter = transform.InverseTransformPoint(bounds.center);

            obstacle.shape  = NavMeshObstacleShape.Box;
            obstacle.size   = localSize;
            obstacle.center = localCenter;
            return;
        }

        // Không tìm được gì → fallback mặc định
        Debug.LogWarning($"[BuildingNavMeshObstacle] '{name}': Không tìm thấy Collider hoặc Renderer nào. " +
                          $"Dùng kích thước mặc định 4x3x4. Hãy tick 'Override Size' và tự nhập cho đúng.");
        obstacle.shape  = NavMeshObstacleShape.Box;
        obstacle.size   = new Vector3(4f, 3f, 4f);
        obstacle.center = Vector3.up * 1.5f;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Vẽ preview vùng cản trong Scene View khi chưa chạy game
        if (obstacle != null) return; // Runtime: Unity tự vẽ

        Color c = carve ? new Color(1f, 0.4f, 0f, 0.25f) : new Color(1f, 0f, 0f, 0.25f);
        Gizmos.color = c;

        Vector3 size   = overrideSize ? manualSize   : new Vector3(4f, 3f, 4f);
        Vector3 center = overrideSize ? manualCenter : Vector3.up * 1.5f;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(c.r, c.g, c.b, 0.8f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
