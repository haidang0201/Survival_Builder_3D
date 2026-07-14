using TMPro;
using UnityEngine;

public class UIThemeManager : MonoBehaviour
{
    public static UIThemeManager Instance;


    [Header("TEXT COLORS")]

    // Tiêu đề lớn.
    // Trước: (255, 238, 190) quá nhạt -> gần như biến mất trên nền giấy da/trắng ngà.
    // Sau: vàng đồng đậm hơn, vẫn thuộc tông "building" nhưng đủ độ bão hòa để nổi trên nền sáng,
    // đồng thời vẫn rõ trên banner đỏ/nền gỗ tối nhờ outline dày bên dưới.
    public Color title =
        new Color32(214, 158, 46, 255);


    // Label nhỏ (tên trường thông tin).
    // Trước: (120, 88, 55) hơi xám nhạt, khó đọc trên nền trắng/kem.
    // Sau: nâu gỗ đậm, tương phản rõ trên nền sáng mà vẫn hợp tông gỗ.
    public Color label =
        new Color32(66, 42, 22, 255);


    // Giá trị quan trọng (số liệu, tên).
    // Trước: (190, 140, 45) hơi trầm, khó nổi trên nền trắng.
    // Sau: vàng cam đậm, bão hòa cao hơn để bật khỏi nền sáng.
    public Color value =
        new Color32(198, 124, 24, 255);


    // Mô tả.
    // Trước: (105, 78, 52) quá gần màu nền giấy da -> mờ.
    // Sau: nâu sẫm gần như đen-nâu, tương phản mạnh trên nền trắng/kem.
    public Color description =
        new Color32(58, 38, 22, 255);


    // Reward.
    // Trước: (180, 125, 35) hơi nhạt.
    // Sau: cam gỗ cháy, nổi bật hơn trên cả nền sáng lẫn nền tối.
    public Color reward =
        new Color32(190, 92, 18, 255);



    [Header("TEXT EFFECT")]

    // Outline tối và dày hơn để "viền" chữ tách khỏi mọi loại nền (sáng lẫn tối),
    // đúng kiểu chữ khắc gỗ/kim loại của các UI "building" style.
    public Color outline =
        new Color32(35, 20, 10, 235);


    public float outlineWidth = 0.28f;



    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;
        DontDestroyOnLoad(gameObject);
    }



    public void Apply(TMP_Text text, UI_TEXT_TYPE type)
    {
        if (text == null)
            return;


        switch (type)
        {
            case UI_TEXT_TYPE.Title:
                text.color = title;
                break;


            case UI_TEXT_TYPE.Label:
                text.color = label;
                break;


            case UI_TEXT_TYPE.Value:
                text.color = value;
                break;


            case UI_TEXT_TYPE.Description:
                text.color = description;
                break;


            case UI_TEXT_TYPE.Reward:
                text.color = reward;
                break;
        }


        text.outlineColor = outline;
        text.outlineWidth = outlineWidth;
    }
}



public enum UI_TEXT_TYPE
{
    Title,
    Label,
    Value,
    Description,
    Reward
}