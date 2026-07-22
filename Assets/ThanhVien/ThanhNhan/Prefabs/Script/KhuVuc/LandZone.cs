using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LandZone.cs
/// Người làm: ThanhNhan
///
/// Quản lý trạng thái khóa / mở của một vùng đất mới trên bản đồ.
/// - Click chuột vào vùng đất → mở LandUnlockPanel (giống cơ chế mỏ đá)
/// - Khi bị khóa: BoxCollider bật → chặn mọi click xuống các object bên dưới
/// - Khi mở khóa: BoxCollider tắt → click xuyên qua, tương tác bình thường
/// - Lưu trạng thái mở khóa bằng PlayerPrefs (bền vững qua các lần restart)
///
/// Setup trong Unity Inspector:
///   1. Gán script này lên GameObject vùng đất (cần có Collider)
///   2. Kéo LockCanvas (icon ổ khóa 3D) vào lockCanvas
///   3. Kéo LandUnlockPanel (Canvas UI) vào landUnlockPanel
/// </summary>
public class LandZone : MonoBehaviour
{
    [Header("Lock System")]
    [Tooltip("Icon ổ khóa nổi trên vùng đất (Billboard UI)")]
    public GameObject lockCanvas;
    [Tooltip("GameObject chứa sương mù (Fog_Visual) để làm hiệu ứng sương tan")]
    public GameObject fogVisual;
    [Tooltip("Thời gian sương tan dần (giây)")]
    public float fogDissolveDuration = 2.5f;
    [Tooltip("Trạng thái khóa ban đầu — thường để true")]
    public bool isLocked = true;

    [Header("UI Panel Ref")]
    [Tooltip("Kéo LandUnlockPanel vào đây")]
    public GameObject landUnlockPanel;

    [Header("Firework Celebration System")]
    [Tooltip("Mảng các Prefab hiệu ứng pháo hoa để bắn ngẫu nhiên")]
    [SerializeField] private GameObject[] fireworkPrefabs;
    [Tooltip("Số lượng pháo hoa sẽ phát nổ sau khi sương tan hết")]
    [SerializeField] private int fireworkCount = 6;
    [Tooltip("Bán kính khu vực bắn pháo hoa tính từ tâm vùng đất này")]
    [SerializeField] private float fireworkSpawnRadius = 8f;
    [Tooltip("Thời gian giãn cách tối thiểu giữa các phát bắn (giây)")]
    [SerializeField] private float minFireworkDelay = 0.15f;
    [Tooltip("Thời gian giãn cách tối đa giữa các phát bắn (giây)")]
    [SerializeField] private float maxFireworkDelay = 0.5f;

    // ─── Private ─────────────────────────────────────────────────────────────
    private LandUnlockManager _panelManager;
    private bool _ignorePanelOpen = false;
    private Coroutine _ignoreCoroutine;

    // ─── Unity Lifecycle ─────────────────────────────────────────────────────

