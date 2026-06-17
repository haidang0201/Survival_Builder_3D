using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuidePanelUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text guideText;

    public Button tabStart;
    public Button tabResource;
    public Button tabDefense;

    [Header("Tab Colors")]
    public Color normalColor = new Color(0.35f, 0.23f, 0.14f);
    public Color selectedColor = new Color(0.65f, 0.42f, 0.16f);

    void Start()
    {
        tabStart.onClick.AddListener(() => SetTab(0));
        tabResource.onClick.AddListener(() => SetTab(1));
        tabDefense.onClick.AddListener(() => SetTab(2));

        SetTab(0);
    }

    void SetTab(int id)
    {
        // ResetTabs();

        switch (id)
        {
            case 0:
                guideText.text =
@"W / A / S / D : Di chuyển camera
Chuột trái     : Chọn công trình
ESC            : Hủy thao tác";

                //SetSelected(tabStart);
                break;

            case 1:
                guideText.text =
@"R : Thu thập gỗ
T : Thu thập đá
F : Thu thập lương thực
Shift + Click : Gán nhiều lệnh";

                //SetSelected(tabResource);
                break;

            case 2:
                guideText.text =
@"C :  Click chuột trái xây tháp canh
M : Click chuột trái & click building di chuyển nhà
P : Pause game
Không để bị bọn cướp bóc chiếm làng";

                //SetSelected(tabDefense);
                break;
        }
    }

    // void ResetTabs()
    // {
    //     tabStart.image.color = normalColor;
    //     tabResource.image.color = normalColor;
    //     tabDefense.image.color = normalColor;
    // }

    // void SetSelected(Button btn)
    // {
    //     btn.image.color = selectedColor;
    // }
}