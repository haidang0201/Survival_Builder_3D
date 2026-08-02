using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/*
 * StorageHUDPanel.cs
 * Folder: ThanhVien/NhatTien/Script/UI/StorageHUD/
 * Người làm: TIẾN
 * FIX: Panel xuất hiện ngay cạnh kho được click, tự tránh lề màn hình.
 *
 * CHỨC NĂNG: Tự sinh Canvas + Panel UI hoàn chỉnh bằng code.
 * FIX: Không còn ô đen khi ẩn panel, phông chuẩn, kích thước lớn hơn.
 */
public class StorageHUDPanel : MonoBehaviour
{
    // ── Optional icons ──
    [Header("Icons (không bắt buộc — bỏ trống cũng chạy được)")]
    public Sprite woodIcon;
    public Sprite riceIcon;
    public Sprite stoneIcon;

    // ── Runtime UI refs ──
    private GameObject      _panelRoot;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _typeText;
    private TextMeshProUGUI _amountText;
    private TextMeshProUGUI _percentText;
    private Image           _fillBar;
    private Image           _resourceIcon;
    private Image           _headerImage;

    // ── Data ──
    private WoodStorage  _wood;
    private RiceStorage  _rice;
    private StoneStorage _stone;
    private StorageSlotHUD.StorageType _currentType;
    private bool _isOpen;
    private int  _lastCurrent = -1, _lastMax = -1;

    // ── Màu accent ──
    private static readonly Color WoodColor  = new Color(0.70f, 0.38f, 0.08f);
    private static readonly Color RiceColor  = new Color(0.82f, 0.72f, 0.08f);
    private static readonly Color StoneColor = new Color(0.48f, 0.50f, 0.56f);
    private static readonly Color PanelBg    = new Color(0.10f, 0.08f, 0.06f, 0.95f);
    private static readonly Color FillBgColor= new Color(0.15f, 0.13f, 0.10f, 1.00f);

    // ══════════════════════════════════════════════
    void Awake()
    {
        BuildUI();
        _panelRoot.SetActive(false); // ẩn ngay, KHÔNG giữ lại shadow hay object thừa
        EnsureEventSystem();
    }

    // ══════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════

    public void ShowWood(WoodStorage s, string buildingName, Vector3 worldPos)
    {
        _wood = s; _rice = null; _stone = null;
        _currentType = StorageSlotHUD.StorageType.Wood;
        _lastCurrent = _lastMax = -1;
        OpenPanel(buildingName, "Kho Go", woodIcon, WoodColor, worldPos);
    }

    public void ShowRice(RiceStorage s, string buildingName, Vector3 worldPos)
    {
        _rice = s; _wood = null; _stone = null;
        _currentType = StorageSlotHUD.StorageType.Rice;
        _lastCurrent = _lastMax = -1;
        OpenPanel(buildingName, "Kho Lua", riceIcon, RiceColor, worldPos);
    }

    public void ShowStone(StoneStorage s, string buildingName, Vector3 worldPos)
    {
        _stone = s; _wood = null; _rice = null;
        _currentType = StorageSlotHUD.StorageType.Stone;
        _lastCurrent = _lastMax = -1;
        OpenPanel(buildingName, "Kho Da", stoneIcon, StoneColor, worldPos);
    }

    public void Hide()
    {
        _isOpen = false;
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }

    public void OnCloseButtonClick() => Hide();

    // ══════════════════════════════════════════════
    // UPDATE
    // ══════════════════════════════════════════════

    void Update()
    {
        if (!_isOpen) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                Hide();
        }

