using UnityEngine;
using UnityEngine.UI;

public class UIHighlightSystem : MonoBehaviour
{
    public static UIHighlightSystem Instance;

    [Header("CANVASES")]
    public Canvas hudCanvas;             // Canvas chứa hệ thống Tutorial (Sort Order cao)
    public Canvas mainBuildCanvas;       // Kéo "BuildCanvas" từ Hierarchy vào đây!

    [Header("SPRITES")]
    public Sprite circleSprite;

    [Header("HIGHLIGHT SIZE")]
    public float circleSize = 140f;

    [Header("PULSE / MOVEMENT FX")]
    public float scaleStrength = 0.1f;
    public float moveStrength = 6f;
    public float moveSpeed = 3f;

    GameObject dimGO;
    GameObject circleGO;

    RectTransform circleRT;
    RectTransform canvasRT;
    RectTransform currentTarget;
    RectTransform parentScrollViewViewport; // Khung Viewport của ScrollView chứa nút

    Vector3 baseScale;
    Vector2 basePos;

    void Awake()
    {
        Instance = this;
        canvasRT = hudCanvas.transform as RectTransform;
    }

    void Update()
    {
        if (circleRT == null || !circleRT.gameObject.activeInHierarchy) return;

        // 💥 SCALE PULSE
        float scale = 1f + Mathf.Sin(Time.time * 2f) * scaleStrength;
        circleRT.localScale = baseScale * scale;

        // 💥 STRONG FLOAT UP/DOWN
        float yOffset = Mathf.Sin(Time.time * moveSpeed) * moveStrength;
        circleRT.anchoredPosition = basePos + new Vector2(0, yOffset);
    }

    void LateUpdate()
    {
        if (currentTarget == null) return;

        // ================= FIX LỖI SCROLLVIEW ĐA HƯỚNG (NGANG & DỌC) =================
        if (parentScrollViewViewport != null)
        {
            if (!IsTargetInsideViewport(currentTarget, parentScrollViewViewport))
            {
                // Nếu nút trượt ra ngoài rìa (Trái/Phải/Trên/Dưới) -> Ẩn Highlight ngay
                if (circleGO != null && circleGO.activeSelf) circleGO.SetActive(false);
                if (dimGO != null && dimGO.activeSelf) dimGO.SetActive(false);
                return;
            }
            else
            {
                // Nếu nút quay trở lại vùng nhìn thấy -> Hiện Highlight lên
                if (circleGO != null && !circleGO.activeSelf) circleGO.SetActive(true);
                if (dimGO != null && !dimGO.activeSelf) dimGO.SetActive(true);
            }
        }
        // ============================================================================

        Vector2 pos = GetPos(currentTarget);

        if (circleRT != null)
        {
            basePos = pos;
            circleRT.anchoredPosition = pos;
        }
    }

    // Kiểm tra va chạm hình học tuyệt đối 4 chiều giữa Nút và Khung Viewport
    private bool IsTargetInsideViewport(RectTransform target, RectTransform viewport)
    {
        Vector3[] targetCorners = new Vector3[4];
        target.GetWorldCorners(targetCorners); // 0: bottom-left, 1: top-left, 2: top-right, 3: bottom-right

        Vector3[] viewportCorners = new Vector3[4];
        viewport.GetWorldCorners(viewportCorners);

        // Biên vật lý của Viewport trên màn hình
        float viewLeft = viewportCorners[0].x;
        float viewRight = viewportCorners[2].x;
        float viewBottom = viewportCorners[0].y;
        float viewTop = viewportCorners[2].y;

        // Biên vật lý của Nút bấm
        float targetLeft = targetCorners[0].x;
        float targetRight = targetCorners[2].x;
        float targetBottom = targetCorners[0].y;
        float targetTop = targetCorners[1].y;

        // TRƯỜNG HỢP 1: Nút cuộn mất theo chiều ngang (X) - Cho Scroll View Ngang của bạn
        if (targetRight < viewLeft || targetLeft > viewRight)
        {
            return false;
        }

        // TRƯỜNG HỢP 2: Nút cuộn mất theo chiều dọc (Y)
        if (targetTop < viewBottom || targetBottom > viewTop)
        {
            return false;
        }

        return true; // Vẫn nằm trong vùng nhìn thấy
    }

    // ================= MAIN =================

    public void Highlight(RectTransform target)
    {
        ClearAll();
        if (target == null) return;

        currentTarget = target;

        // Nếu quên chưa kéo BuildCanvas trong Inspector, hệ thống tự tìm ngược lên cha
        if (mainBuildCanvas == null)
        {
            mainBuildCanvas = target.GetComponentInParent<Canvas>();
        }

        // Tự động tìm khung Viewport của ScrollRect chứa nút công trình
        ScrollRect scrollRect = target.GetComponentInParent<ScrollRect>();
        if (scrollRect != null && scrollRect.viewport != null)
        {
            parentScrollViewViewport = scrollRect.viewport;
        }
        else
        {
            parentScrollViewViewport = null;
        }

        CreateDim();
        CreateCircle(target);
    }

    // ================= DIM =================

    void CreateDim()
    {
        dimGO = new GameObject("DIM", typeof(RectTransform), typeof(Image));
        dimGO.transform.SetParent(hudCanvas.transform, false);

        var img = dimGO.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.6f);
        img.raycastTarget = false;

        RectTransform rt = dimGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;

        // Tạo Canvas riêng biệt đè lên UI thường
        Canvas c = dimGO.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 998;
        dimGO.AddComponent<GraphicRaycaster>();
    }

    // ================= CIRCLE =================

    void CreateCircle(RectTransform target)
    {
        circleGO = new GameObject("CIRCLE", typeof(RectTransform), typeof(Image));
        circleGO.transform.SetParent(hudCanvas.transform, false);

        circleRT = circleGO.GetComponent<RectTransform>();

        var img = circleGO.GetComponent<Image>();
        img.sprite = circleSprite;
        img.color = Color.white;
        img.raycastTarget = false;

        circleRT.anchorMin = circleRT.anchorMax = new Vector2(0.5f, 0.5f);
        circleRT.sizeDelta = new Vector2(circleSize, circleSize);

        baseScale = circleRT.localScale;

        // Tạo Canvas riêng biệt có Sort Order cao nhất để đè lên tất cả
        Canvas c = circleGO.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 999;
        circleGO.AddComponent<GraphicRaycaster>();
    }

    // ================= POSITION =================

    Vector2 GetPos(RectTransform target)
    {
        Vector3[] c = new Vector3[4];
        target.GetWorldCorners(c);

        Vector3 center = (c[0] + c[2]) * 0.5f;

        Camera targetCamera = (mainBuildCanvas != null) ? mainBuildCanvas.worldCamera : hudCanvas.worldCamera;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(
            targetCamera,
            center
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            screen,
            hudCanvas.worldCamera,
            out Vector2 pos
        );

        return pos;
    }

    // ================= CLEAR =================

    public void ClearAll()
    {
        currentTarget = null;
        parentScrollViewViewport = null;

        if (dimGO) Destroy(dimGO);
        if (circleGO) Destroy(circleGO);
    }
    public void HighlightWorld(Transform target)
    {
        // tạo mũi tên chỉ vào target
        // hoặc dùng UI arrow prefab
        Debug.Log("Arrow pointing to: " + target.name);
    }
}