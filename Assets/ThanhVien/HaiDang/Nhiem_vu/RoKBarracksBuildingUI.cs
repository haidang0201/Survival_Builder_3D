using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn script này TRỰC TIẾP vào GameObject "Nhà Quân Đội" (Barracks) trong scene.
/// GameObject đó cần có Collider (2D hoặc 3D) để nhận click (OnMouseDown).
///
/// Khi người chơi bấm vào building:
///   -> hiện 1 nút nổi "Huấn Luyện" ngay phía trên building (world space theo camera).
/// Khi bấm nút đó:
///   -> mở RoKArcherTrainingUI (panel huấn luyện cung thủ) và ẩn nút nổi đi.
/// Bấm ra ngoài building (chỗ trống) sẽ tự ẩn nút nổi.
///
/// CÁCH GẮN (kéo vào field trống trong Inspector):
/// 1. worldCamera        -> Camera chính đang render scene (thường là Main Camera).
/// 2. targetCanvas       -> Canvas chính (Screen Space - Overlay hoặc Camera).
/// 3. archerTrainingUI   -> GameObject đang gắn RoKArcherTrainingUI.
/// 4. vietnameseFont     -> font TMP tiếng Việt bạn đang dùng (tuỳ chọn).
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoKBarracksBuildingUI : MonoBehaviour
{
    [Header("REFERENCES - KÉO VÀO ĐÂY")]
    public Camera worldCamera;
    public Canvas targetCanvas;
    public RoKArcherTrainingUI archerTrainingUI;
    public TMP_FontAsset vietnameseFont;

    [Header("NÚT NỔI PHÍA TRÊN BUILDING")]
    [Tooltip("Vị trí nút nổi tính từ tâm building, theo trục world (X, Y, Z).")]
    public Vector3 worldOffset = new Vector3(0f, 2.5f, 0f);
    public Vector2 buttonSize = new Vector2(180, 60);
    public string buttonLabel = "Huấn Luyện";

    [Header("STYLE")]
    public Color buttonColor = new Color32(199, 106, 27, 255);
    public Color buttonBorderColor = new Color32(224, 166, 74, 255);
    public Color buttonTextColor = new Color32(255, 241, 194, 255);

    [Header("OPTIONS")]
    [Tooltip("Nếu bật: bấm building lần nữa khi nút đang hiện sẽ ẩn nút đi (toggle). Nếu tắt: bấm building luôn hiện nút.")]
    public bool toggleOnClick = true;

    GameObject floatingButtonGO;
    RectTransform floatingButtonRT;
    Button floatingButton;

    bool isButtonVisible = false;

    void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();

        BuildFloatingButton();
        HideFloatingButton();
    }

    void Update()
    {
        if (isButtonVisible)
            UpdateFloatingButtonPosition();
    }

    // =====================================================
    // CLICK VÀO BUILDING
    // =====================================================

    void OnMouseDown()
    {
        if (isButtonVisible && toggleOnClick)
        {
            HideFloatingButton();
            return;
        }

        ShowFloatingButton();
    }

    // Hỗ trợ thêm cho building dùng UI Button (thay vì Collider world) —
    // gọi hàm này từ OnClick() của Button nếu building của bạn là 1 UI Image/Button.
    public void OnBuildingUIClicked()
    {
        if (isButtonVisible && toggleOnClick)
        {
            HideFloatingButton();
            return;
        }

        ShowFloatingButton();
    }

    // =====================================================
    // NÚT NỔI "HUẤN LUYỆN"
    // =====================================================

    void BuildFloatingButton()
    {
        if (targetCanvas == null)
        {
            Debug.LogError("[RoKBarracksBuildingUI] Chưa có Target Canvas.");
            return;
        }

        floatingButtonGO = new GameObject("BarracksTrainButton_" + gameObject.name,
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        floatingButtonGO.transform.SetParent(targetCanvas.transform, false);

        floatingButtonRT = floatingButtonGO.GetComponent<RectTransform>();
        floatingButtonRT.anchorMin = floatingButtonRT.anchorMax = new Vector2(0.5f, 0.5f);
        floatingButtonRT.pivot = new Vector2(0.5f, 0.5f);
        floatingButtonRT.sizeDelta = buttonSize;

        Image bg = floatingButtonGO.GetComponent<Image>();
        bg.color = buttonColor;

        Outline outline = floatingButtonGO.GetComponent<Outline>();
        outline.effectColor = buttonBorderColor;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = false;

        floatingButton = floatingButtonGO.GetComponent<Button>();
        floatingButton.onClick.AddListener(OnTrainButtonClicked);

        GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(floatingButtonGO.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TMP_Text text = textGO.GetComponent<TMP_Text>();
        text.text = buttonLabel;
        text.fontSize = 26;
        text.color = buttonTextColor;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;

        if (vietnameseFont != null)
            text.font = vietnameseFont;
    }

    void ShowFloatingButton()
    {
        if (floatingButtonGO == null)
            return;

        isButtonVisible = true;
        floatingButtonGO.SetActive(true);
        floatingButtonGO.transform.SetAsLastSibling();
        UpdateFloatingButtonPosition();
    }

    void HideFloatingButton()
    {
        if (floatingButtonGO == null)
            return;

        isButtonVisible = false;
        floatingButtonGO.SetActive(false);
    }

    void UpdateFloatingButtonPosition()
    {
        if (worldCamera == null || floatingButtonRT == null)
            return;

        Vector3 worldPos = transform.position + worldOffset;
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        // Nếu building ở phía sau camera thì ẩn nút đi tránh hiện sai vị trí
        if (screenPos.z < 0)
        {
            floatingButtonGO.SetActive(false);
            return;
        }

        floatingButtonGO.SetActive(true);
        floatingButtonRT.position = screenPos;
    }

    // =====================================================
    // BẤM NÚT "HUẤN LUYỆN"
    // =====================================================

    void OnTrainButtonClicked()
    {
        if (archerTrainingUI == null)
        {
            Debug.LogWarning("[RoKBarracksBuildingUI] Chưa gán archerTrainingUI (RoKArcherTrainingUI).");
            return;
        }

        archerTrainingUI.OpenPanel();
        HideFloatingButton();
    }
}