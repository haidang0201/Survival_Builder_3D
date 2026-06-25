using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ResourceCardUI : MonoBehaviour
{
    [Header("Stats Elements")]
    public TextMeshProUGUI workerProgressText;
    public TextMeshProUGUI productionText;
    public TextMeshProUGUI workerCountText;

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
}