    void Start()
    {
        // ── Auto-find Fog_Visual (Màn sương mù) ──────────────────────────────
        if (fogVisual == null || !fogVisual.transform.IsChildOf(transform))
        {
            Transform found = transform.Find("Fog_Visual");
            if (found != null)
            {
                fogVisual = found.gameObject;
                Debug.Log($"[LandZone] '{gameObject.name}' tự tìm thấy Fog_Visual.");
            }
        }

        // ── Auto-find LockCanvas (Icon ổ khóa) ───────────────────────────────
        if (lockCanvas == null || !lockCanvas.transform.IsChildOf(transform))
        {
            // Thử tìm theo tên phổ biến
            Transform found = transform.Find("LockIcon");
            if (found == null) found = transform.Find("LockCanvas");
            if (found == null) found = transform.Find("Lock_Canvas");
            
            // Thử tìm bất kỳ child nào có BillboardUI (đặc trưng của LockIcon)
            if (found == null)
            {
                BillboardUI billboard = GetComponentInChildren<BillboardUI>(true);
                if (billboard != null && billboard.transform != transform)
                {
                    found = billboard.transform;
                }
            }

            if (found != null)
            {
                lockCanvas = found.gameObject;
                Debug.Log($"[LandZone] '{gameObject.name}' tự tìm thấy LockIcon/LockCanvas.");
            }
        }

        // Đã bỏ LoadLockState() để không tự động khôi phục trạng thái mở khóa khi mở lại game
        // LoadLockState();
        UpdateLockStatus();

        // ── Auto-find KhuVucCard qua nhiều cách (Tag / Name / Inactive search) ─
        if (landUnlockPanel == null)
        {
            // Cách 1: Thử tag đầy đủ 'LandUnlockPanel'
            GameObject panel = GameObject.FindWithTag("LandUnlockPanel");
            
            // Cách 2: Thử tag viết tắt 'LandUnlockPan' (nếu user tạo tag bị thiếu chữ)
            if (panel == null)
            {
                try {
                    panel = GameObject.FindWithTag("LandUnlockPan");
                } catch { }
            }

            // Cách 3: Thử tìm trực tiếp theo tên GameObject 'KhuVucCard'
            if (panel == null)
            {
                panel = GameObject.Find("KhuVucCard");
                if (panel != null)
                {
                    Debug.Log($"[LandZone] '{gameObject.name}' tìm thấy panel bằng cách tìm tên 'KhuVucCard'.");
                }
            }

            // Cách 4 (Tốt nhất cho Object bị ẩn/Inactive từ đầu): Tìm qua Component trong Scene
            if (panel == null)
            {
                LandUnlockManager[] managers = Resources.FindObjectsOfTypeAll<LandUnlockManager>();
                foreach (var manager in managers)
                {
                    // Đảm bảo object nằm trong scene đang mở (không lấy nhầm prefab trong Assets)
                    if (manager.gameObject.scene.name != null)
                    {
                        panel = manager.gameObject;
                        _panelManager = manager;
                        Debug.Log($"[LandZone] '{gameObject.name}' tìm thấy panel bằng cách quét component LandUnlockManager ẩn.");
                        break;
                    }
                }
            }

            if (panel != null)
            {
                landUnlockPanel = panel;
                Debug.Log($"[LandZone] '{gameObject.name}' đã liên kết thành công với landUnlockPanel.");
            }
            else
            {
                Debug.LogError($"[LandZone] '{gameObject.name}' KHÔNG THỂ tìm thấy panel mở khóa! Vui lòng đảm bảo có GameObject tên 'KhuVucCard' hoặc gán Tag đúng.");
            }
        }

        if (landUnlockPanel != null)
        {
            landUnlockPanel.SetActive(false);
            _panelManager = landUnlockPanel.GetComponent<LandUnlockManager>();
        }
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Cập nhật hiển thị icon ổ khóa và BoxCollider theo trạng thái hiện tại.
    /// - isLocked=true:  Fog_Visual bật + LockIcon bật + BoxCollider bật → chặn click xuống dưới
    /// - isLocked=false: Fog_Visual tắt + LockIcon tắt + BoxCollider tắt → click xuyên qua bình thường
    /// </summary>
    public void UpdateLockStatus()
    {
        // Hiện/ẩn sương mù
        if (fogVisual != null)
            fogVisual.SetActive(isLocked);

        // Hiện/ẩn icon ổ khóa
        if (lockCanvas != null)
            lockCanvas.SetActive(isLocked);

        // Bật/tắt BoxCollider — khi unlock, click xuyên qua được
        var col = GetComponent<BoxCollider>();
        if (col != null)
            col.enabled = isLocked;
    }

    /// <summary>Mở khóa vùng đất — gọi bởi LandUnlockManager sau khi đủ tài nguyên.</summary>
    public void UnlockLand()
    {
        isLocked = false;
        
        // Tắt Box Collider ngay để người chơi tương tác được các công trình bên dưới ngay lập tức
        var col = GetComponent<BoxCollider>();
        if (col != null) col.enabled = false;

        // Tắt ngay lập tức icon ổ khóa
        if (lockCanvas != null)
            lockCanvas.SetActive(false);

        // Chạy hiệu ứng tan sương mù từ từ
        if (gameObject.activeInHierarchy && fogVisual != null && fogVisual.activeSelf)
        {
            StartCoroutine(Co_AnimateFogDissipation());
        }
        else
        {
            UpdateLockStatus();
            // Đã bỏ SaveLockState() để không lưu trạng thái mở khóa
            // SaveLockState();
        }
        
        Debug.Log($"[LandZone] Vùng đất '{gameObject.name}' đã kích hoạt hiệu ứng mở khóa.");
    }

    /// <summary>Coroutine chạy hiệu ứng tan sương mù mượt mà.</summary>
    private IEnumerator Co_AnimateFogDissipation()
    {
        // 1. Tìm tất cả Particle System con trong fogVisual
        ParticleSystem[] particleSystems = fogVisual.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                var emission = ps.emission;
                emission.enabled = false; // Ngừng tạo hạt sương mới
            }
        }

