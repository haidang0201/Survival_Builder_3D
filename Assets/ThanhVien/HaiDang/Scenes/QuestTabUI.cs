using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestTabUI : MonoBehaviour
{
    [Header("Tabs")]
    public Button[] tabButtons;
    public Image[] tabImages;

    [Header("Colors - VIỆT CỔ STYLE (FIXED CONTRAST)")]
    public Color normalColor = new Color32(55, 40, 25, 255);      // tối hơn, dễ đọc
    public Color selectedColor = new Color32(245, 200, 110, 255); // vàng sáng nổi bật

    [Header("Content")]
    public TMP_Text contentTitleText;
    public TMP_Text contentBodyText;

    // 💥 RESOURCE ICON POSITIONS (FIELD BẠN MUỐN)
    [Header("RESOURCE ICON POSITIONS (UI IMAGE)")]
    public Image woodIconUI;
    public Image stoneIconUI;
    public Image foodIconUI;

    [Header("RESOURCE SPRITES")]
    public Sprite woodSprite;
    public Sprite stoneSprite;
    public Sprite foodSprite;

    private string[] titles =
    {
        "GIAI ĐOẠN 1: THIẾT LẬP MÔI TRƯỜNG",
        "GIAI ĐOẠN 2: TỐI ƯU VẬN CHUYỂN",
        "GIAI ĐOẠN 3: BẢO VỆ DÂN LÀNG"
    };

    private string[] bodies =
    {
        "1. Mở rộng Sức chứa Nhân lực\n- Xây nhà dân\n- Mục tiêu: Có 4 Worker\n\n" +
        "2. Thu thập tài nguyên\n- 200 \n- 100 \n- 70 \n\n" +
        "3. Tối ưu vận chuyển\n- Xây kho gần mỏ",

        "1. Đạt mốc tích trữ\n- Nâng cấp kho\n\n" +
        "Mục tiêu:\n- 500 \n- 500 \n- 500 ",

        "1. Thiết lập phòng thủ\n- Xây Tháp Canh\n- Xây Tháp Pháo\n\n" +
        "2. Tiêu diệt kẻ địch\n- Diệt 10 Enemy"
    };

    void Start()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            tabButtons[i].onClick.AddListener(() => ShowTab(index));
        }

        ShowTab(0);

        ApplyResourceIcons();
    }

    public void ShowTab(int index)
    {
        contentTitleText.text = titles[index];

        // 💥 TEXT NỔI HƠN
        contentBodyText.color = new Color32(90, 60, 30, 255);
        contentBodyText.text = bodies[index];

        for (int i = 0; i < tabImages.Length; i++)
        {
            tabImages[i].color = (i == index) ? selectedColor : normalColor;
        }
    }

    // 💥 SET ICON INTO UI POSITIONS
    void ApplyResourceIcons()
    {
        if (woodIconUI != null && woodSprite != null)
            woodIconUI.sprite = woodSprite;

        if (stoneIconUI != null && stoneSprite != null)
            stoneIconUI.sprite = stoneSprite;

        if (foodIconUI != null && foodSprite != null)
            foodIconUI.sprite = foodSprite;
    }
}