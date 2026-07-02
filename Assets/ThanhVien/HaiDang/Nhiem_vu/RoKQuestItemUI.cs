using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoKQuestItemUI : MonoBehaviour
{
    [Header("QUEST CARD UI")]
    public Image questIcon;
    public TMP_Text questTitleText;
    public TMP_Text questProgressText;
    public TMP_Text questDescriptionText;

    [Header("REWARDS")]
    public Image[] rewardIcons;
    public TMP_Text[] rewardAmountTexts;

    [Header("GO BUTTON")]
    public Button goButton;
    public TMP_Text goButtonText;

    [Header("STATE")]
    public GameObject completedMark;
    public GameObject claimedMark;

    private string questId;
    private RoKQuestPanelUI owner;

    public void Bind(RoKQuestPanelUI.Quest quest, RoKQuestPanelUI panelOwner)
    {
        owner = panelOwner;
        questId = quest.id;

        if (questIcon != null)
        {
            questIcon.sprite = quest.icon;
            questIcon.enabled = quest.icon != null;
        }

        if (questTitleText != null)
            questTitleText.text = quest.title;

        if (questProgressText != null)
            questProgressText.text = $"({quest.current}/{quest.target}) {quest.description}";

        if (questDescriptionText != null)
            questDescriptionText.text = quest.shortHint;

        SetupRewards(quest);
        SetupState(quest);

        if (goButton != null)
        {
            goButton.onClick.RemoveAllListeners();
            goButton.onClick.AddListener(() => owner.OnQuestButtonClicked(questId));
        }
    }

    private void SetupRewards(RoKQuestPanelUI.Quest quest)
    {
        for (int i = 0; i < rewardIcons.Length; i++)
        {
            bool hasReward = i < quest.rewards.Count;

            if (rewardIcons[i] != null)
            {
                rewardIcons[i].gameObject.SetActive(hasReward);

                if (hasReward)
                    rewardIcons[i].sprite = quest.rewards[i].icon;
            }

            if (rewardAmountTexts != null && i < rewardAmountTexts.Length && rewardAmountTexts[i] != null)
            {
                rewardAmountTexts[i].gameObject.SetActive(hasReward);

                if (hasReward)
                    rewardAmountTexts[i].text = owner.FormatAmount(quest.rewards[i].amount);
            }
        }
    }

    private void SetupState(RoKQuestPanelUI.Quest quest)
    {
        bool completed = quest.IsCompleted();
        bool claimed = quest.claimed;

        if (completedMark != null)
            completedMark.SetActive(completed && !claimed);

        if (claimedMark != null)
            claimedMark.SetActive(claimed);

        if (goButtonText != null)
        {
            if (claimed)
                goButtonText.text = "Xong";
            else if (completed)
                goButtonText.text = "Nhận";
            else
                goButtonText.text = "Đi";
        }

        if (goButton != null)
            goButton.interactable = !claimed;
    }
}