        RefreshValues();
    }

    // ══════════════════════════════════════════════
    // INTERNAL LOGIC
    // ══════════════════════════════════════════════

    private void OpenPanel(string buildingName, string typeName, Sprite icon, Color accent, Vector3 worldPos)
    {
        _panelRoot.SetActive(true);
        _isOpen = true;

        if (_titleText   != null) _titleText.text   = buildingName;
        if (_typeText    != null) _typeText.text     = typeName;
        if (_headerImage != null) _headerImage.color = accent;
        if (_fillBar     != null) _fillBar.color     = accent;

        if (_resourceIcon != null)
        {
            _resourceIcon.sprite  = icon;
            _resourceIcon.enabled = icon != null;
        }

        PositionNearBuilding(worldPos);
        RefreshValues();
    }

    /// <summary>
    /// Dùng RectTransformUtility để chuyển world pos → canvas local pos chính xác.
    /// Panel hiện sang phải + lên trên kho, tự clamp vào trong màn hình.
    /// </summary>
    private void PositionNearBuilding(Vector3 worldPos)
    {
        if (Camera.main == null) return;

        Canvas canvas = _panelRoot.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        // World → Screen pixel (chính xác)
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        // Screen → Canvas local point (API chuẩn Unity)
        RectTransform canvasRt = canvas.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt, screenPoint, canvas.worldCamera, out Vector2 localPoint);

        RectTransform panelRt = _panelRoot.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot     = new Vector2(0f, 0f); // góc dưới-trái của panel làm điểm neo

        float pw = panelRt.sizeDelta.x;
        float ph = panelRt.sizeDelta.y;

        // Offset: panel nằm bên phải kho, hơi lên trên
        float ox = 40f;
        float oy = 20f;
        Vector2 desired = localPoint + new Vector2(ox, oy);

        // Canvas half size để clamp
        Vector2 half = canvasRt.sizeDelta * 0.5f;
        float margin = 16f;

        // Nếu bên phải không đủ chỗ → flip sang trái
        if (desired.x + pw > half.x - margin)
            desired.x = localPoint.x - pw - ox;

        // Clamp X, Y trong canvas
        desired.x = Mathf.Clamp(desired.x, -half.x + margin, half.x - pw - margin);
        desired.y = Mathf.Clamp(desired.y, -half.y + margin, half.y - ph - margin);

        panelRt.anchoredPosition = desired;
    }


    private void RefreshValues()
    {
        int current = 0, max = 1;
        switch (_currentType)
        {
            case StorageSlotHUD.StorageType.Wood:
                if (_wood  != null) { current = _wood.CurrentAmount;  max = _wood.MaxCapacity;  } break;
            case StorageSlotHUD.StorageType.Rice:
                if (_rice  != null) { current = _rice.CurrentAmount;  max = _rice.MaxCapacity;  } break;
            case StorageSlotHUD.StorageType.Stone:
                if (_stone != null) { current = _stone.CurrentAmount; max = _stone.MaxCapacity; } break;
        }
        if (current == _lastCurrent && max == _lastMax) return;
        _lastCurrent = current;
        _lastMax     = max;

        float pct = (max > 0) ? (float)current / max : 0f;
        if (_amountText  != null) _amountText.text  = current + " / " + max;
        if (_percentText != null) _percentText.text = Mathf.RoundToInt(pct * 100) + "%";
        if (_fillBar     != null) _fillBar.fillAmount = pct;
    }

    // ══════════════════════════════════════════════
    // UI BUILDER
    // ══════════════════════════════════════════════

    private void BuildUI()
    {
        // ── Canvas ──────────────────────────────────────────
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject cGo = new GameObject("StorageHUD_Canvas");
            DontDestroyOnLoad(cGo);
            canvas            = cGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler cs     = cGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.screenMatchMode   = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;

            cGo.AddComponent<GraphicRaycaster>();
            transform.SetParent(cGo.transform, false);
        }

        // Panel kích thước 400×300, góc dưới-trái
        _panelRoot = MakeImg("StorageInfoPanel", canvas.transform,
            ancMin: Vector2.zero, ancMax: Vector2.zero,
            pivot: Vector2.zero,
            pos: new Vector2(30f, 30f), size: new Vector2(400f, 280f),
            col: PanelBg);

        // Viền mỏng vàng
        Outline outline = _panelRoot.AddComponent<Outline>();
        outline.effectColor    = new Color(1f, 0.82f, 0.3f, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);

        // ── Header (cao 56px) ─────────────────────────────
        GameObject header = MakeImg("Header", _panelRoot.transform,
            ancMin: new Vector2(0,1), ancMax: new Vector2(1,1),
            pivot: new Vector2(0.5f,1),
            pos: Vector2.zero, size: new Vector2(0, 56f),
            col: WoodColor);
        _headerImage = header.GetComponent<Image>();

        // Icon trong header
        _resourceIcon = MakeImgComp("Icon", header.transform,
            ancMin: new Vector2(0,0.5f), ancMax: new Vector2(0,0.5f),
            pivot: new Vector2(0, 0.5f),
            pos: new Vector2(12f, 0f), size: new Vector2(38f, 38f));
        _resourceIcon.preserveAspect = true;
        _resourceIcon.enabled = false;

        // Tên loại kho
        _typeText = MakeTMP("TypeText", header.transform,
            ancMin: new Vector2(0,0.5f), ancMax: new Vector2(0,0.5f),
            pivot: new Vector2(0,0.5f),
            pos: new Vector2(58f, 0f), size: new Vector2(260f, 44f),
            text: "Kho Go", size2: 22f, bold: true, col: Color.white);
        _typeText.alignment = TextAlignmentOptions.MidlineLeft;

        // Nút đóng
        GameObject closeGo = MakeImg("CloseBtn", header.transform,
            ancMin: new Vector2(1,0.5f), ancMax: new Vector2(1,0.5f),
            pivot: new Vector2(1,0.5f),
            pos: new Vector2(-10f, 0f), size: new Vector2(38f, 38f),
            col: new Color(0.75f, 0.12f, 0.08f, 0.95f));

        TextMeshProUGUI xLbl = MakeTMP("X", closeGo.transform,
            ancMin: Vector2.zero, ancMax: Vector2.one,
            pivot: new Vector2(0.5f,0.5f),
            pos: Vector2.zero, size: Vector2.zero,
            text: "X", size2: 18f, bold: true, col: Color.white);
        xLbl.alignment = TextAlignmentOptions.Center;

        Button btn = closeGo.AddComponent<Button>();
        btn.targetGraphic = closeGo.GetComponent<Image>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.75f, 0.12f, 0.08f, 0.95f);
        cb.highlightedColor = new Color(1f, 0.2f, 0.14f, 1f);
        cb.pressedColor     = new Color(0.5f, 0.08f, 0.05f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(OnCloseButtonClick);

        // ── Body ─────────────────────────────────────────
        float y = -76f; // offset từ top của panel

        // Tên kho nhỏ
        _titleText = MakeTMP("TitleText", _panelRoot.transform,
            ancMin: new Vector2(0,1), ancMax: new Vector2(1,1),
            pivot: new Vector2(0.5f,1),
            pos: new Vector2(0f, y), size: new Vector2(0f, 36f),
            text: "Kho Go 1", size2: 15f, bold: false,
            col: new Color(0.95f, 0.88f, 0.65f));
        _titleText.alignment = TextAlignmentOptions.Center;
        y -= 38f;

        // Divider
        MakeImg("Div1", _panelRoot.transform,
            ancMin: new Vector2(0.5f,1), ancMax: new Vector2(0.5f,1),
            pivot: new Vector2(0.5f,1),
            pos: new Vector2(0f, y), size: new Vector2(360f, 1f),
            col: new Color(1f,1f,1f,0.12f));
        y -= 16f;

        // Row: Label + Amount
        MakeTMP("LabelTon", _panelRoot.transform,
            ancMin: new Vector2(0,1), ancMax: new Vector2(0,1),
            pivot: new Vector2(0,1),
            pos: new Vector2(22f, y), size: new Vector2(180f, 32f),
            text: "", size2: 16f, bold: false,
            col: new Color(0.75f,0.75f,0.72f))
            .alignment = TextAlignmentOptions.MidlineLeft;

        _amountText = MakeTMP("AmountText", _panelRoot.transform,
            ancMin: new Vector2(1,1), ancMax: new Vector2(1,1),
            pivot: new Vector2(1,1),
            pos: new Vector2(-22f, y), size: new Vector2(180f, 32f),
            text: "0 / 0", size2: 17f, bold: true, col: Color.white);
        _amountText.alignment = TextAlignmentOptions.MidlineRight;
        y -= 40f;

        // Fill bar bg
        GameObject fillBg = MakeImg("FillBg", _panelRoot.transform,
            ancMin: new Vector2(0.5f,1), ancMax: new Vector2(0.5f,1),
            pivot: new Vector2(0.5f,1),
            pos: new Vector2(0f, y), size: new Vector2(360f, 28f),
            col: FillBgColor);
        Outline fo = fillBg.AddComponent<Outline>();
        fo.effectColor = new Color(1f,1f,1f,0.08f);
        fo.effectDistance = new Vector2(1f,-1f);

        // Fill bar fill
        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillBg.transform, false);
        _fillBar            = fillGo.AddComponent<Image>();
        _fillBar.color      = WoodColor;
        _fillBar.type       = Image.Type.Filled;
        _fillBar.fillMethod = Image.FillMethod.Horizontal;
        _fillBar.fillAmount = 0f;
        RectTransform frt = fillGo.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(3f, 3f);
        frt.offsetMax = new Vector2(-3f,-3f);

        // % text trên fill bar
        _percentText = MakeTMP("PctText", fillBg.transform,
            ancMin: Vector2.zero, ancMax: Vector2.one,
            pivot: new Vector2(0.5f,0.5f),
            pos: Vector2.zero, size: Vector2.zero,
            text: "0%", size2: 13f, bold: true, col: Color.white);
        _percentText.alignment = TextAlignmentOptions.Center;
        y -= 36f;

        // Divider
        MakeImg("Div2", _panelRoot.transform,
            ancMin: new Vector2(0.5f,1), ancMax: new Vector2(0.5f,1),
            pivot: new Vector2(0.5f,1),
            pos: new Vector2(0f, y), size: new Vector2(360f, 1f),
            col: new Color(1f,1f,1f,0.08f));

        // Footer hint (bottom)
        TextMeshProUGUI hint = MakeTMP("Hint", _panelRoot.transform,
            ancMin: new Vector2(0.5f,0), ancMax: new Vector2(0.5f,0),
            pivot: new Vector2(0.5f,0),
            pos: new Vector2(0f, 12f), size: new Vector2(360f, 22f),
            text: "Click bên ngoài đóng tab này", size2: 11f, bold: false,
            col: new Color(0.55f,0.55f,0.52f));
        hint.alignment = TextAlignmentOptions.Center;
        hint.fontStyle = FontStyles.Italic;
    }

    // ══════════════════════════════════════════════
    // BUILDER HELPERS
    // ══════════════════════════════════════════════

    private GameObject MakeImg(string name, Transform parent,
        Vector2 ancMin, Vector2 ancMax, Vector2 pivot,
        Vector2 pos, Vector2 size, Color col)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img   = go.AddComponent<Image>();
        img.color   = col;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot     = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    private Image MakeImgComp(string name, Transform parent,
        Vector2 ancMin, Vector2 ancMax, Vector2 pivot,
        Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot     = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    private TextMeshProUGUI MakeTMP(string name, Transform parent,
        Vector2 ancMin, Vector2 ancMax, Vector2 pivot,
        Vector2 pos, Vector2 size,
        string text, float size2, bool bold, Color col)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size2;
        tmp.color     = col;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        // Dùng font mặc định của TMP (tránh null font)
        if (tmp.font == null)
        {
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot     = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return tmp;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }
}
