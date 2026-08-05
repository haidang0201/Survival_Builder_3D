using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestItemUI : MonoBehaviour
{
    [Header("UI Text & Icon Elements")]
    [SerializeField] private Image questIcon;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Claim Button State")]
    [SerializeField] private Button claimButton;
    [SerializeField] private Image claimButtonImage;
    [SerializeField] private TextMeshProUGUI claimButtonText;

    [Header("Status Text (Hiển thị khi ẩn nút)")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Dynamic Reward System")]
    [SerializeField] private Transform rewardAreaContainer; // Drag GameObject RewardArea vào đây
    [SerializeField] private GameObject rewardItemPrefab;   // Drag RewardItemPrefab (Prefab) vào đây

    [Header("Default Reward Icons")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite expIcon;
    [SerializeField] private Sprite woodIcon;
    [SerializeField] private Sprite stoneIcon;

    [Header("Text Strings Config")]
    [SerializeField] private string claimableTextStr = "NHẬN";
    [SerializeField] private string notClaimableTextStr = "CHƯA XONG";
    [SerializeField] private string claimedTextStr = "ĐÃ NHẬN";

    [Header("Display Mode")]
    [Tooltip("True: Khi chưa đủ điều kiện sẽ ẨN NÚT HÌNH và HIỆN statusText.")]
    [SerializeField] private bool hideButtonWhenNotClaimable = true;

    private Action onClaimClicked;

    private void Awake()
    {
        EnsureReferences();

        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => onClaimClicked?.Invoke());
        }
    }

    private void EnsureReferences()
    {
        if (claimButton != null)
        {
            if (claimButtonImage == null) claimButtonImage = claimButton.GetComponent<Image>();
            if (claimButtonText == null) claimButtonText = claimButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (statusText == null)
        {
            Transform statusObj = transform.Find("StatusText");
            if (statusObj != null) statusText = statusObj.GetComponent<TextMeshProUGUI>();
        }
    }

    public void SetupQuest(Sprite icon, string title, string description, int currentProgress, int maxProgress, List<QuestReward> rewards, bool isCompleted, Action onClaim)
    {
        EnsureReferences();

        if (questIcon != null && icon != null) questIcon.sprite = icon;
        if (questTitleText != null) questTitleText.text = title;
        if (questDescriptionText != null) questDescriptionText.text = description;
        if (progressText != null) progressText.text = string.Format("{0}/{1}", currentProgress, maxProgress);

        onClaimClicked = onClaim;

        // Render động danh sách phần thưởng (cho dù là 1, 2, 3 hay 4 phần thưởng)
        SetupRewards(rewards);

        // Xử lý trạng thái hiển thị Nút / Chữ status
        bool canClaim = currentProgress >= maxProgress;

        if (isCompleted)
        {
            SetState(showButton: !hideButtonWhenNotClaimable, isInteractable: false, textStr: claimedTextStr);
        }
        else if (canClaim)
        {
            SetState(showButton: true, isInteractable: true, textStr: claimableTextStr);
        }
        else
        {
            SetState(showButton: !hideButtonWhenNotClaimable, isInteractable: false, textStr: notClaimableTextStr);
        }
    }

    private void SetupRewards(List<QuestReward> rewards)
    {
        if (rewardAreaContainer == null) return;

        // Xóa sạch các ô phần thưởng tĩnh cũ (Vàng/Exp cũ hardcode trong Prefab) đang có trong RewardArea
        foreach (Transform child in rewardAreaContainer)
        {
            Destroy(child.gameObject);
        }

        if (rewardItemPrefab == null)
        {
            Debug.LogWarning("[QuestItemUI] 'Reward Item Prefab' chưa được kéo vào Inspector! Hãy kéo RewardItemPrefab vào ô này.");
            return;
        }

        if (rewards == null || rewards.Count == 0) return;

        // Tạo mới đúng số lượng phần thưởng được cấu hình
        foreach (var reward in rewards)
        {
            GameObject itemObj = Instantiate(rewardItemPrefab, rewardAreaContainer);
            RewardItemUI rewardUI = itemObj.GetComponent<RewardItemUI>();

            if (rewardUI != null)
            {
                // Nếu customIcon null thì tự lấy Icon mặc định theo RewardType
                Sprite iconToUse = reward.customIcon != null ? reward.customIcon : GetDefaultIcon(reward.rewardType);
                string suffix = (reward.rewardType == RewardType.Exp) ? " XP" : "";
                string amountStr = $"+{FormatNumber(reward.amount)}{suffix}";

                rewardUI.SetupReward(iconToUse, amountStr);
            }
        }
    }

    private void SetState(bool showButton, bool isInteractable, string textStr)
    {
        if (claimButton != null)
        {
            claimButton.gameObject.SetActive(showButton);
            claimButton.interactable = isInteractable;

            if (claimButtonText != null) 
                claimButtonText.text = textStr;
        }

        if (statusText != null)
        {
            statusText.gameObject.SetActive(!showButton);
            statusText.text = textStr;
        }
    }

    private Sprite GetDefaultIcon(RewardType type)
    {
        switch (type)
        {
            case RewardType.Gold:  return goldIcon;
            case RewardType.Exp:   return expIcon;
            case RewardType.Wood:  return woodIcon;
            case RewardType.Stone: return stoneIcon;
            default: return null;
        }
    }

    private string FormatNumber(int num)
    {
        if (num >= 1000000) return (num / 1000000f).ToString("0.#") + "M";
        if (num >= 1000) return (num / 1000f).ToString("0.#") + "K";
        return num.ToString();
    }
}