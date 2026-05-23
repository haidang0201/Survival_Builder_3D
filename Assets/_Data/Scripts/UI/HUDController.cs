using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class HUDController : MonoBehaviour
{
    [Header("Top UI (Resources)")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI stoneText;

    [Header("Bottom UI (New Toolbar)")]
    public Button buildButton;
    public Button toolsButton;
    public Button settingButton;
    public GameObject controlHintsGroup; // Bảng chứa text: Chuột trái đặt, chuột phải hủy...

    [Header("External UI References")]
    public GameObject settingUI;         // Kéo thả Setting_UI có sẵn của bạn vào đây

    [Header("Floating Text")]
    public GameObject floatingTextPrefab;
    public Transform floatingTextParent;

    private int currentGold;
    private int currentWood;
    private int currentStone;

    private void Start()
    {
        UpdateGold(0);
        UpdateWood(0);
        UpdateStone(0);

        // Mặc định vào game: Ẩn bảng hướng dẫn phím tắt đi
        if (controlHintsGroup != null) controlHintsGroup.SetActive(false);

        // Đăng ký sự kiện Click cho 3 nút bấm dưới Toolbar
        if (buildButton != null) buildButton.onClick.AddListener(OnBuildButtonClicked);
        if (toolsButton != null) toolsButton.onClick.AddListener(OnToolsButtonClicked);
        if (settingButton != null) settingButton.onClick.AddListener(OnSettingButtonClicked);
    }

    private void Update()
    {
        // Nhận diện nếu đang bật bảng hướng dẫn (Chế độ xây dựng/Bộ công cụ) 
        // mà người chơi click CHUỘT PHẢI thì sẽ hủy chế độ đó và ẩn bảng hướng dẫn đi
        if (controlHintsGroup != null && controlHintsGroup.activeSelf)
        {
            if (Input.GetMouseButtonDown(1)) // 1 là Chuột phải
            {
                ExitActionModes();
                Debug.Log("Đã hủy chế độ hiện tại bằng Chuột Phải.");
            }
        }
    }

    // ================= BOTTOM TOOLBAR LOGIC =================

    private void OnBuildButtonClicked()
    {
        // Khi bấm nút Xây dựng -> Hiện bảng hướng dẫn phím tắt đặt/xoay nhà
        if (controlHintsGroup != null)
        {
            controlHintsGroup.SetActive(!controlHintsGroup.activeSelf);
        }

        // Nếu có script BuildSystem riêng, bạn có thể gọi kích hoạt Ghost Building tại đây
        Debug.Log("Đã bấm nút Xây Dựng!");
    }

    private void OnToolsButtonClicked()
    {
        // Khi bấm Bộ công cụ (Ví dụ công cụ hủy/ủi nhà) -> Cũng hiện hướng dẫn thao tác chuột
        if (controlHintsGroup != null)
        {
            controlHintsGroup.SetActive(!controlHintsGroup.activeSelf);
        }
        Debug.Log("Đã bấm nút Bộ Công Cụ!");
    }

    private void OnSettingButtonClicked()
    {
        // Trước khi mở cài đặt, tắt chế độ xây dựng/bảng hướng dẫn đi cho gọn
        ExitActionModes();

        // Bật/Tắt bảng Setting_UI có sẵn của bạn
        if (settingUI != null)
        {
            settingUI.SetActive(!settingUI.activeSelf);
        }
    }

    // Hàm bổ trợ dùng để tắt nhanh chế độ thao tác và ẩn bảng hướng dẫn
    public void ExitActionModes()
    {
        if (controlHintsGroup != null) controlHintsGroup.SetActive(false);
    }


    // ================= GOLD =================
    public void UpdateGold(int value)
    {
        int oldValue = currentGold;
        currentGold = value;

        int delta = value - oldValue;

        AnimateNumber(goldText, oldValue, value);

        if (delta != 0)
        {
            ShowFloatingText(delta, goldText.transform.position, Color.yellow);

            if (delta > 0)
            {
                goldText.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }
            else
            {
                goldText.transform.DOShakeScale(0.3f, 0.5f);
                goldText.DOColor(Color.red, 0.2f)
                    .OnComplete(() => goldText.DOColor(Color.white, 0.2f));
            }
        }
    }

    // ================= WOOD =================
    public void UpdateWood(int value)
    {
        int oldValue = currentWood;
        currentWood = value;

        int delta = value - oldValue;

        AnimateNumber(woodText, oldValue, value);

        if (delta != 0)
        {
            ShowFloatingText(delta, woodText.transform.position, Color.green);

            if (delta > 0)
            {
                woodText.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }
            else
            {
                woodText.transform.DOShakeScale(0.3f, 0.5f);
                woodText.DOColor(Color.red, 0.2f)
                    .OnComplete(() => woodText.DOColor(Color.white, 0.2f));
            }
        }
    }

    // ================= STONE =================
    public void UpdateStone(int value)
    {
        int oldValue = currentStone;
        currentStone = value;

        int delta = value - oldValue;

        AnimateNumber(stoneText, oldValue, value);

        if (delta != 0)
        {
            ShowFloatingText(delta, stoneText.transform.position, Color.gray);

            if (delta > 0)
            {
                stoneText.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }
            else
            {
                stoneText.transform.DOShakeScale(0.3f, 0.5f);
                stoneText.DOColor(Color.red, 0.2f)
                    .OnComplete(() => stoneText.DOColor(Color.white, 0.2f));
            }
        }
    }

    // ================= SUPPORT =================

    void AnimateNumber(TextMeshProUGUI text, int from, int to)
    {
        DOTween.To(() => from, x =>
        {
            text.text = x.ToString();
        }, to, 0.3f);
    }

    void ShowFloatingText(int amount, Vector3 worldPos, Color color)
    {
        if (floatingTextPrefab == null || floatingTextParent == null) return;

        GameObject obj = Instantiate(floatingTextPrefab, floatingTextParent);

        obj.transform.position = worldPos;

        // Note: Đảm bảo bạn đã có script FloatingText đính kèm trên prefab này
        var ft = obj.GetComponent<FloatingText>();

        string prefix = amount > 0 ? "+" : "";
        ft.Setup(prefix + amount.ToString(), color);
    }
}