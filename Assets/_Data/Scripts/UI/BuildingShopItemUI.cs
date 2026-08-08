using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * BuildingShopItemUI.cs
 * Folder: Scripts/UI/
 * Dự án: KHẨN HOANG (PENTA DEV)
 * Phong cách: Demacia Rising Building Item List Card (Cột Bên Trái)
 */

public class BuildingShopItemUI : MonoBehaviour
{
    [Header("=== CẤU HÌNH CÔNG TRÌNH ===")]
    public BuildingType buildingType;
    public string buildingName = "Xưởng Gỗ";
    public string benefitText = "15 Gỗ mỗi lượt";
    [TextArea(2, 4)]
    public string buildingDescription = "Công trình khai thác tài nguyên gỗ phục vụ xây dựng...";
    public Sprite artworkSprite;
    public int buildDuration = 1;

    [Header("=== THÀNH PHẦN UI CỘT TRÁI ===")]
    [SerializeField] private TextMeshProUGUI nameTMP;
    [SerializeField] private GameObject selectionHighlightObj; // Nền kem phát sáng khi được chọn

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if (btn == null) btn = GetComponentInChildren<Button>();

        if (btn != null)
        {
            btn.onClick.AddListener(OnClickItem);
        }
    }

    private void Start()
    {
        RefreshItemName();
    }

    public void RefreshItemName()
    {
        if (nameTMP != null && !string.IsNullOrEmpty(buildingName))
        {
            nameTMP.text = buildingName;
        }
    }

    /// <summary>
    /// Đổi trạng thái hiển thị Nền Kem được chọn
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectionHighlightObj != null)
        {
            selectionHighlightObj.SetActive(isSelected);
        }
    }

    /// <summary>
    /// Khi nhấp vào mục công trình bên cột trái
    /// </summary>
    private void OnClickItem()
    {
        if (BuildingShopUI.Ins != null)
        {
            BuildingShopUI.Ins.SelectBuildingItem(this);
        }
    }
}
