using UnityEngine;

/*
 * StorageHUDAutoSetup.cs
 * Folder: ThanhVien/NhatTien/Script/UI/StorageHUD/
 * Người làm: TIẾN
 *
 * CHỨC NĂNG:
 * Tự động quét toàn bộ Scene, tìm tất cả WoodStorage / RiceStorage / StoneStorage,
 * gắn StorageSlotHUD vào từng kho và nối reference hudPanel — không cần làm tay gì cả.
 *
 * SETUP (1 bước duy nhất):
 * - Gắn script này VÀO CÙNG GameObject với StorageHUDPanel (object "StorageHUD").
 * - Ấn Play → xong, tự chạy hết.
 *
 * KHÔNG xung đột: chỉ AddComponent nếu chưa có, không đụng vào WoodStorage / RiceStorage /
 * StoneStorage / BuildingCtrl / BuildingManager hay bất kỳ script nào khác.
 */
public class StorageHUDAutoSetup : MonoBehaviour
{
    [Header("Tự động quét lại khi có kho mới xây (dùng cho build lúc runtime)")]
    [Tooltip("Bật thì cứ mỗi N giây quét lại 1 lần để bắt kho mới xây.")]
    public bool rescanPeriodically = false;
    public float rescanInterval    = 5f;

    private StorageHUDPanel _panel;
    private float           _timer;

    // ══════════════════════════════════════════════
    void Awake()
    {
        _panel = GetComponent<StorageHUDPanel>();
        if (_panel == null)
            _panel = GetComponentInChildren<StorageHUDPanel>();

        if (_panel == null)
        {
            Debug.LogError("[StorageHUDAutoSetup] Không tìm thấy StorageHUDPanel trên cùng object! " +
                           "Đảm bảo StorageHUDPanel.cs cũng được gắn vào object này.");
            enabled = false;
            return;
        }
    }

    void Start()
    {
        ScanAndSetup();
    }

    void Update()
    {
        if (!rescanPeriodically) return;
        _timer += Time.deltaTime;
        if (_timer >= rescanInterval)
        {
            _timer = 0f;
            ScanAndSetup();
        }
    }

    // ══════════════════════════════════════════════
    // CORE
    // ══════════════════════════════════════════════

    private void ScanAndSetup()
    {
        int woodCount = 0, riceCount = 0, stoneCount = 0;

        // ── WoodStorage ──────────────────────────
        WoodStorage[] woodStorages = FindObjectsByType<WoodStorage>(FindObjectsSortMode.None);
        foreach (WoodStorage ws in woodStorages)
        {
            StorageSlotHUD hud = EnsureHUD(ws.gameObject);
            hud.storageType  = StorageSlotHUD.StorageType.Wood;
            hud.hudPanel     = _panel;
            hud.woodStorage  = ws;
            hud.riceStorage  = null;
            hud.stoneStorage = null;
            woodCount++;
        }

        // ── RiceStorage ──────────────────────────
        RiceStorage[] riceStorages = FindObjectsByType<RiceStorage>(FindObjectsSortMode.None);
        foreach (RiceStorage rs in riceStorages)
        {
            StorageSlotHUD hud = EnsureHUD(rs.gameObject);
            hud.storageType  = StorageSlotHUD.StorageType.Rice;
            hud.hudPanel     = _panel;
            hud.riceStorage  = rs;
            hud.woodStorage  = null;
            hud.stoneStorage = null;
            riceCount++;
        }

        // ── StoneStorage ─────────────────────────
        StoneStorage[] stoneStorages = FindObjectsByType<StoneStorage>(FindObjectsSortMode.None);
        foreach (StoneStorage ss in stoneStorages)
        {
            StorageSlotHUD hud = EnsureHUD(ss.gameObject);
            hud.storageType  = StorageSlotHUD.StorageType.Stone;
            hud.hudPanel     = _panel;
            hud.stoneStorage = ss;
            hud.woodStorage  = null;
            hud.riceStorage  = null;
            stoneCount++;
        }

        Debug.Log($"[StorageHUDAutoSetup] Setup xong: {woodCount} Kho Gỗ | {riceCount} Kho Lúa | {stoneCount} Kho Đá");
    }

    /// <summary>
    /// Trả về StorageSlotHUD trên object đó.
    /// Nếu chưa có thì AddComponent mới.
    /// </summary>
    private StorageSlotHUD EnsureHUD(GameObject go)
    {
        StorageSlotHUD existing = go.GetComponent<StorageSlotHUD>();
        return existing != null ? existing : go.AddComponent<StorageSlotHUD>();
    }
}
