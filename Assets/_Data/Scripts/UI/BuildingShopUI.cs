using UnityEngine;
using UnityEngine.UI;

public enum BuildingCategory
{
    Civilian,   // Trang 1: Dân sự (Nhà dân, Chợ, Bếp...)
    Military,   // Trang 2: Quân sự (Tháp bắn, Trại lính...)
    Resource    // Trang 3: Tài nguyên (Kho gỗ, Kho đá, Ruộng lúa...)
}

public class BuildingShopUI : MonoBehaviour
{
    [Header("=== 3 TRANG NỘI DUNG ===")]
    [SerializeField] private GameObject civilianPage;  // Panel chứa công trình Dân sự
    [SerializeField] private GameObject militaryPage;  // Panel chứa công trình Quân sự
    [SerializeField] private GameObject resourcePage;  // Panel chứa công trình Tài nguyên

    [Header("=== 3 NÚT CHUYỂN TRANG (TAB BUTTONS) ===")]
    [SerializeField] private Button civilianTabBtn;
    [SerializeField] private Button militaryTabBtn;
    [SerializeField] private Button resourceTabBtn;

    [Header("=== ĐỔI MÀU NÚT KHI ĐƯỢC CHỌN (TÙY CHỌN) ===")]
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public static BuildingShopUI Ins { get; private set; }
    private void Awake()
    {
        if (Ins == null) Ins = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Mặc định khi mở bảng sẽ hiện Trang 1 (Dân sự)
        SelectCategory(BuildingCategory.Civilian);

        // Gán sự kiện click cho các nút Tab
        if (civilianTabBtn != null) 
            civilianTabBtn.onClick.AddListener(() => SelectCategory(BuildingCategory.Civilian));
            
        if (militaryTabBtn != null) 
            militaryTabBtn.onClick.AddListener(() => SelectCategory(BuildingCategory.Military));
            
        if (resourceTabBtn != null) 
            resourceTabBtn.onClick.AddListener(() => SelectCategory(BuildingCategory.Resource));
    }

    /// <summary>
    /// Hàm thực hiện Bật/Tắt trang tương ứng
    /// </summary>
    public void SelectCategory(BuildingCategory category)
    {
        // 1. Bật/Tắt các Panel trang
        if (civilianPage != null) civilianPage.SetActive(category == BuildingCategory.Civilian);
        if (militaryPage != null) militaryPage.SetActive(category == BuildingCategory.Military);
        if (resourcePage != null) resourcePage.SetActive(category == BuildingCategory.Resource);

        // 2. Cập nhật màu sắc nút Tab để người chơi biết đang ở trang nào
        UpdateTabButtonVisual(civilianTabBtn, category == BuildingCategory.Civilian);
        UpdateTabButtonVisual(militaryTabBtn, category == BuildingCategory.Military);
        UpdateTabButtonVisual(resourceTabBtn, category == BuildingCategory.Resource);
    }

    private void UpdateTabButtonVisual(Button btn, bool isActive)
    {
        if (btn == null) return;
        
        Image btnImage = btn.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = isActive ? activeTabColor : inactiveTabColor;
        }
    }
}