using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class StoneMineUnlockPanelController : MonoBehaviour
{
    [Header("Panel References")]
    public ResourceCardUI cardUI;
    public Button confirmButton;
    public Button backgroundButton; // Overlay/background để đóng panel khi bấm ngoài

    [Header("Logic Config")]
    public BuildingType targetBuildingType = BuildingType.StoneMine;
    public int requiredWorkersToUnlock = 4;
    public int productionRatePerMinute = 6;

    private ResourceNode targetNode;

    private void Awake()
    {
        if (cardUI == null)
        {
            cardUI = GetComponent<ResourceCardUI>();
        }

        if (confirmButton == null && cardUI != null)
        {
            confirmButton = cardUI.confirmButton;
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnClickUnlock);
        }

        if (backgroundButton != null)
        {
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.onClick.AddListener(ClosePanel);
        }
    }

    private void OnEnable()
    {
        RefreshPanelData();
    }

    public void BindTargetNode(ResourceNode node)
    {
        targetNode = node;
    }

    public void RefreshPanelData()
    {
        int currentWorkers;
        int maxWorkers;
        ReadWorkersFromSave(out currentWorkers, out maxWorkers);

        bool canUnlock = currentWorkers >= requiredWorkersToUnlock;

        if (cardUI != null)
        {
            cardUI.SetUIData(currentWorkers, requiredWorkersToUnlock, productionRatePerMinute, currentWorkers, maxWorkers, canUnlock);
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = canUnlock;
        }
    }

    private void ReadWorkersFromSave(out int currentWorkers, out int maxWorkers)
    {
        currentWorkers = 0;
        maxWorkers = requiredWorkersToUnlock;

        if (JsonDataManager.Ins == null)
        {
            return;
        }

        string savePath = Path.Combine(Application.persistentDataPath, JsonDataManager.Ins.saveFileName);
        if (!File.Exists(savePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            var save = JsonUtility.FromJson<JsonDataManager.GameSaveData>(json);
            if (save == null || save.buildings == null)
            {
                return;
            }

            for (int i = save.buildings.Count - 1; i >= 0; i--)
            {
                var state = save.buildings[i];
                if (state == null || state.buildingType != targetBuildingType)
                {
                    continue;
                }

                currentWorkers = Mathf.Max(0, state.currentWorkers);
                maxWorkers = state.maxWorkers > 0 ? state.maxWorkers : requiredWorkersToUnlock;
                return;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[StoneMineUnlockPanelController] Không đọc được save JSON: " + ex.Message);
        }
    }

    private void OnClickUnlock()
    {
        if (confirmButton != null && !confirmButton.interactable)
        {
            Debug.Log("Chưa đủ worker để mở khóa mỏ đá.");
            return;
        }

        if (targetNode != null)
        {
            targetNode.UnlockNode();
        }

        ClosePanel();
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}
