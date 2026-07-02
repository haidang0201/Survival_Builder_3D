using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class RoKQuestItemUI : MonoBehaviour
{
    [Header("UI")]
    public Button goButton;
    public TMP_Text goButtonText;

    private string questId;

    // 👉 SEND EVENT RA PANEL
    public Action<string> onGoClicked;

    public void Bind(RoKQuestPanelUI.Quest quest, RoKQuestPanelUI owner)
    {
        questId = quest.id;

        if (goButton != null)
        {
            goButton.onClick.RemoveAllListeners();
            goButton.onClick.AddListener(() =>
            {
                Debug.Log("[QuestItem] GO CLICKED: " + questId);
                onGoClicked?.Invoke(questId);
            });
        }
    }
}