        // 2. Tìm tất cả Renderer con trong fogVisual (các tấm plane hiển thị sương)
        Renderer[] renderers = fogVisual.GetComponentsInChildren<Renderer>();
        List<Material> instantiatedMaterials = new List<Material>();
        List<Color> originalColors = new List<Color>();

        foreach (var ren in renderers)
        {
            if (ren != null && ren.material != null)
            {
                instantiatedMaterials.Add(ren.material); // Tự tạo instance material
                originalColors.Add(ren.material.color);
            }
        }

        // 3. Chạy animation mờ dần (Fade Out)
        float duration = fogDissolveDuration; // Sử dụng thông số tùy chỉnh từ Inspector
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Fade các tấm Plane sương
            for (int i = 0; i < instantiatedMaterials.Count; i++)
            {
                if (instantiatedMaterials[i] != null)
                {
                    Color c = originalColors[i];
                    c.a = Mathf.Lerp(c.a, 0f, t);
                    instantiatedMaterials[i].color = c;
                }
            }

            // Fade các hạt Particle đang bay lơ lửng bằng cách giảm thời gian sống còn lại
            // Cách này giúp kích hoạt tính năng tự tan (Color over Lifetime) tự nhiên của Unity mượt mà hơn
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    ParticleSystem.Particle[] activeParticles = new ParticleSystem.Particle[ps.particleCount];
                    int count = ps.GetParticles(activeParticles);
                    float remainingAnimTime = duration - elapsed;
                    
