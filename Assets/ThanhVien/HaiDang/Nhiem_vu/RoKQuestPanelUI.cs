using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;


public class RoKQuestPanelUI : MonoBehaviour
{

    [Header("FONT")]
    public TMP_FontAsset vietnameseFont;
    public enum QuestType
    {
        Main,
        Side
    }

    [System.Serializable]
    public class Reward
    {
        public Sprite icon;
        public int amount;

        public Reward(Sprite icon, int amount)
        {
            this.icon = icon;
            this.amount = amount;
        }
    }

    [System.Serializable]
    public class Quest
    {
        public string id;
        public QuestType type;
        public Sprite icon;

        public string title;
        public string description;
        public string shortHint;

        public int current;
        public int target;
        public bool claimed;

        public List<Reward> rewards = new List<Reward>();

        public bool IsCompleted()
        {
            return current >= target;
        }
    }

    [System.Serializable]
    public class QuestGoEvent : UnityEvent<string> { }

    [Header("ROOT")]
    public GameObject questPanelRoot;
    public Button openButton;
    public Button closeButton;

    [Header("QUEST ICONS")]
    public Sprite trainArcherIcon;
    public Sprite scrollIcon;
    public Sprite barbarianIcon;
    public Sprite renameIcon;
    public Sprite watchTowerIcon;
    public Sprite cannonIcon;
    public Sprite storageIcon;
    public Sprite raidIcon;

    [Header("REWARD ICONS")]
    public Sprite speedupIcon;
    public Sprite foodIcon;
    public Sprite woodIcon;
    public Sprite stoneIcon;
    public Sprite goldIcon;
    public Sprite chestIcon;

    [Header("WOOD THEME")]
    [Tooltip("Bật để tự ép toàn bộ bảng nhiệm vụ sang màu vàng gỗ khi Play.")]
    public bool forceWoodThemeOnStart = true;

    [Header("STYLE - WOOD / MEDIEVAL")]
    public Color panelColor = new Color32(58, 36, 21, 255);          // #3A2415
    public Color headerColor = new Color32(107, 63, 31, 255);        // #6B3F1F
    public Color scrollBgColor = new Color32(43, 26, 16, 235);       // #2B1A10

    public Color mainHeaderColor = new Color32(255, 211, 90, 255);   // #FFD35A
    public Color sideHeaderColor = new Color32(242, 179, 90, 255);   // #F2B35A

    public Color mainCardColor = new Color32(184, 117, 50, 255);     // #B87532
    public Color sideCardColor = new Color32(122, 74, 36, 255);      // #7A4A24
    public Color cardBorderColor = new Color32(224, 166, 74, 255);   // #E0A64A

    public Color titleTextColor = new Color32(255, 241, 194, 255);   // #FFF1C2
    public Color descriptionTextColor = new Color32(232, 212, 162, 255); // #E8D4A2
    public Color rewardTextColor = new Color32(255, 224, 138, 255);  // #FFE08A

    public Color buttonColor = new Color32(199, 106, 27, 255);       // #C76A1B
    public Color buttonHighlightColor = new Color32(240, 167, 58, 255); // #F0A73A

    [Header("QUEST BUTTON STATES - UI ONLY")]
    [Tooltip("Nút Đi: nhiệm vụ chưa hoàn thành, bấm để dẫn người chơi tới vị trí làm nhiệm vụ.")]
    public Color goButtonColor = new Color32(38, 116, 170, 255);          // xanh dẫn đường
    public Color goButtonHighlightColor = new Color32(70, 165, 220, 255);
    public Color goButtonPressedColor = new Color32(22, 75, 118, 255);

    [Tooltip("Nút Nhận: nhiệm vụ đã hoàn thành, bấm để nhận thưởng.")]
    public Color claimButtonColor = new Color32(42, 145, 66, 255);        // xanh lá nhận thưởng
    public Color claimButtonHighlightColor = new Color32(77, 195, 88, 255);
    public Color claimButtonPressedColor = new Color32(28, 95, 44, 255);

    [Tooltip("Nút Xong: nhiệm vụ đã nhận thưởng, chỉ để báo trạng thái.")]
    public Color claimedButtonColor = new Color32(92, 78, 66, 255);       // xám gỗ đã xong
    public Color claimedButtonHighlightColor = new Color32(115, 95, 78, 255);
    public Color claimedButtonPressedColor = new Color32(70, 58, 48, 255);

    public Color goButtonTextColor = new Color32(255, 241, 194, 255);
    public Color claimButtonTextColor = new Color32(255, 255, 210, 255);
    public Color claimedButtonTextColor = new Color32(205, 190, 160, 255);

