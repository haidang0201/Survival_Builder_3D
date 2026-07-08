using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ResourceNode : MonoBehaviour
{
    [Header("Lock System")]
    public GameObject lockCanvas; // Ô để kéo thả Lock_Canvas vào
    public bool isLocked = true;   // Trạng thái khóa ban đầu

    [Header("UI Panel Ref")]
    public GameObject moDaCardPanel; // Kéo thả cái bảng trắng MoDaCard vào đây

    private StoneMineUnlockPanelController panelController;
    private bool _ignorePanelOpen = false;
    private Coroutine _ignoreCoroutine;

    void Start()
    {
        // Ban đầu cập nhật trạng thái ổ khóa lơ lửng trên đầu
        UpdateLockStatus();
        
        // Đảm bảo bảng thông tin MoDaCard luôn ẩn lúc vào game
        if (moDaCardPanel != null)
        {
            moDaCardPanel.SetActive(false);
            panelController = moDaCardPanel.GetComponent<StoneMineUnlockPanelController>();
        }
    }

    public void UpdateLockStatus()
    {
        if (lockCanvas != null)
        {
            // Nếu isLocked = true thì hiện ổ khóa, false thì ẩn ổ khóa
            lockCanvas.SetActive(isLocked); 
        }
    }

    public void UnlockNode()
    {
        isLocked = false;
        UpdateLockStatus();
        Debug.Log("Mỏ đá đã được mở khóa thành công.");
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // --- CHẨN ĐOÁN: bật tạm để debug, xóa sau khi hoạt động ổn ---
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        Debug.Log($"[ResourceNode] Click detected | IsPointerOverUI={overUI}");

        if (overUI) return;

        if (Camera.main == null)
        {
            Debug.LogWarning("[ResourceNode] Không tìm thấy Camera.main!");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit))
        {
            Debug.Log("[ResourceNode] Raycast không trúng gì cả!");
            return;
        }

        Debug.Log($"[ResourceNode] Raycast trúng: '{hit.collider.gameObject.name}' (parent: '{hit.collider.transform.parent?.name}')");

        ResourceNode hitNode = hit.collider.GetComponentInParent<ResourceNode>();
        Debug.Log($"[ResourceNode] hitNode={hitNode?.gameObject.name ?? "null"} | this={gameObject.name} | match={hitNode == this}");
        if (hitNode != this) return;

        Debug.Log("[ResourceNode] ✓ Click trúng mỏ đá: " + gameObject.name);

        if (isLocked)
        {
            if (_ignorePanelOpen) return;

            if (moDaCardPanel == null)
            {
                Debug.LogError("[ResourceNode] moDaCardPanel chưa được gán! Hãy kéo MoDaCard vào Inspector.");
                return;
            }

            if (panelController == null)
                panelController = moDaCardPanel.GetComponent<StoneMineUnlockPanelController>();

            if (panelController == null)
            {
                Debug.LogError("[ResourceNode] Không tìm thấy StoneMineUnlockPanelController trên " + moDaCardPanel.name);
                return;
            }

            panelController.BindTargetNode(this);
            panelController.RefreshPanelData();
            moDaCardPanel.SetActive(true);
            Debug.Log("[ResourceNode] Đã bật panel: " + moDaCardPanel.name);
        }
        else
        {
            Debug.Log("[ResourceNode] Mỏ đá đã mở khóa! Tiến hành khai thác...");
        }
    }

    /// <summary>
    /// Được gọi bởi StoneMineUnlockPanelController khi đóng panel.
    /// Chặn click mở lại trong thời gian animation + buffer để tránh xung đột.
    /// </summary>
    public void NotifyPanelClosed()
    {
        // Hủy coroutine cũ nếu có để tránh chạy cùng lúc
        if (_ignoreCoroutine != null) StopCoroutine(_ignoreCoroutine);
        _ignoreCoroutine = StartCoroutine(IgnoreOpenBriefly());
    }

    private IEnumerator IgnoreOpenBriefly()
    {
        _ignorePanelOpen = true;
        // Chờ đủ thời gian animation đóng (0.2s) + buffer (0.15s)
        yield return new WaitForSecondsRealtime(0.35f);
        _ignorePanelOpen = false;
        _ignoreCoroutine = null;
    }
}