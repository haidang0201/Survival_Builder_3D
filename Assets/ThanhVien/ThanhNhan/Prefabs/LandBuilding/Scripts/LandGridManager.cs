using System.Collections.Generic;
using UnityEngine;

public enum ExpandDirection
{
    North, // +Z (Trên)
    South, // -Z (Dưới)
    East,  // +X (Phải)
    West   // -X (Trái)
}

public class LandGridManager : MonoBehaviour
{
    public static LandGridManager Ins { get; private set; }

    [Header("=== CẤU HÌNH Ô ĐẤT ===")]
    [Tooltip("Prefab của 1 ô đất (có MeshRenderer & Collider)")]
    [SerializeField] private GameObject landPlotPrefab; 
    [Tooltip("Kích thước chiều dài / rộng của 1 ô đất (ví dụ 10m x 10m)")]
    [SerializeField] private float tileSize = 10f;
    [Tooltip("Layer dành riêng cho ô đất cho phép xây dựng")]
    [SerializeField] private int buildableLayerIndex = 8; // Layer 'Building' hoặc 'Buildable'

    [Header("=== CẤU HÌNH HÀNG RÀO ===")]
    [Tooltip("Prefab của đoạn hàng rào")]
    [SerializeField] private GameObject fencePrefab;

    [Header("=== CẤU HÌNH VỊ TRÍ SPAWN ===")]
    [Tooltip("Đẩy ô đất lên cao một chút để không bị chìm vào mặt đất")]
    [SerializeField] private float plotSpawnY = 1.3f;

    [Header("=== 4 NÚT MỞ RỘNG (+) ===")]
    [SerializeField] private Transform btnNorth; // Nút + phía Bắc (+Z)
    [SerializeField] private Transform btnSouth; // Nút + phía Nam (-Z)
    [SerializeField] private Transform btnEast;  // Nút + phía Đông (+X)
    [SerializeField] private Transform btnWest;  // Nút + phía Tây (-X)

    // Lưu trữ danh sách ô đất theo tọa độ ma trận (X, Z)
    private Dictionary<Vector2Int, GameObject> activePlots = new Dictionary<Vector2Int, GameObject>();
    private List<GameObject> activeFences = new List<GameObject>();

