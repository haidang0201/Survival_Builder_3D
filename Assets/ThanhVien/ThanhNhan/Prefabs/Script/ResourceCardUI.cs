using UnityEngine;
using TMPro;
using UnityEngine.UI; // Nhớ thêm thư viện này để điều khiển Button

public class ResourceCardUI : MonoBehaviour
{
    [Header("Stats Elements")]
    public TextMeshProUGUI workerProgressText;
    public TextMeshProUGUI productionText;
    public TextMeshProUGUI workerCountText;

    [Header("Action Button")]
    public Button confirmButton; // Kéo thả nút Xác Nhận vào đây

    void Start()
    {
        SetUIData(0, 4, 6, 0, 4);

        // Lắng nghe sự kiện click nút
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmUpgrade);
        }
    }

    public void SetUIData(int currentProgress, int maxProgress, int productionRate, int currentWorkers, int maxWorkers)
    {
        if(workerProgressText != null) workerProgressText.text = $"{currentProgress}/{maxProgress}";
        if(productionText != null) productionText.text = $"<b>+{productionRate} đá/phút</b>";
        if(workerCountText != null) workerCountText.text = $"{currentWorkers}/{maxWorkers}";
    }

    // Hàm xử lý khi người chơi ấn nút Xác nhận
    void OnConfirmUpgrade()
    {
        Debug.Log("Đã bấm xác nhận nâng cấp mỏ đá!");
        
        // Tạm thời ẩn cái bảng UI này đi sau khi bấm xong
        gameObject.SetActive(false); 
        
        // Sau này Kai sẽ viết thêm logic trừ tài nguyên, tăng cấp mỏ đá... ở đây nhé
    }
}