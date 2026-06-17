using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestTabUI : MonoBehaviour
{
    [Header("Tabs")]
    public Button[] tabButtons;
    public Image[] tabImages;

    [Header("Colors")]
    public Color normalColor = new Color32(90, 59, 36, 255);
    public Color selectedColor = new Color32(200, 138, 61, 255);

    [Header("Content")]
    public TMP_Text contentTitleText;
    public TMP_Text contentBodyText;

    private string[] titles =
    {
        "GIAI ĐOẠN 1: THIẾT LẬP MÔI TRƯỜNG",
        "GIAI ĐOẠN 2: TỐI ƯU VẬN CHUYỂN",
        "GIAI ĐOẠN 3: BẢO VỆ DÂN LÀNG"
    };

    private string[] bodies =
    {
        "1. Mở rộng Sức chứa Nhân lực\n- Xây nhà dân\n- Mục tiêu: Có 4 Worker trên bản đồ\n\n" +
        "2. Kích hoạt chuỗi Lương thực\n- Đặt 1 Cánh đồng lúa\n- Mục tiêu: Thu về 50 Lúa\n\n" +
        "3. Tối ưu vận chuyển\n- Xây Kho chứa phụ gần mỏ\n- Mục tiêu: Tăng tốc độ thu thập +100 Gỗ",

        "1. Đạt mốc Tích trữ\n- Xây hoặc nâng cấp kho chứa lớn\n\n" +
        "Mục tiêu:\n- 500 Gỗ\n- 500 Đá\n- 500 Lúa\n\n" +
        "Kiểm tra hệ thống Worker tự động hoạt động ổn định.",

        "1. Thiết lập Vành đai bảo hộ\n- Xây Tháp Canh, Tháp Pháo, Tháp Cung\n- Bảo vệ 2 khu khai thác\n\n" +
        "2. Tự động hóa Tiêu diệt\n- Tháp tự bắn Enemy\n- Mục tiêu: Diệt 10 Enemy\n\n" +
        "3. Vận hành không tổn thất\n- Sống sót qua đợt tấn công\n- Worker chết = 0"
    };

    private void Start()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            tabButtons[i].onClick.AddListener(() => ShowTab(index));
        }

        ShowTab(0);
    }

    public void ShowTab(int index)
    {
        contentTitleText.text = titles[index];
        contentBodyText.text = bodies[index];

        for (int i = 0; i < tabImages.Length; i++)
        {
            tabImages[i].color = i == index ? selectedColor : normalColor;
        }
    }
}