    // Giới hạn chỉ số Ma trận hiện tại
    private int minX = 0, maxX = 1;
    private int minZ = 0, maxZ = 1;

    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Khởi tạo vùng đất mặc định ban đầu (Kích thước 2x2 ô)
        InitializeGrid(2, 2);
    }

    /// <summary>
    /// Khởi tạo vùng đất mặc định lúc mới vào game
    /// </summary>
    public void InitializeGrid(int width, int height)
    {
        minX = 0; maxX = width - 1;
        minZ = 0; maxZ = height - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                SpawnPlot(x, z);
            }
        }

        RebuildFences();
        UpdateExpandButtonsPosition();
    }

    /// <summary>
    /// Tạo 1 ô đất tại tọa độ Ma trận (x, z)
    /// </summary>
    private void SpawnPlot(int x, int z)
    {
        Vector2Int gridCoord = new Vector2Int(x, z);
        if (activePlots.ContainsKey(gridCoord)) return;

        Vector3 worldPos = GetWorldPosition(x, z, plotSpawnY);
        GameObject plot = Instantiate(landPlotPrefab, worldPos, Quaternion.identity, transform);
        
        // Đảm bảo ô đất nằm đúng Layer được xây dựng
        plot.layer = buildableLayerIndex;
        // Đảm bảo các con của ô đất cũng đổi Layer (nếu có)
        foreach (Transform child in plot.transform)
        {
            child.gameObject.layer = buildableLayerIndex;
        }

        activePlots.Add(gridCoord, plot);
    }

    /// <summary>
    /// Hàm gọi khi bấm vào 1 trong 4 nút (+) để mở rộng đất
    /// </summary>
    public void ExpandGrid(ExpandDirection direction)
    {
        // Có thể chèn code kiểm tra Tiền / Gỗ / Đá ở đây trước khi cho mở rộng
        // if (!JsonDataManager.Ins.TrySpendCombined(...)) return;

        switch (direction)
        {
            case ExpandDirection.North: // Mở rộng lên trên (+Z)
                maxZ++;
                for (int x = minX; x <= maxX; x++) SpawnPlot(x, maxZ);
                break;

            case ExpandDirection.South: // Mở rộng xuống dưới (-Z)
                minZ--;
                for (int x = minX; x <= maxX; x++) SpawnPlot(x, minZ);
                break;

            case ExpandDirection.East:  // Mở rộng sang phải (+X)
                maxX++;
                for (int z = minZ; z <= maxZ; z++) SpawnPlot(maxX, z);
                break;

            case ExpandDirection.West:  // Mở rộng sang trái (-X)
                minX--;
                for (int z = minZ; z <= maxZ; z++) SpawnPlot(minX, z);
                break;
        }

        // Tái tạo lại hàng rào bao quanh và dời nút (+) ra mép mới
        RebuildFences();
        UpdateExpandButtonsPosition();

        Debug.Log($"[LandGridManager] 🟩 Đã mở rộng đất theo hướng {direction}!");
    }

    /// <summary>
    /// Tính toán dựng lại hàng rào ôm trọn chu vi bên ngoài vùng đất
    /// </summary>
    private void RebuildFences()
    {
        // 1. Xóa hàng rào cũ
        foreach (var fence in activeFences)
        {
            if (fence != null) Destroy(fence);
        }
        activeFences.Clear();

        if (fencePrefab == null) return;

        float halfTile = tileSize / 2f;

        // 2. Dựng hàng rào Cạnh Bắc (+Z) & Cạnh Nam (-Z)
        for (int x = minX; x <= maxX; x++)
        {
            Vector3 northPos = GetWorldPosition(x, maxZ) + new Vector3(0, 0, halfTile);
            SpawnFence(northPos, 0f); // Mặt hướng Bắc

            Vector3 southPos = GetWorldPosition(x, minZ) + new Vector3(0, 0, -halfTile);
            SpawnFence(southPos, 180f); // Mặt hướng Nam
        }

        // 3. Dựng hàng rào Cạnh Đông (+X) & Cạnh Tây (-X)
        for (int z = minZ; z <= maxZ; z++)
        {
            Vector3 eastPos = GetWorldPosition(maxX, z) + new Vector3(halfTile, 0, 0);
            SpawnFence(eastPos, 90f); // Mặt hướng Đông

            Vector3 westPos = GetWorldPosition(minX, z) + new Vector3(-halfTile, 0, 0);
            SpawnFence(westPos, -90f); // Mặt hướng Tây
        }
    }

    private void SpawnFence(Vector3 pos, float rotationY)
    {
        GameObject fence = Instantiate(fencePrefab, pos, Quaternion.Euler(0, rotationY, 0), transform);
        activeFences.Add(fence);
    }

    /// <summary>
    /// Di chuyển 4 nút (+) ra vị trí trung tâm mép ngoài cùng
    /// </summary>
    private void UpdateExpandButtonsPosition()
    {
        float centerX = (minX + maxX) * tileSize / 2f;
        float centerZ = (minZ + maxZ) * tileSize / 2f;
        float offset = tileSize / 2f + 1.5f; // Đẩy nút ra ngoài hàng rào một chút

        if (btnNorth != null) btnNorth.position = new Vector3(centerX, 0.5f, maxZ * tileSize + offset);
        if (btnSouth != null) btnSouth.position = new Vector3(centerX, 0.5f, minZ * tileSize - offset);
        if (btnEast != null)  btnEast.position  = new Vector3(maxX * tileSize + offset, 0.5f, centerZ);
        if (btnWest != null)  btnWest.position  = new Vector3(minX * tileSize - offset, 0.5f, centerZ);
    }

    /// <summary>
    /// Chuyển từ Tọa độ Ma trận (x, z) sang Tọa độ World Space (X, Y, Z)
    /// </summary>
    public Vector3 GetWorldPosition(int x, int z, float yOffset = 0f)
    {
        return new Vector3(x * tileSize, yOffset, z * tileSize);
    }
}