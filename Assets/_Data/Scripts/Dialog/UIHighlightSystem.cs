using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHighlightSystem : MonoBehaviour
{
    [Header("HUD Canvas chính")]
    public Canvas hudCanvas;

    [Header("Kéo SPRITE highlight vào đây - không cần tạo HL_Icon")]
    public Sprite defaultHighlightSprite;
    public Sprite lockedHighlightSprite;
    public Sprite warningHighlightSprite;
    public Sprite successHighlightSprite;

    [Header("Màu highlight")]
    public Color defaultColor = new Color(1f, 0.9f, 0.05f, 0.95f);
    public Color lockedColor = new Color(1f, 0.25f, 0.25f, 0.95f);
    public Color warningColor = new Color(1f, 0.1f, 0.1f, 0.95f);
    public Color successColor = new Color(0.25f, 1f, 0.25f, 0.95f);

    [Header("Dim nền - làm chỗ khác chìm xuống")]
    public bool useDim = true;
    public Color dimColor = new Color(0f, 0f, 0f, 0.58f);
    public bool dimRaycastTarget = false;

    [Header("Clone icon được highlight lên trên dim")]
    public bool cloneTargetIconAboveDim = true;
    public Color clonedIconColor = Color.white;

    [Header("UI luôn nổi trên Dim - kéo Panel_Tutorial vào đây nếu cần")]
    public RectTransform[] keepOnTopElements;

    [Header("Kéo icon HUD vào đây")]
    public RectTransform woodIconRT;
    public RectTransform stoneIconRT;
    public RectTransform foodIconRT;
    public RectTransform workerIconRT;
    public RectTransform buildButtonRT;
    public RectTransform dayTimerRT;
    public RectTransform enemyCounterRT;

    [Header("Size / Offset")]
    public bool matchTargetSize = true;
    public bool useNativeSpriteSize = false;
    public Vector2 manualSize = new Vector2(80f, 80f);
    public Vector2 padding = new Vector2(24f, 24f);
    public Vector2 positionOffset = Vector2.zero;

    [Header("Pulse")]
    public bool usePulse = true;
    public float pulseSpeed = 2.5f;
    public float pulseExtra = 14f;

    private RectTransform canvasRT;
    private RectTransform currentTarget;

    private GameObject runtimeDimGO;
    private RectTransform runtimeDimRT;
    private Image runtimeDimImage;

    private GameObject runtimeCloneGO;
    private RectTransform runtimeCloneRT;
    private Image runtimeCloneImage;

    private GameObject runtimeHighlightGO;
    private RectTransform runtimeHighlightRT;
    private Image runtimeHighlightImage;

    private Coroutine pulseRoutine;
    private float pulseAdd;
    private Color activeColor;

    private readonly List<Button> blockedButtons = new List<Button>();

    void Awake()
    {
        if (hudCanvas == null)
            hudCanvas = GetComponentInParent<Canvas>();

        if (hudCanvas == null)
        {
            Debug.LogError("[HIGHLIGHT] Chưa gán Hud Canvas.");
            enabled = false;
            return;
        }

        canvasRT = hudCanvas.GetComponent<RectTransform>();

        CreateRuntimeDim();
        CreateRuntimeCloneIcon();
        CreateRuntimeHighlight();

        ClearAll();
    }

    void LateUpdate()
    {
        if (currentTarget == null)
            return;

        UpdateHighlightPosition();
        BringKeepOnTopElementsToFront();
    }

    // =====================================================
    // TẠO RUNTIME UI
    // =====================================================

    private void CreateRuntimeDim()
    {
        if (runtimeDimGO != null) return;

        runtimeDimGO = new GameObject(
            "Runtime_Highlight_Dim",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        runtimeDimGO.transform.SetParent(hudCanvas.transform, false);

        runtimeDimRT = runtimeDimGO.GetComponent<RectTransform>();
        runtimeDimImage = runtimeDimGO.GetComponent<Image>();

        runtimeDimRT.anchorMin = Vector2.zero;
        runtimeDimRT.anchorMax = Vector2.one;
        runtimeDimRT.offsetMin = Vector2.zero;
        runtimeDimRT.offsetMax = Vector2.zero;

        runtimeDimImage.color = dimColor;
        runtimeDimImage.raycastTarget = dimRaycastTarget;

        runtimeDimGO.SetActive(false);
    }

    private void CreateRuntimeCloneIcon()
    {
        if (runtimeCloneGO != null) return;

        runtimeCloneGO = new GameObject(
            "Runtime_Highlight_TargetClone",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        runtimeCloneGO.transform.SetParent(hudCanvas.transform, false);

        runtimeCloneRT = runtimeCloneGO.GetComponent<RectTransform>();
        runtimeCloneImage = runtimeCloneGO.GetComponent<Image>();

        runtimeCloneRT.anchorMin = new Vector2(0.5f, 0.5f);
        runtimeCloneRT.anchorMax = new Vector2(0.5f, 0.5f);
        runtimeCloneRT.pivot = new Vector2(0.5f, 0.5f);
        runtimeCloneRT.localScale = Vector3.one;

        runtimeCloneImage.color = clonedIconColor;
        runtimeCloneImage.raycastTarget = false;
        runtimeCloneImage.preserveAspect = true;

        runtimeCloneGO.SetActive(false);
    }

    private void CreateRuntimeHighlight()
    {
        if (runtimeHighlightGO != null) return;

        runtimeHighlightGO = new GameObject(
            "Runtime_HighlightIcon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        runtimeHighlightGO.transform.SetParent(hudCanvas.transform, false);

        runtimeHighlightRT = runtimeHighlightGO.GetComponent<RectTransform>();
        runtimeHighlightImage = runtimeHighlightGO.GetComponent<Image>();

        runtimeHighlightRT.anchorMin = new Vector2(0.5f, 0.5f);
        runtimeHighlightRT.anchorMax = new Vector2(0.5f, 0.5f);
        runtimeHighlightRT.pivot = new Vector2(0.5f, 0.5f);
        runtimeHighlightRT.sizeDelta = manualSize;
        runtimeHighlightRT.anchoredPosition = Vector2.zero;
        runtimeHighlightRT.localScale = Vector3.one;

        runtimeHighlightImage.sprite = defaultHighlightSprite;
        runtimeHighlightImage.color = defaultColor;
        runtimeHighlightImage.raycastTarget = false;
        runtimeHighlightImage.preserveAspect = true;

        runtimeHighlightGO.SetActive(false);
    }

    // =====================================================
    // GỌI NHANH THEO ICON HUD
    // =====================================================

    public void HighlightWood()
    {
        HighlightRT(woodIconRT, defaultColor, defaultHighlightSprite);
    }

    public void HighlightStone()
    {
        HighlightRT(stoneIconRT, defaultColor, defaultHighlightSprite);
    }

    public void HighlightFood()
    {
        HighlightRT(foodIconRT, defaultColor, defaultHighlightSprite);
    }

    public void HighlightWorker()
    {
        HighlightRT(workerIconRT, defaultColor, defaultHighlightSprite);
    }

    public void HighlightBuild()
    {
        HighlightRT(buildButtonRT, defaultColor, defaultHighlightSprite);
    }

    public void HighlightDayTimer()
    {
        HighlightRT(dayTimerRT, defaultColor, defaultHighlightSprite);
    }

    public void HighlightEnemy()
    {
        HighlightRT(enemyCounterRT, warningColor, warningHighlightSprite);
    }

    // =====================================================
    // PUBLIC API
    // =====================================================

    public void HighlightUI(GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning("[HIGHLIGHT] Target GameObject null.");
            return;
        }

        HighlightUI(target.GetComponent<RectTransform>());
    }

    public void HighlightUI(RectTransform target)
    {
        HighlightRT(target, defaultColor, defaultHighlightSprite);
    }

    public void HighlightLocked(GameObject target)
    {
        if (target == null) return;

        HighlightRT(
            target.GetComponent<RectTransform>(),
            lockedColor,
            lockedHighlightSprite
        );
    }

    public void HighlightRedWarning(GameObject target)
    {
        if (target == null) return;

        HighlightRT(
            target.GetComponent<RectTransform>(),
            warningColor,
            warningHighlightSprite
        );
    }

    public void HighlightSuccess(GameObject target)
    {
        if (target == null) return;

        HighlightRT(
            target.GetComponent<RectTransform>(),
            successColor,
            successHighlightSprite
        );
    }

    public void HighlightRT(RectTransform target)
    {
        HighlightRT(target, defaultColor, defaultHighlightSprite);
    }

    public void HighlightRT(RectTransform target, Color color, Sprite sprite)
    {
        if (target == null)
        {
            Debug.LogWarning("[HIGHLIGHT] Target RectTransform null.");
            return;
        }

        ClearAll();

        Canvas.ForceUpdateCanvases();

        currentTarget = target;
        activeColor = color;
        pulseAdd = 0f;

        // 1. Dim nền trước
        if (useDim && runtimeDimGO != null)
        {
            runtimeDimImage.color = dimColor;
            runtimeDimImage.raycastTarget = dimRaycastTarget;
            runtimeDimGO.SetActive(true);
            runtimeDimGO.transform.SetAsLastSibling();
        }

        // 2. Clone icon target lên trên dim
        SetupCloneFromTarget(target);

        // 3. Sprite vòng / hiệu ứng highlight trên cùng
        if (runtimeHighlightImage != null)
        {
            if (sprite != null)
                runtimeHighlightImage.sprite = sprite;
            else if (defaultHighlightSprite != null)
                runtimeHighlightImage.sprite = defaultHighlightSprite;

            runtimeHighlightImage.color = color;
            runtimeHighlightImage.raycastTarget = false;
        }

        runtimeHighlightGO.SetActive(true);
        runtimeHighlightGO.transform.SetAsLastSibling();

        UpdateHighlightPosition();
        BringKeepOnTopElementsToFront();

        if (usePulse)
            pulseRoutine = StartCoroutine(Pulse());
    }

    public void Clear()
    {
        ClearAll();
    }

    public void ClearAll()
    {
        currentTarget = null;
        pulseAdd = 0f;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (runtimeDimGO != null)
            runtimeDimGO.SetActive(false);

        if (runtimeCloneGO != null)
            runtimeCloneGO.SetActive(false);

        if (runtimeHighlightGO != null)
            runtimeHighlightGO.SetActive(false);
    }

    // =====================================================
    // CLONE TARGET ICON
    // =====================================================

    private void SetupCloneFromTarget(RectTransform target)
    {
        if (!cloneTargetIconAboveDim)
        {
            runtimeCloneGO.SetActive(false);
            return;
        }

        Image sourceImage = GetBestImage(target);

        if (sourceImage == null || sourceImage.sprite == null)
        {
            runtimeCloneGO.SetActive(false);
            return;
        }

        runtimeCloneImage.sprite = sourceImage.sprite;
        runtimeCloneImage.color = clonedIconColor;
        runtimeCloneImage.preserveAspect = true;
        runtimeCloneImage.raycastTarget = false;

        runtimeCloneGO.SetActive(true);
        runtimeCloneGO.transform.SetAsLastSibling();
    }

    private Image GetBestImage(RectTransform target)
    {
        Image img = target.GetComponent<Image>();

        if (img != null && img.sprite != null)
            return img;

        Image[] children = target.GetComponentsInChildren<Image>(true);

        foreach (Image child in children)
        {
            if (child != null && child.sprite != null)
                return child;
        }

        return img;
    }

    // =====================================================
    // BLOCK BUTTONS
    // =====================================================

    public void BlockAllExcept(params GameObject[] allowed)
    {
        UnblockAll();

        HashSet<GameObject> allowSet = new HashSet<GameObject>(allowed);

        Button[] buttons = FindObjectsOfType<Button>(true);

        foreach (Button btn in buttons)
        {
            if (btn == null) continue;

            if (!allowSet.Contains(btn.gameObject))
            {
                btn.interactable = false;
                blockedButtons.Add(btn);
            }
        }
    }

    public void UnblockAll()
    {
        foreach (Button btn in blockedButtons)
        {
            if (btn != null)
                btn.interactable = true;
        }

        blockedButtons.Clear();
    }

    // =====================================================
    // GET ICON BY NAME
    // =====================================================

    public RectTransform GetIconRT(string name)
    {
        string n = name.ToLower();

        switch (n)
        {
            case "wood":
            case "woodicon":
                return woodIconRT;

            case "stone":
            case "stoneicon":
                return stoneIconRT;

            case "food":
            case "foodicon":
            case "wheat":
                return foodIconRT;

            case "worker":
            case "workericon":
                return workerIconRT;

            case "build":
            case "buildbutton":
                return buildButtonRT;

            case "day":
            case "daytimer":
                return dayTimerRT;

            case "enemy":
            case "enemycounter":
                return enemyCounterRT;

            default:
                Debug.LogWarning("[HIGHLIGHT] Không nhận ra icon: " + name);
                return null;
        }
    }

    // =====================================================
    // POSITION
    // =====================================================

    private void UpdateHighlightPosition()
    {
        if (currentTarget == null)
            return;

        Vector2 pos = GetCanvasPos(currentTarget) + positionOffset;
        Vector2 targetSize = GetCanvasSize(currentTarget);
        Vector2 highlightSize = GetTargetSize(currentTarget) + Vector2.one * pulseAdd;

        if (runtimeCloneGO != null && runtimeCloneGO.activeSelf)
        {
            runtimeCloneRT.anchoredPosition = pos;
            runtimeCloneRT.sizeDelta = targetSize;
        }

        if (runtimeHighlightGO != null && runtimeHighlightGO.activeSelf)
        {
            runtimeHighlightRT.anchoredPosition = pos;
            runtimeHighlightRT.sizeDelta = highlightSize;
        }
    }

    private Vector2 GetTargetSize(RectTransform target)
    {
        if (matchTargetSize)
            return GetCanvasSize(target) + padding;

        if (useNativeSpriteSize && runtimeHighlightImage != null && runtimeHighlightImage.sprite != null)
            return runtimeHighlightImage.sprite.rect.size;

        return manualSize;
    }

    private Vector2 GetCanvasPos(RectTransform source)
    {
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);

        Vector3 center = (corners[0] + corners[2]) * 0.5f;

        Camera cam = GetCanvasCamera();

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, center);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            screenPoint,
            cam,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private Vector2 GetCanvasSize(RectTransform source)
    {
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);

        Camera cam = GetCanvasCamera();

        Vector2 blScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 trScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            blScreen,
            cam,
            out Vector2 bl
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRT,
            trScreen,
            cam,
            out Vector2 tr
        );

        return new Vector2(
            Mathf.Abs(tr.x - bl.x),
            Mathf.Abs(tr.y - bl.y)
        );
    }

    private Camera GetCanvasCamera()
    {
        if (hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (hudCanvas.worldCamera != null)
            return hudCanvas.worldCamera;

        return Camera.main;
    }

    // =====================================================
    // PULSE
    // =====================================================

    private IEnumerator Pulse()
    {
        while (runtimeHighlightGO != null && runtimeHighlightGO.activeSelf)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);

            pulseAdd = Mathf.Lerp(0f, pulseExtra, t);

            if (runtimeHighlightImage != null)
            {
                Color c = activeColor;
                c.a = Mathf.Lerp(activeColor.a * 0.45f, activeColor.a, t);
                runtimeHighlightImage.color = c;
            }

            yield return null;
        }
    }

    private void BringKeepOnTopElementsToFront()
    {
        if (keepOnTopElements == null) return;

        foreach (RectTransform rt in keepOnTopElements)
        {
            if (rt != null && rt.gameObject.activeInHierarchy)
                rt.SetAsLastSibling();
        }
    }
}