    public Color textColor = new Color32(255, 241, 194, 255);        // fallback

    [Header("LAYOUT")]
    public int panelSortingOrder = 5000;
    public float topOffset = 95f;
    public float bottomOffset = 35f;
    public float leftOffset = 35f;
    public float rightOffset = 35f;
    public float itemHeight = 145f;
    public float sectionHeight = 42f;
    public float spacing = 14f;

    [Header("OPTIONS")]
    public bool closePanelWhenPressGo = true;
    public bool debugMode = true;

    [Header("REWARD CLAIM FX")]
    public RoKCoinRewardFlyEffect coinRewardFlyEffect;

    [Tooltip("Kéo RectTransform của icon xu/vàng trên thanh top HUD vào đây.")]
    public RectTransform goldHudTarget;
    [Header("RESOURCE HUD TARGETS")]
    public RectTransform woodHudTarget;
    public RectTransform stoneHudTarget;
    public RectTransform foodHudTarget;

    [Header("RESOURCE CLAIM SFX")]
    public AudioClip goldRewardSfx;
    public AudioClip woodRewardSfx;
    public AudioClip stoneRewardSfx;
    public AudioClip foodRewardSfx;

    public bool playAllResourceFlyEffectOnClaim = true;

    public bool playGoldFlyEffectOnClaim = true;

    // Fix lỗi dòng đỏ này
    public int renameQuestFallbackGoldReward = 100;
    [Header("REWARD TO JSON")]
    public bool giveResourceRewardsOnClaim = true;
    public bool broadcastResourcesAfterClaim = true;

    private bool rewardClaimRunning;

    [Header("EVENT")]
    public QuestGoEvent onGoPressed;

    private RectTransform generatedContent;
    private readonly List<Quest> quests = new List<Quest>();
    private readonly Dictionary<string, Quest> questMap = new Dictionary<string, Quest>();
    public RoKQuestItemUI questItemPrefab;

    private const string GENERATED_ROOT_NAME = "GeneratedQuestScroll";

