using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ResourceCardUI : MonoBehaviour
{
    [Header("Stats Elements")]
    // ─ Giá trị (số hiển thị bên phải)
    public TextMeshProUGUI workerProgressText;   // Dòng 1 giá trị
    public TextMeshProUGUI productionText;        // Dòng 2 giá trị
    public TextMeshProUGUI workerCountText;       // Dòng 3 giá trị

    [Header("Labels (tên dòng bên trái)")]
    [Tooltip("Kéo Text 'Tiến độ worker' vào đây")]
    public TextMeshProUGUI workerProgressLabel;   // Label dòng 1
    [Tooltip("Kéo Text 'Worker' (dòng 3) vào đây")]
    public TextMeshProUGUI workerCountLabel;      // Label dòng 3

    [Header("Rows to hide when showing resource unlock")]
    [Tooltip("Kéo GameObject row 'Worker' (dòng 3) vào đây để tự ẩn khi mở khóa bằng tài nguyên")]
    public List<GameObject> workerOnlyRows;

    [Header("Action Button")]
    public Button confirmButton;

    public void SetUIData(int currentProgress, int maxProgress, int productionRate, int currentWorkers, int maxWorkers, bool isConditionMet = true)
    {
        if(workerProgressText != null)
        {
            workerProgressText.text = $"{currentProgress}/{maxProgress}";
            workerProgressText.color = isConditionMet ? Color.white : Color.red;
        }
        if(productionText != null) productionText.text = $"<b>+{productionRate} đá/phút</b>";
        if(workerCountText != null) workerCountText.text = $"{currentWorkers}/{maxWorkers}";
    }

    /// <summary>
    /// Hiển thị yêu cầu mở khóa bằng TÀI NGUYÊN (gỗ).
    /// Dùng bởi StoneMineUnlockManager.
    /// </summary>
    /// <param name="currentWood">Gỗ hiện tại của player</param>
    /// <param name="requiredWood">Gỗ cần để mở khóa</param>
    /// <param name="productionRate">Sản lượng đá/phút</param>
    /// <param name="canUnlock">Đủ điều kiện chưa</param>
    public void SetResourceUnlockData(int currentWood, int requiredWood, int productionRate, bool canUnlock,
                                       int currentWorkers = 0, int maxWorkers = 0, bool enoughWorkers = true)
    {
        // Dòng 1 label: "Tiến độ worker" → "Gỗ cần thiết"
        if (workerProgressLabel != null)
            workerProgressLabel.text = "Gỗ cần thiết";

        // Dòng 1 giá trị: gỗ hiện tại / gỗ cần (đỏ nếu chưa đủ)
        bool enoughWood = currentWood >= requiredWood;
        if (workerProgressText != null)
        {
            workerProgressText.text = $"{currentWood}/{requiredWood}";
            workerProgressText.color = enoughWood
                ? Color.white
                : new Color(1f, 0.35f, 0.35f);
        }

        // Dòng 2: sản lượng — ẩn nếu productionRate = -1 (ví dụ: LandUnlockManager)
        if (productionText != null)
        {
            if (productionRate < 0)
                productionText.transform.parent.gameObject.SetActive(false);
            else
            {
                productionText.transform.parent.gameObject.SetActive(true);
                productionText.text = $"<b>+{productionRate} đá/phút</b>";
            }
        }

        // Dòng 3: worker hiện tại / tối đa (đỏ nếu chưa đủ worker)
        if (workerCountText != null)
        {
            workerCountText.transform.parent.gameObject.SetActive(true);
            workerCountText.text = $"{currentWorkers}/{maxWorkers}";
            workerCountText.color = enoughWorkers
                ? Color.white
                : new Color(1f, 0.35f, 0.35f);
        }

        if (workerCountLabel != null)
        {
            workerCountLabel.transform.parent.gameObject.SetActive(true);
            workerCountLabel.text = "Worker";
        }

        // Ẩn các row khác được cấu hình thêm (nếu có)
        if (workerOnlyRows != null)
            foreach (var row in workerOnlyRows)
                if (row != null) row.SetActive(false);
    }
}