                    for (int j = 0; j < count; j++)
                    {
                        // Giới hạn thời gian sống còn lại của hạt không vượt quá thời gian còn lại của hiệu ứng
                        if (activeParticles[j].remainingLifetime > remainingAnimTime)
                        {
                            activeParticles[j].remainingLifetime = remainingAnimTime;
                        }
                    }
                    ps.SetParticles(activeParticles, count);
                }
            }

            yield return null;
        }

        // 4. Tắt hoàn toàn Object sương mù sau khi tan hết
        if (fogVisual != null)
            fogVisual.SetActive(false);

        // 5. Khôi phục lại màu gốc cho vật liệu (tránh lưu trạng thái trong suốt vĩnh viễn vào Asset của Unity)
        for (int i = 0; i < instantiatedMaterials.Count; i++)
        {
            if (instantiatedMaterials[i] != null)
            {
                instantiatedMaterials[i].color = originalColors[i];
            }
        }

        // Đã bỏ SaveLockState() để không lưu trạng thái mở khóa
        // SaveLockState();
        Debug.Log($"[LandZone] Vùng đất '{gameObject.name}' sương đã tan hết.");
    }


    /// <summary>Được gọi bởi LandUnlockManager khi đóng panel — kích hoạt cooldown click.</summary>
    public void NotifyPanelClosed()
    {
        if (_ignoreCoroutine != null) StopCoroutine(_ignoreCoroutine);
        _ignoreCoroutine = StartCoroutine(IgnoreOpenBriefly());
    }

    // ─── Save / Load ─────────────────────────────────────────────────────────
    // Key theo tên GameObject → mỗi vùng đất có 1 key riêng
    private string SaveKey => $"LandZone_Unlocked_{gameObject.name}";

    private void SaveLockState()
    {
        PlayerPrefs.SetInt(SaveKey, isLocked ? 0 : 1);
        PlayerPrefs.Save();
        Debug.Log($"[LandZone] Đã lưu: '{gameObject.name}' = unlocked");
    }

    private void LoadLockState()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return;
        isLocked = PlayerPrefs.GetInt(SaveKey, 1) == 0;
        Debug.Log($"[LandZone] Load: '{gameObject.name}' = {(isLocked ? "locked" : "unlocked")}");
    }

    /// <summary>[DEBUG] Reset vùng đất về trạng thái khóa để test lại.</summary>
    [ContextMenu("Reset Unlock State (Debug)")]
    private void DebugResetLockState()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        isLocked = true;
        UpdateLockStatus();
        Debug.Log($"[LandZone] 🔄 Reset: '{gameObject.name}' đã khóa lại. Bấm Play lại để test.");
    }

    // ─── Input Detection (giống ResourceNode — raycast click) ────────────────

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        
        if (overUI)
        {
            Debug.Log($"[LandZone] '{gameObject.name}' Click bị bỏ qua vì click trúng UI.");
            return;
        }

        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit))
        {
            return;
        }

        // Log chẩn đoán xem tia raycast thực sự va chạm vào vật thể nào
        Debug.Log($"[LandZone] Raycast từ chuột trúng: '{hit.collider.gameObject.name}' (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, Tag: {hit.collider.gameObject.tag})");

        // Kiểm tra có phải vùng đất này không
        LandZone hitZone = hit.collider.GetComponentInParent<LandZone>();
        if (hitZone == null)
        {
            return;
        }

        if (hitZone != this)
        {
            // Chỉ log từ zone khớp để tránh spam log từ 27 zones khác
            return;
        }

        Debug.Log($"[LandZone] ✓ Click trúng collider của: {gameObject.name} (isLocked={isLocked}, _ignorePanelOpen={_ignorePanelOpen})");

        if (!isLocked)
        {
            Debug.Log($"[LandZone] '{gameObject.name}' bỏ qua mở panel vì đã unlock rồi.");
            return;
        }

        // Đang trong cooldown — bỏ qua
        if (_ignorePanelOpen)
        {
            Debug.Log($"[LandZone] '{gameObject.name}' đang trong thời gian chờ (cooldown click).");
            return;
        }

        if (landUnlockPanel == null)
        {
            Debug.LogError($"[LandZone] '{gameObject.name}' landUnlockPanel chưa được gán hoặc tự tìm bằng tag thất bại!");
            return;
        }

        if (_panelManager == null)
            _panelManager = landUnlockPanel.GetComponent<LandUnlockManager>();

        if (_panelManager == null)
        {
            Debug.LogError($"[LandZone] Không tìm thấy LandUnlockManager trên " + landUnlockPanel.name);
            return;
        }

        _panelManager.BindTargetZone(this);
        _panelManager.RefreshPanelData();
        landUnlockPanel.SetActive(true);
        Debug.Log($"[LandZone] Đã bật thành công panel '{landUnlockPanel.name}' cho vùng đất '{gameObject.name}'");
    }

    // ─── Cooldown coroutine ───────────────────────────────────────────────────

    private IEnumerator IgnoreOpenBriefly()
    {
        _ignorePanelOpen = true;
        // Chờ animation đóng (0.2s) + buffer (0.15s)
        yield return new WaitForSecondsRealtime(0.35f);
        _ignorePanelOpen = false;
        _ignoreCoroutine = null;
    }

    /// <summary>
    /// Coroutine tính vị trí ngẫu nhiên quanh BoxCollider và kích nổ pháo hoa
    /// </summary>
    private IEnumerator Co_SpawnCelebrationFireworks()
    {
        for (int i = 0; i < fireworkCount; i++)
        {
            GameObject randomPrefab = fireworkPrefabs[Random.Range(0, fireworkPrefabs.Length)];

            if (randomPrefab != null)
            {
                // Tính tọa độ ngẫu nhiên trên mặt phẳng XZ quanh vị trí của BoxCollider (transform.position)
                Vector2 randomCircle = Random.insideUnitCircle * fireworkSpawnRadius;
                
                // Cao hơn mặt đất 0.5m để không bị chìm hiệu ứng
                Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

                // Tạo pháo hoa và tự hủy sau 4 giây để tránh rác RAM
                GameObject fireworkInstance = Instantiate(randomPrefab, spawnPos, Quaternion.identity);
                Destroy(fireworkInstance, 4f);
            }

            // Chờ một khoảng ngẫu nhiên trước khi bắn phát tiếp theo cho tự nhiên
            yield return new WaitForSeconds(Random.Range(minFireworkDelay, maxFireworkDelay));
        }
    }
}