    void Start()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPanel);
            openButton.onClick.AddListener(OpenPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }

        BuildDefaultQuestList();

        if (forceWoodThemeOnStart)
            ApplyWoodThemeColors();

        ApplyTopCanvas();
        ApplyBasePanelColors();

        if (questPanelRoot != null)
            questPanelRoot.SetActive(false);
    }

    // =====================================================
    // QUEST DATA
    // =====================================================

    void BuildDefaultQuestList()
    {
        quests.Clear();
        questMap.Clear();

        AddQuest(new Quest
        {
            id = "train_archer",
            type = QuestType.Main,
            icon = trainArcherIcon,
            title = "Huấn luyện cung thủ",
            current = 0,
            target = 20,
            description = "Huấn luyện 20 đơn vị cung thủ",
            shortHint = "Chuẩn bị lực lượng tầm xa.",
            rewards =
            {
                new Reward(speedupIcon, 2)
            }
        });

        AddQuest(new Quest
        {
            id = "build_watchtower",
            type = QuestType.Main,
            icon = watchTowerIcon,
            title = "Xây Tháp Canh",
            current = 0,
            target = 1,
            description = "Xây 1 Tháp Canh để phát hiện kẻ địch",
            shortHint = "Mở tầm nhìn phòng thủ.",
            rewards =
            {
                new Reward(woodIcon, 100),
                new Reward(stoneIcon, 50)
            }
        });

        AddQuest(new Quest
        {
            id = "unlock_cannon",
            type = QuestType.Main,
            icon = cannonIcon,
            title = "Mở khóa Pháo Thủ",
            current = 0,
            target = 1,
            description = "Mở khóa công trình Pháo Thủ",
            shortHint = "Tăng sức mạnh phòng thủ.",
            rewards =
            {
                new Reward(chestIcon, 1)
            }
        });

        AddQuest(new Quest
        {
            id = "first_raid",
            type = QuestType.Main,
            icon = raidIcon,
            title = "Đánh bại đợt cướp đầu tiên",
            current = 0,
            target = 1,
            description = "Đẩy lùi nhóm cướp tấn công làng",
            shortHint = "Bảo vệ dân làng.",
            rewards =
            {
                new Reward(goldIcon, 200)
            }
        });

        AddQuest(new Quest
        {
            id = "landlord",
            type = QuestType.Side,
            icon = scrollIcon,
            title = "Đại địa chủ",
            current = 451,
            target = 500,
            description = "Đạt 500 sản lượng Lúa",
            shortHint = "Tăng nguồn lương thực.",
            rewards =
            {
                new Reward(foodIcon, 2500),
                new Reward(woodIcon, 2500),
                new Reward(chestIcon, 1)
            }
        });

        AddQuest(new Quest
        {
            id = "civilization_land",
            type = QuestType.Side,
            icon = barbarianIcon,
            title = "Xứ sở của nền văn minh",
            current = 1,
            target = 2,
            description = "Đánh bại 2 đội quân Man Di trên bản đồ",
            shortHint = "Dọn sạch hiểm họa quanh làng.",
            rewards =
            {
                new Reward(woodIcon, 2000),
                new Reward(stoneIcon, 2000),
                new Reward(speedupIcon, 1)
            }
        });

        AddQuest(new Quest
        {
            id = "my_name",
            type = QuestType.Side,
            icon = renameIcon,
            title = "Bằng tên tôi",
            current = 0,
            target = 1,
            description = "Đặt biệt danh trong hồ sơ thống đốc",
            shortHint = "Khẳng định danh tính.",
            rewards =
            {
                new Reward(goldIcon, 100)
            }
        });

        AddQuest(new Quest
        {
            id = "gather_wood",
            type = QuestType.Side,
            icon = storageIcon,
            title = "Người gom góp",
            current = 120,
            target = 300,
            description = "Thu thập 300 Gỗ ngoài bản đồ",
            shortHint = "Tăng nguồn xây dựng.",
            rewards =
            {
                new Reward(woodIcon, 1000)
            }
        });

        AddQuest(new Quest
        {
            id = "upgrade_storage",
            type = QuestType.Side,
            icon = storageIcon,
            title = "Người giữ kho",
            current = 0,
            target = 1,
            description = "Nâng cấp Kho chứa gỗ lên cấp 3",
            shortHint = "Bảo vệ tài nguyên.",
            rewards =
            {
                new Reward(stoneIcon, 500),
                new Reward(foodIcon, 500)
            }
        });
    }

    void AddQuest(Quest quest)
    {
        quests.Add(quest);
        questMap[quest.id] = quest;
    }

    // =====================================================
    // PANEL OPEN / CLOSE
    // =====================================================

    public void OpenPanel()
    {
        if (questPanelRoot == null)
        {
            Debug.LogError("[RoKQuestPanelUI] QuestPanelRoot chưa gán.");
            return;
        }

        questPanelRoot.SetActive(true);
        questPanelRoot.transform.SetAsLastSibling();

        ApplyTopCanvas();
        RenderQuestList();
    }

    public void ClosePanel()
    {
        if (questPanelRoot != null)
            questPanelRoot.SetActive(false);
    }

    void ApplyTopCanvas()
    {
        if (questPanelRoot == null) return;

        Canvas canvas = questPanelRoot.GetComponent<Canvas>();
        if (canvas == null)
            canvas = questPanelRoot.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = panelSortingOrder;

        if (questPanelRoot.GetComponent<GraphicRaycaster>() == null)
            questPanelRoot.AddComponent<GraphicRaycaster>();

        questPanelRoot.transform.SetAsLastSibling();
    }

    [ContextMenu("Apply Wood Theme Colors")]
    public void ApplyWoodThemeColors()
    {
        panelColor = new Color32(58, 36, 21, 255);
        headerColor = new Color32(107, 63, 31, 255);
        scrollBgColor = new Color32(43, 26, 16, 235);

        mainHeaderColor = new Color32(255, 211, 90, 255);
        sideHeaderColor = new Color32(242, 179, 90, 255);

        mainCardColor = new Color32(184, 117, 50, 255);
        sideCardColor = new Color32(122, 74, 36, 255);
        cardBorderColor = new Color32(224, 166, 74, 255);

        titleTextColor = new Color32(255, 241, 194, 255);
        descriptionTextColor = new Color32(232, 212, 162, 255);
        rewardTextColor = new Color32(255, 224, 138, 255);

        buttonColor = new Color32(199, 106, 27, 255);
        buttonHighlightColor = new Color32(240, 167, 58, 255);

        goButtonColor = new Color32(38, 116, 170, 255);
        goButtonHighlightColor = new Color32(70, 165, 220, 255);
        goButtonPressedColor = new Color32(22, 75, 118, 255);

        claimButtonColor = new Color32(42, 145, 66, 255);
        claimButtonHighlightColor = new Color32(77, 195, 88, 255);
        claimButtonPressedColor = new Color32(28, 95, 44, 255);

        claimedButtonColor = new Color32(92, 78, 66, 255);
        claimedButtonHighlightColor = new Color32(115, 95, 78, 255);
        claimedButtonPressedColor = new Color32(70, 58, 48, 255);

        goButtonTextColor = new Color32(255, 241, 194, 255);
        claimButtonTextColor = new Color32(255, 255, 210, 255);
        claimedButtonTextColor = new Color32(205, 190, 160, 255);

        textColor = titleTextColor;

        ApplyBasePanelColors();
    }

    void ApplyBasePanelColors()
    {
        if (questPanelRoot == null) return;

        Image rootImage = questPanelRoot.GetComponent<Image>();
        if (rootImage != null)
            rootImage.color = panelColor;

        Transform header = questPanelRoot.transform.Find("Header");
        if (header != null)
        {
            Image headerImage = header.GetComponent<Image>();
            if (headerImage != null)
                headerImage.color = headerColor;
        }
    }

    // =====================================================
    // RENDER UI
    // =====================================================

    public void RenderQuestList()
    {
        if (questPanelRoot == null)
        {
            Debug.LogError("[RoKQuestPanelUI] QuestPanelRoot NULL.");
            return;
        }

        ClearOldGeneratedUI();
        ApplyBasePanelColors();

        GameObject scrollGO = new GameObject(GENERATED_ROOT_NAME, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(questPanelRoot.transform, false);
        scrollGO.transform.SetAsLastSibling();

        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(leftOffset, bottomOffset);
        scrollRT.offsetMax = new Vector2(-rightOffset, -topOffset);

        Image scrollBg = scrollGO.GetComponent<Image>();
        scrollBg.color = scrollBgColor;

        ScrollRect scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;

        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportGO.transform.SetParent(scrollGO.transform, false);

        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;

        Image viewportImage = viewportGO.GetComponent<Image>();
        viewportImage.color = new Color(1, 1, 1, 0.01f);

        GameObject contentGO = new GameObject("GeneratedContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);

        generatedContent = contentGO.GetComponent<RectTransform>();
        generatedContent.anchorMin = new Vector2(0, 1);
        generatedContent.anchorMax = new Vector2(1, 1);
        generatedContent.pivot = new Vector2(0.5f, 1);
        generatedContent.anchoredPosition = Vector2.zero;
        generatedContent.offsetMin = new Vector2(0, generatedContent.offsetMin.y);
        generatedContent.offsetMax = new Vector2(0, generatedContent.offsetMax.y);

        VerticalLayoutGroup layout = contentGO.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = spacing;
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRT;
        scroll.content = generatedContent;

        CreateSection("◆ Nhiệm vụ chính", mainHeaderColor);

        foreach (Quest quest in quests)
        {
            if (quest.type == QuestType.Main)
                CreateQuestCard(quest);
        }

        CreateSection("◆ Nhiệm vụ phụ", sideHeaderColor);

        foreach (Quest quest in quests)
        {
            if (quest.type == QuestType.Side)
                CreateQuestCard(quest);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(generatedContent);
        Canvas.ForceUpdateCanvases();

        if (debugMode)
            Debug.Log("[RoKQuestPanelUI] Render xong. Quest count = " + quests.Count);
    }

    void ClearOldGeneratedUI()
    {
        Transform old = questPanelRoot.transform.Find(GENERATED_ROOT_NAME);

        if (old != null)
            Destroy(old.gameObject);
    }

    // =====================================================
    // CREATE PARTS
    // =====================================================

    void CreateSection(string title, Color color)
    {
        GameObject go = new GameObject(title, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(generatedContent, false);

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = title;
        text.color = color;
        text.fontSize = 28;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        if (vietnameseFont != null)
        {
            text.font = vietnameseFont;
        }

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.minHeight = sectionHeight;
        le.preferredHeight = sectionHeight;
        le.flexibleWidth = 1;
    }

    void CreateQuestCard(Quest quest)
    {
        GameObject card = new GameObject("QuestItem_" + quest.id, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Outline));
        card.transform.SetParent(generatedContent, false);

        Image bg = card.GetComponent<Image>();
        bg.color = quest.type == QuestType.Main ? mainCardColor : sideCardColor;
        bg.raycastTarget = true;

        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = cardBorderColor;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, itemHeight);

        LayoutElement le = card.GetComponent<LayoutElement>();
        le.minHeight = itemHeight;
        le.preferredHeight = itemHeight;
        le.flexibleWidth = 1;

        CreateQuestIcon(card.transform, quest.icon);
        CreateQuestTexts(card.transform, quest);
        CreateRewards(card.transform, quest);
        CreateGoButton(card.transform, quest);
    }

    void CreateQuestIcon(Transform parent, Sprite sprite)
    {
        float frameSize = 95f;

        Image frame = CreateImage(
            parent,
            "QuestIconFrame",
            new Vector2(70, 0),
            new Vector2(frameSize, frameSize),
            Anchor.LeftMiddle
        );

        frame.raycastTarget = false;

        // Nếu có icon thật: không dùng nền vàng nữa, để icon tự hiện full khung
        if (sprite != null)
        {
            frame.color = new Color(1f, 1f, 1f, 0f); // trong suốt

            Image icon = CreateImage(
                frame.transform,
                "QuestIcon",
                Vector2.zero,
                new Vector2(frameSize, frameSize),
                Anchor.Center
            );

            icon.sprite = sprite;
            icon.enabled = true;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }
        else
        {
            // Nếu chưa gán icon thì hiện ô vàng + dấu ?
            frame.color = new Color32(255, 210, 45, 255);

            TMP_Text fallback = CreateText(
                frame.transform,
                "IconFallback",
                "?",
                Vector2.zero,
                new Vector2(frameSize, frameSize),
                40,
                Anchor.Center
            );

            fallback.alignment = TextAlignmentOptions.Center;
            fallback.fontStyle = FontStyles.Bold;
            fallback.color = Color.white;
        }
    }

    void CreateQuestTexts(Transform parent, Quest quest)
    {
        TMP_Text title = CreateText(parent, "QuestTitleText", quest.title, new Vector2(130, -18), new Vector2(700, 34), 30, Anchor.TopLeft);
        title.fontStyle = FontStyles.Bold;
        title.color = titleTextColor;

        TMP_Text progress = CreateText(
            parent,
            "QuestProgressText",
            $"({quest.current}/{quest.target}) {quest.description}",
            new Vector2(130, -52),
            new Vector2(780, 28),
            22,
            Anchor.TopLeft
        );
        progress.color = descriptionTextColor;

        TMP_Text hint = CreateText(parent, "QuestDescriptionText", quest.shortHint, new Vector2(130, -80), new Vector2(780, 26), 19, Anchor.TopLeft);
        hint.color = descriptionTextColor;
    }

    void CreateRewards(Transform parent, Quest quest)
    {
        TMP_Text label = CreateText(parent, "RewardLabelText", "Thưởng", new Vector2(130, -112), new Vector2(90, 26), 20, Anchor.TopLeft);
        label.color = rewardTextColor;

        for (int i = 0; i < quest.rewards.Count; i++)
        {
            float x = 225 + i * 120;

            Image icon = CreateImage(parent, "RewardIcon_" + i, new Vector2(x, -101), new Vector2(30, 30), Anchor.TopLeft);
            icon.sprite = quest.rewards[i].icon;
            icon.color = Color.white;
            icon.enabled = quest.rewards[i].icon != null;

            TMP_Text amount = CreateText(parent, "RewardText_" + i, FormatAmount(quest.rewards[i].amount), new Vector2(x + 36, -106), new Vector2(85, 26), 20, Anchor.TopLeft);
            amount.fontStyle = FontStyles.Bold;
            amount.color = rewardTextColor;
        }
    }

    void CreateGoButton(Transform parent, Quest quest)
    {
        string label = GetQuestButtonLabel(quest);
        Color normalColor = GetQuestButtonNormalColor(quest);
        Color highlightColor = GetQuestButtonHighlightColor(quest);
        Color pressedColor = GetQuestButtonPressedColor(quest);
        Color textStateColor = GetQuestButtonTextColor(quest);

        GameObject go = new GameObject("GoButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        SetAnchor(rt, Anchor.RightMiddle);
        rt.anchoredPosition = new Vector2(-35, 0);
        rt.sizeDelta = new Vector2(130, 55);

        Image img = go.GetComponent<Image>();
        img.color = normalColor;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = quest.claimed ? new Color32(120, 95, 70, 255) : cardBorderColor;
        outline.effectDistance = quest.IsCompleted() && !quest.claimed ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        Button btn = go.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightColor;
        colors.selectedColor = highlightColor;
        colors.pressedColor = pressedColor;
        colors.disabledColor = claimedButtonColor;
        colors.colorMultiplier = 1f;
        btn.colors = colors;

        btn.onClick.AddListener(() => OnQuestButtonClicked(quest.id, rt));

        TMP_Text text = CreateText(
            go.transform,
            "GoButtonText",
            label,
            Vector2.zero,
            new Vector2(130, 55),
            28,
            Anchor.Center
        );

        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.color = textStateColor;
    }

    string GetQuestButtonLabel(Quest quest)
    {
        if (quest.claimed)
            return "Xong";

        if (quest.IsCompleted())
            return "Nhận";

        return "Đi";
    }

    Color GetQuestButtonNormalColor(Quest quest)
    {
        if (quest.claimed)
            return claimedButtonColor;

        if (quest.IsCompleted())
            return claimButtonColor;

        return goButtonColor;
    }

    Color GetQuestButtonHighlightColor(Quest quest)
    {
        if (quest.claimed)
            return claimedButtonHighlightColor;

        if (quest.IsCompleted())
            return claimButtonHighlightColor;

        return goButtonHighlightColor;
    }

    Color GetQuestButtonPressedColor(Quest quest)
    {
        if (quest.claimed)
            return claimedButtonPressedColor;

        if (quest.IsCompleted())
            return claimButtonPressedColor;

        return goButtonPressedColor;
    }

    Color GetQuestButtonTextColor(Quest quest)
    {
        if (quest.claimed)
            return claimedButtonTextColor;

        if (quest.IsCompleted())
            return claimButtonTextColor;

        return goButtonTextColor;
    }

    // =====================================================
    // CREATE UI HELPERS
    // =====================================================

    enum Anchor
    {
        Center,
        TopLeft,
        LeftMiddle,
        RightMiddle
    }

    Image CreateImage(Transform parent, string name, Vector2 pos, Vector2 size, Anchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        SetAnchor(rt, anchor);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;

        return img;
    }

    TMP_Text CreateText(Transform parent, string name, string value, Vector2 pos, Vector2 size, int fontSize, Anchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        SetAnchor(rt, anchor);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = textColor;
        if (vietnameseFont != null)
        {
            text.font = vietnameseFont;
        }
        text.alignment = TextAlignmentOptions.Left;
        text.fontStyle = FontStyles.Normal;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;

        return text;
    }

    void SetAnchor(RectTransform rt, Anchor anchor)
    {
        switch (anchor)
        {
            case Anchor.Center:
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                break;

            case Anchor.TopLeft:
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                break;

            case Anchor.LeftMiddle:
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                break;

            case Anchor.RightMiddle:
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                break;
        }
    }

    // =====================================================
    // QUEST BUTTON LOGIC
    // =====================================================

    public void OnQuestButtonClicked(string questId, RectTransform clickSource = null)
    {
        if (!questMap.ContainsKey(questId))
            return;

        Quest quest = questMap[questId];

        if (quest.claimed)
            return;

        // Nút "Nhận"
        if (quest.IsCompleted())
        {
            if (!rewardClaimRunning)
                StartCoroutine(ClaimQuestRewardRoutine(quest, clickSource));

            return;
        }

        // Nút "Đi"
        Debug.Log("[Quest] Đi tới nhiệm vụ: " + quest.title);
        onGoPressed?.Invoke(questId);

        if (closePanelWhenPressGo)
            ClosePanel();
    }
    void GiveQuestRewardsToJson(Quest quest)
    {
        JsonDataManager data = FindObjectOfType<JsonDataManager>();

        if (data == null)
        {
            Debug.LogWarning("[RoKQuestPanelUI] Không tìm thấy JsonDataManager. Không cộng được thưởng.");
            return;
        }

        bool matchedAnyResource = false;

        foreach (Reward reward in quest.rewards)
        {
            if (reward.amount <= 0)
                continue;

            if (reward.icon == goldIcon)
            {
                data.AddGold(reward.amount);
                matchedAnyResource = true;
            }
            else if (reward.icon == woodIcon)
            {
                data.AddWood(reward.amount);
                matchedAnyResource = true;
            }
            else if (reward.icon == stoneIcon)
            {
                data.AddStone(reward.amount);
                matchedAnyResource = true;
            }
            else if (reward.icon == foodIcon)
            {
                data.AddFood(reward.amount);
                matchedAnyResource = true;
            }
        }

        // Fallback nếu icon reward chưa gán đúng
        if (!matchedAnyResource)
            GiveFallbackReward(data, quest.id);

        if (broadcastResourcesAfterClaim)
            data.BroadcastAllResources();

        Debug.Log("[RoKQuestPanelUI] Đã nhận thưởng quest: " + quest.id);
    }

    void GiveFallbackReward(JsonDataManager data, string questId)
    {
        switch (questId)
        {
            case "build_watchtower":
                data.AddWood(100);
                data.AddStone(50);
                break;

            case "my_name":
                data.AddGold(renameQuestFallbackGoldReward);
                break;

            case "first_raid":
                data.AddGold(200);
                break;

            case "landlord":
                data.AddFood(2500);
                data.AddWood(2500);
                break;

            case "gather_wood":
                data.AddWood(1000);
                break;

            case "upgrade_storage":
                data.AddStone(500);
                data.AddFood(500);
                break;
        }
    }

    IEnumerator ClaimQuestRewardRoutine(Quest quest, RectTransform clickSource)
    {
        rewardClaimRunning = true;

        JsonDataManager data = FindObjectOfType<JsonDataManager>();

        if (data == null)
        {
            Debug.LogWarning("[RoKQuestPanelUI] Không tìm thấy JsonDataManager.");
            rewardClaimRunning = false;
            yield break;
        }

        bool matchedAnyResource = false;

        foreach (Reward reward in quest.rewards)
        {
            if (reward.amount <= 0)
                continue;

            if (reward.icon == goldIcon)
            {
                matchedAnyResource = true;

                yield return PlayAndAddResource(
                    clickSource,
                    goldHudTarget,
                    goldIcon,
                    reward.amount,
                    goldRewardSfx,
                    () => data.AddGold(reward.amount),
                    "Gold"
                );
            }
            else if (reward.icon == woodIcon)
            {
                matchedAnyResource = true;

                yield return PlayAndAddResource(
                    clickSource,
                    woodHudTarget,
                    woodIcon,
                    reward.amount,
                    woodRewardSfx,
                    () => data.AddWood(reward.amount),
                    "Wood"
                );
            }
            else if (reward.icon == stoneIcon)
            {
                matchedAnyResource = true;

                yield return PlayAndAddResource(
                    clickSource,
                    stoneHudTarget,
                    stoneIcon,
                    reward.amount,
                    stoneRewardSfx,
                    () => data.AddStone(reward.amount),
                    "Stone"
                );
            }
            else if (reward.icon == foodIcon)
            {
                matchedAnyResource = true;

                yield return PlayAndAddResource(
                    clickSource,
                    foodHudTarget,
                    foodIcon,
                    reward.amount,
                    foodRewardSfx,
                    () => data.AddFood(reward.amount),
                    "Food"
                );
            }
        }

        // Fallback nếu icon reward chưa match đúng
        if (!matchedAnyResource)
            yield return GiveFallbackRewardWithFx(data, quest.id, clickSource);

        if (broadcastResourcesAfterClaim)
            data.BroadcastAllResources();

        quest.claimed = true;
        rewardClaimRunning = false;

        RenderQuestList();

        Debug.Log("[RoKQuestPanelUI] Đã nhận thưởng quest: " + quest.id);
    }


    IEnumerator PlayAndAddResource(
    RectTransform from,
    RectTransform hudTarget,
    Sprite sprite,
    int amount,
    AudioClip sfx,
    System.Action addAction,
    string debugName
)
    {
        if (amount <= 0)
            yield break;

        if (playAllResourceFlyEffectOnClaim &&
            coinRewardFlyEffect != null &&
            hudTarget != null)
        {
            bool done = false;

            coinRewardFlyEffect.PlayResourceFly(
                from,
                hudTarget,
                sprite,
                amount,
                sfx,
                () =>
                {
                    addAction?.Invoke();
                    done = true;
                }
            );

            yield return new WaitUntil(() => done);
        }
        else
        {
            if (coinRewardFlyEffect == null)
                Debug.LogWarning("[RewardFX] Thiếu Coin Reward Fly Effect.");

            if (hudTarget == null)
                Debug.LogWarning("[RewardFX] Thiếu HUD target cho: " + debugName);

            addAction?.Invoke();
        }
    }

    IEnumerator GiveFallbackRewardWithFx(JsonDataManager data, string questId, RectTransform clickSource)
    {
        switch (questId)
        {
            case "build_watchtower":
                yield return PlayAndAddResource(
                    clickSource,
                    woodHudTarget,
                    woodIcon,
                    100,
                    woodRewardSfx,
                    () => data.AddWood(100),
                    "Wood"
                );

                yield return PlayAndAddResource(
                    clickSource,
                    stoneHudTarget,
                    stoneIcon,
                    50,
                    stoneRewardSfx,
                    () => data.AddStone(50),
                    "Stone"
                );
                break;

            case "my_name":
                yield return PlayAndAddResource(
                    clickSource,
                    goldHudTarget,
                    goldIcon,
                    renameQuestFallbackGoldReward,
                    goldRewardSfx,
                    () => data.AddGold(renameQuestFallbackGoldReward),
                    "Gold"
                );
                break;

            case "first_raid":
                yield return PlayAndAddResource(
                    clickSource,
                    goldHudTarget,
                    goldIcon,
                    200,
                    goldRewardSfx,
                    () => data.AddGold(200),
                    "Gold"
                );
                break;

            case "landlord":
                yield return PlayAndAddResource(
                    clickSource,
                    foodHudTarget,
                    foodIcon,
                    2500,
                    foodRewardSfx,
                    () => data.AddFood(2500),
                    "Food"
                );

                yield return PlayAndAddResource(
                    clickSource,
                    woodHudTarget,
                    woodIcon,
                    2500,
                    woodRewardSfx,
                    () => data.AddWood(2500),
                    "Wood"
                );
                break;

            case "gather_wood":
                yield return PlayAndAddResource(
                    clickSource,
                    woodHudTarget,
                    woodIcon,
                    1000,
                    woodRewardSfx,
                    () => data.AddWood(1000),
                    "Wood"
                );
                break;

            case "upgrade_storage":
                yield return PlayAndAddResource(
                    clickSource,
                    stoneHudTarget,
                    stoneIcon,
                    500,
                    stoneRewardSfx,
                    () => data.AddStone(500),
                    "Stone"
                );

                yield return PlayAndAddResource(
                    clickSource,
                    foodHudTarget,
                    foodIcon,
                    500,
                    foodRewardSfx,
                    () => data.AddFood(500),
                    "Food"
                );
                break;
        }
    }

    int GetGoldRewardAmount(Quest quest)
    {
        int total = 0;

        foreach (Reward reward in quest.rewards)
        {
            if (reward.icon == goldIcon)
                total += reward.amount;
        }

        // Fallback riêng cho nhiệm vụ đổi tên nếu chưa gán đúng goldIcon
        if (total <= 0 && quest.id == "my_name")
            total = renameQuestFallbackGoldReward;

        return total;
    }

    void GiveNonGoldResourceRewards(Quest quest)
    {
        JsonDataManager data = FindObjectOfType<JsonDataManager>();

        if (data == null)
        {
            Debug.LogWarning("[RoKQuestPanelUI] Không tìm thấy JsonDataManager.");
            return;
        }

        foreach (Reward reward in quest.rewards)
        {
            if (reward.amount <= 0)
                continue;

            // Gold đã cộng bằng hiệu ứng rồi, bỏ qua ở đây
            if (reward.icon == goldIcon)
                continue;

            if (reward.icon == woodIcon)
                data.AddWood(reward.amount);
            else if (reward.icon == stoneIcon)
                data.AddStone(reward.amount);
            else if (reward.icon == foodIcon)
                data.AddFood(reward.amount);
        }

        data.BroadcastAllResources();
    }

    void AddGoldReward(int amount)
    {
        if (amount <= 0)
            return;

        JsonDataManager data = FindObjectOfType<JsonDataManager>();

        if (data == null)
        {
            Debug.LogWarning("[RoKQuestPanelUI] Không tìm thấy JsonDataManager.");
            return;
        }

        data.AddGold(amount);
        data.BroadcastAllResources();

        Debug.Log("[RoKQuestPanelUI] Đã cộng xu/vàng: +" + amount);
    }

    public void SetProgress(string questId, int value)
    {
        if (!questMap.ContainsKey(questId))
            return;

        Quest quest = questMap[questId];
        quest.current = Mathf.Clamp(value, 0, quest.target);
        RenderQuestList();
    }

    public void AddProgress(string questId, int amount)
    {
        if (!questMap.ContainsKey(questId))
            return;

        Quest quest = questMap[questId];
        quest.current = Mathf.Clamp(quest.current + amount, 0, quest.target);
        RenderQuestList();
    }

    public void CompleteQuest(string questId)
    {
        if (!questMap.ContainsKey(questId))
            return;

        Quest quest = questMap[questId];
        quest.current = quest.target;
        RenderQuestList();
    }

    public string FormatAmount(int value)
    {
        return value.ToString("#,0").Replace(",", ".");
    }
    public void OnQuestGoClicked(string questId)
    {
        Debug.Log("[QuestPanel] Bridge GO: " + questId);

        if (onGoPressed != null)
            onGoPressed.Invoke(questId);
    }
    private void CreateQuestItem(Quest quest)
    {
        RoKQuestItemUI item = Instantiate(questItemPrefab, generatedContent);

        item.Bind(quest, this);

        // 👉 LINK ITEM → PANEL → TUTORIAL
        item.onGoClicked = OnQuestGoClicked;
    }

}