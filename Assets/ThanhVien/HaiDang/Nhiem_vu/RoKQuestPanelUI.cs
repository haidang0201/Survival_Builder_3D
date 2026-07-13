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
    /// <summary>
    /// Main: nhiệm vụ chính theo cốt truyện, chạy tuần tự từng giai đoạn.
    /// Side: nhiệm vụ phụ, làm song song tự do, không liên quan cốt truyện chính.
    /// Urgent: nhiệm vụ ép buộc/khẩn cấp (vd: đang bị tấn công). Mặc định ẨN,
    /// chỉ xuất hiện khi bị kích hoạt qua ActivateUrgentQuest(), và khi active
    /// sẽ CHIẾM TOÀN BỘ panel — ẩn hết mọi nhiệm vụ Main/Side khác cho tới khi
    /// nhiệm vụ khẩn cấp được xử lý xong.
    /// </summary>
    public enum QuestType
    {
        Main,
        Side,
        Urgent
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

        [Tooltip("Chỉ dùng cho QuestType.Urgent: true khi nhiệm vụ khẩn cấp này đang được kích hoạt bởi sự kiện game (vd bị tấn công). Kích hoạt qua ActivateUrgentQuest().")]
        public bool isActive = false;

        [Tooltip("Chỉ dùng cho QuestType.Urgent: id của một nhiệm vụ CHÍNH phía sau sẽ tự động được bỏ qua (đánh dấu hoàn thành) khi nhiệm vụ khẩn cấp này được claim, để tránh bắt người chơi làm trùng hành động (vd đã xây Tháp Canh khẩn cấp thì không bắt xây lại ở nhiệm vụ chính).")]
        public string skipsMainQuestId;

        [Tooltip("Danh sách id các nhiệm vụ ĐIỀU KIỆN (có thể là Main hoặc Side) phải được claim xong thì nhiệm vụ này mới được MỞ KHOÁ (hiển thị + nhận tiến độ). Để trống nếu nhiệm vụ không phụ thuộc nhiệm vụ nào khác. Dùng để tạo liên kết chặt chẽ giữa các nhiệm vụ, không giới hạn trong 1 loại (Main có thể phụ thuộc Side và ngược lại).")]
        public List<string> prerequisiteQuestIds = new List<string>();

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
    public Sprite allianceIcon;

    [Header("REWARD ICONS")]
    public Sprite speedupIcon;
    public Sprite foodIcon;
    public Sprite woodIcon;
    public Sprite stoneIcon;
    public Sprite goldIcon;
    public Sprite chestIcon;

    // =====================================================
    // UI SPRITES - gán ảnh trực tiếp trong Inspector giống file hồ sơ.
    // Nếu để trống, code giữ nguyên màu nền và Outline cũ.
    // =====================================================
    [Header("UI SPRITES")]
    public Sprite panelBackgroundSprite;
    public Sprite headerBackgroundSprite;
    public Sprite scrollBackgroundSprite;

    public Sprite mainQuestCardSprite;
    public Sprite sideQuestCardSprite;
    public Sprite urgentQuestCardSprite;

    public Sprite goButtonSprite;
    public Sprite claimButtonSprite;
    public Sprite claimedButtonSprite;

    public Sprite closeButtonSprite;

    [Header("WOOD THEME")]
    [Tooltip("Bật để tự ép toàn bộ bảng nhiệm vụ sang màu vàng gỗ khi Play.")]
    public bool forceWoodThemeOnStart = true;

    [Header("STYLE - WOOD / MEDIEVAL")]
    public Color panelColor = new Color32(58, 36, 21, 255);          // #3A2415
    public Color headerColor = new Color32(107, 63, 31, 255);        // #6B3F1F
    public Color scrollBgColor = new Color32(43, 26, 16, 235);       // #2B1A10

    public Color mainHeaderColor = new Color32(255, 211, 90, 255);   // #FFD35A
    public Color sideHeaderColor = new Color32(242, 179, 90, 255);   // #F2B35A
    public Color urgentHeaderColor = new Color32(230, 76, 60, 255);  // đỏ cảnh báo - nhiệm vụ khẩn cấp

    public Color mainCardColor = new Color32(184, 117, 50, 255);     // #B87532
    public Color sideCardColor = new Color32(122, 74, 36, 255);      // #7A4A24
    public Color urgentCardColor = new Color32(120, 35, 28, 255);    // nền đỏ sậm - nhiệm vụ khẩn cấp
    public Color cardBorderColor = new Color32(224, 166, 74, 255);   // #E0A64A
    public Color urgentBorderColor = new Color32(255, 140, 120, 255); // viền đỏ sáng - nhiệm vụ khẩn cấp

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

    [Header("MAIN QUEST STAGE FLOW")]
    [Tooltip("Bật để nhiệm vụ CHÍNH hiển thị theo từng giai đoạn (giống tutorial): chỉ hiện nhiệm vụ chính đang cần làm, các nhiệm vụ chính phía sau sẽ ẩn cho tới khi tới lượt.")]
    public bool sequentialMainQuests = true;

    [Tooltip("Nếu bật: các nhiệm vụ chính đã nhận thưởng (claimed) vẫn được liệt kê lại phía trên nhiệm vụ đang làm, để người chơi xem lại lịch sử. Nếu tắt: nhiệm vụ chính đã nhận thưởng sẽ biến mất khỏi danh sách, chỉ còn nhiệm vụ hiện tại.")]
    public bool keepClaimedMainQuestsAsHistory = false;

    [Tooltip("Khi có 1 nhiệm vụ Urgent đang active (vd đang bị tấn công), panel sẽ chỉ hiển thị DUY NHẤT nhiệm vụ đó, ẩn hết nhiệm vụ Main/Side khác, ép người chơi phải xử lý nó trước. Tắt cờ này để nhiệm vụ Urgent chỉ hiển thị thêm chứ không che các nhiệm vụ khác (không khuyến khích).")]
    public bool urgentQuestTakesOverPanel = true;

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
                //new Reward(speedupIcon, 2)
                new Reward(woodIcon, 300),
                new Reward(stoneIcon, 150)
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

        // Nhiệm vụ KHẨN CẤP (ví dụ): kích hoạt bằng ActivateUrgentQuest("urgent_defend_watchtower")
        // khi làng bị tấn công. Khi active, panel sẽ CHỈ hiển thị một mình nhiệm vụ này.
        // skipsMainQuestId = "build_watchtower" -> nếu người chơi đã xây Tháp Canh để chống
        // đợt tấn công khẩn cấp, thì nhiệm vụ chính "Xây Tháp Canh" phía sau sẽ tự động được
        // bỏ qua (không bắt xây lần 2).
        AddQuest(new Quest
        {
            id = "urgent_defend_watchtower",
            type = QuestType.Urgent,
            icon = watchTowerIcon,
            title = "⚠ Khẩn cấp: Xây Tháp Canh phòng thủ!",
            current = 0,
            target = 1,
            description = "Làng đang bị tấn công! Xây ngay 1 Tháp Canh để phòng thủ.",
            shortHint = "Ưu tiên tuyệt đối, tạm dừng mọi nhiệm vụ khác.",
            isActive = false,
            skipsMainQuestId = "build_watchtower",
            rewards =
            {
                new Reward(woodIcon, 150),
                new Reward(stoneIcon, 80)
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
                //new Reward(chestIcon, 1),
                new Reward(stoneIcon, 2200),
                new Reward(woodIcon, 500),
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

        // Nhiệm vụ chính thứ 5 - đủ số lượng demo cho buổi review tuần sau.
        // prerequisiteQuestIds: minh hoạ liên kết CHÉO - nhiệm vụ CHÍNH này phụ thuộc
        // vào 1 nhiệm vụ PHỤ ("civilization_land") đã claim xong, không chỉ phụ thuộc
        // thứ tự tuyến tính của chuỗi Main.
        AddQuest(new Quest
        {
            id = "join_alliance",
            type = QuestType.Main,
            icon = allianceIcon,
            title = "Gia nhập Liên Minh",
            current = 0,
            target = 1,
            description = "Gia nhập 1 Liên Minh để nhận hỗ trợ từ đồng minh",
            shortHint = "Kết nối cộng đồng, mở khoá tính năng liên minh.",
            prerequisiteQuestIds = { "civilization_land" },
            rewards =
            {
                new Reward(goldIcon, 300),
                new Reward(speedupIcon, 2)
            }
        });

        AddQuest(new Quest
        {
            id = "landlord",
            type = QuestType.Side,
            icon = scrollIcon,
            title = "Đại địa chủ",
            current = 199,
            target = 500,
            description = "Đạt 500 sản lượng Lúa ngoài bản đồ",
            shortHint = "Tăng nguồn lương thực.",
            rewards =
            {
                new Reward(foodIcon, 2500),
                new Reward(woodIcon, 2500),
               // new Reward(chestIcon, 1)
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
            prerequisiteQuestIds = { "first_raid" },
            rewards =
            {
                new Reward(woodIcon, 2000),
                new Reward(stoneIcon, 2000),
               // new Reward(speedupIcon, 1)
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
            prerequisiteQuestIds = { "gather_wood" },
            rewards =
            {
                new Reward(stoneIcon, 500),
                new Reward(foodIcon, 500)
            }
        });

        // ---- Thêm nhiệm vụ PHỤ song song, KHÔNG liên quan tới cốt truyện chính ----

        AddQuest(new Quest
        {
            id = "expand_land",
            type = QuestType.Side,
            icon = scrollIcon,
            title = "Mở rộng đất đai",
            current = 0,
            target = 1,
            description = "Mở rộng thêm 1 ô đất xây dựng cho lãnh địa",
            shortHint = "Tăng diện tích lãnh địa, làm bất cứ lúc nào.",
            prerequisiteQuestIds = { "build_watchtower" },
            rewards =
            {
                new Reward(woodIcon, 800),
                new Reward(goldIcon, 50)
            }
        });

        AddQuest(new Quest
        {
            id = "trade_caravan",
            type = QuestType.Side,
            icon = storageIcon,
            title = "Đoàn thương buôn",
            current = 0,
            target = 1,
            description = "Gửi 1 đoàn thương buôn ra ngoài giao dịch",
            shortHint = "Kiếm thêm tài nguyên phụ, không ảnh hưởng nhiệm vụ chính.",
            prerequisiteQuestIds = { "unlock_cannon" },
            rewards =
            {
                new Reward(goldIcon, 150)
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
        {
            ApplyOptionalSprite(rootImage, panelBackgroundSprite, panelColor);
        }

        Transform header = questPanelRoot.transform.Find("Header");
        if (header != null)
        {
            Image headerImage = header.GetComponent<Image>();
            if (headerImage != null)
            {
                ApplyOptionalSprite(headerImage, headerBackgroundSprite, headerColor);
            }
        }

        if (closeButton != null)
        {
            Image closeImage = closeButton.GetComponent<Image>();
            if (closeImage != null && closeButtonSprite != null)
            {
                closeImage.sprite = closeButtonSprite;
                closeImage.type = Image.Type.Simple;
                closeImage.color = Color.white;
                closeImage.preserveAspect = true;
            }
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
        ApplyOptionalSprite(scrollBg, scrollBackgroundSprite, scrollBgColor);

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

        // ---- NHIỆM VỤ KHẨN CẤP: ép buộc, chiếm toàn bộ panel nếu đang active ----
        Quest activeUrgentQuest = GetActiveUrgentQuest();

        if (activeUrgentQuest != null)
        {
            CreateSection("⚠ NHIỆM VỤ KHẨN CẤP", urgentHeaderColor);
            CreateQuestCard(activeUrgentQuest);
            CreateInfoNotice("Hãy xử lý xong nhiệm vụ khẩn cấp này trước khi tiếp tục các nhiệm vụ khác.");

            if (urgentQuestTakesOverPanel)
            {
                // Ép buộc: chỉ hiển thị DUY NHẤT nhiệm vụ khẩn cấp, ẩn hết Main/Side còn lại.
                LayoutRebuilder.ForceRebuildLayoutImmediate(generatedContent);
                Canvas.ForceUpdateCanvases();

                if (debugMode)
                    Debug.Log("[RoKQuestPanelUI] Render xong (chế độ khẩn cấp). Quest khẩn cấp = " + activeUrgentQuest.id);

                return;
            }
        }

        // ---- NHIỆM VỤ CHÍNH: hiển thị theo giai đoạn (tutorial-style) ----
        List<Quest> visibleMainQuests = GetVisibleMainQuests();

        if (visibleMainQuests.Count > 0)
        {
            CreateSection("◆ Nhiệm vụ chính", mainHeaderColor);

            foreach (Quest quest in visibleMainQuests)
            {
                CreateQuestCard(quest);
            }
        }
        else
        {
            Quest pendingStage = GetActiveMainQuest();

            if (pendingStage != null)
            {
                // Vẫn còn nhiệm vụ chính, nhưng giai đoạn hiện tại đang bị KHOÁ vì
                // chưa đủ điều kiện liên kết (prerequisiteQuestIds chưa hoàn thành).
                CreateSection("◆ Nhiệm vụ chính", mainHeaderColor);
                CreateInfoNotice("🔒 " + BuildLockedRequirementMessage(pendingStage));
            }
            else if (HasAnyMainQuest())
            {
                // Đã hoàn thành hết toàn bộ chuỗi nhiệm vụ chính hiện có
                CreateSection("◆ Nhiệm vụ chính", mainHeaderColor);
                CreateInfoNotice("Bạn đã hoàn thành tất cả nhiệm vụ chính hiện tại!");
            }
        }

        // ---- NHIỆM VỤ PHỤ: luôn hiển thị song song, không theo giai đoạn ----
        // (chỉ hiện những nhiệm vụ phụ đã MỞ KHOÁ theo prerequisiteQuestIds)
        CreateSection("◆ Nhiệm vụ phụ", sideHeaderColor);

        foreach (Quest quest in quests)
        {
            if (quest.type == QuestType.Side && IsQuestUnlocked(quest))
                CreateQuestCard(quest);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(generatedContent);
        Canvas.ForceUpdateCanvases();

        if (debugMode)
            Debug.Log("[RoKQuestPanelUI] Render xong. Quest count = " + quests.Count);
    }

    bool HasAnyMainQuest()
    {
        foreach (Quest quest in quests)
        {
            if (quest.type == QuestType.Main)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra nhiệm vụ đã được MỞ KHOÁ chưa, dựa trên prerequisiteQuestIds.
    /// Một nhiệm vụ chỉ mở khoá khi TẤT CẢ nhiệm vụ điều kiện (Main hoặc Side,
    /// không phân biệt loại) đã được claimed. Không có điều kiện -> luôn mở khoá.
    /// Đây là cơ chế tạo liên kết chặt chẽ giữa các nhiệm vụ với nhau.
    /// </summary>
    bool IsQuestUnlocked(Quest quest)
    {
        if (quest == null)
            return false;

        if (quest.prerequisiteQuestIds == null || quest.prerequisiteQuestIds.Count == 0)
            return true;

        foreach (string prerequisiteId in quest.prerequisiteQuestIds)
        {
            if (string.IsNullOrEmpty(prerequisiteId))
                continue;

            if (!questMap.TryGetValue(prerequisiteId, out Quest prerequisite))
            {
                if (debugMode)
                    Debug.LogWarning($"[RoKQuestPanelUI] Quest '{quest.id}' có prerequisite '{prerequisiteId}' không tồn tại trong danh sách quest.");

                return false;
            }

            if (!prerequisite.claimed)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Dựng chuỗi text liệt kê tên các nhiệm vụ điều kiện CHƯA hoàn thành,
    /// dùng để thông báo cho người chơi biết cần làm gì trước để mở khoá.
    /// </summary>
    string BuildLockedRequirementMessage(Quest quest)
    {
        List<string> missingTitles = new List<string>();

        if (quest.prerequisiteQuestIds != null)
        {
            foreach (string prerequisiteId in quest.prerequisiteQuestIds)
            {
                if (questMap.TryGetValue(prerequisiteId, out Quest prerequisite) && !prerequisite.claimed)
                    missingTitles.Add(prerequisite.title);
            }
        }

        if (missingTitles.Count == 0)
            return "Nhiệm vụ tiếp theo sắp được mở khoá.";

        return "Hoàn thành trước: " + string.Join(", ", missingTitles);
    }

    /// <summary>
    /// Trả về danh sách nhiệm vụ CHÍNH sẽ được hiển thị trong panel.
    /// Nhiệm vụ chính chạy theo giai đoạn: nhiệm vụ chính kế tiếp chỉ xuất hiện
    /// sau khi nhiệm vụ chính trước đó đã được nhận thưởng (claimed), VÀ chỉ khi
    /// nhiệm vụ đó đã được MỞ KHOÁ (đã đủ điều kiện prerequisiteQuestIds — có thể
    /// là nhiệm vụ Main hoặc Side khác). Nếu giai đoạn hiện tại chưa mở khoá,
    /// danh sách trả về rỗng — RenderQuestList() sẽ hiện thông báo yêu cầu thay vào đó.
    /// Thứ tự giai đoạn = thứ tự nhiệm vụ chính được thêm vào trong BuildDefaultQuestList().
    /// </summary>
    List<Quest> GetVisibleMainQuests()
    {
        List<Quest> result = new List<Quest>();

        if (!sequentialMainQuests)
        {
            // Chế độ cũ: hiển thị hết toàn bộ nhiệm vụ chính đã mở khoá cùng lúc.
            foreach (Quest quest in quests)
            {
                if (quest.type == QuestType.Main && IsQuestUnlocked(quest))
                    result.Add(quest);
            }

            return result;
        }

        foreach (Quest quest in quests)
        {
            if (quest.type != QuestType.Main)
                continue;

            if (quest.claimed)
            {
                if (keepClaimedMainQuestsAsHistory)
                    result.Add(quest);

                // Đã xong giai đoạn này, tiếp tục xét giai đoạn kế tiếp.
                continue;
            }

            // Đây là giai đoạn hiện tại theo thứ tự (chưa nhận thưởng).
            // Chỉ hiển thị nếu đã mở khoá; các nhiệm vụ chính phía sau luôn bị ẩn.
            if (IsQuestUnlocked(quest))
                result.Add(quest);

            break;
        }

        return result;
    }

    /// <summary>
    /// Trả về nhiệm vụ CHÍNH đang là giai đoạn hiện tại theo THỨ TỰ khai báo
    /// (nhiệm vụ chính đầu tiên chưa được claimed) — bất kể đã mở khoá hay chưa.
    /// Dùng để biết "đang chờ điều kiện gì" khi giai đoạn hiện tại bị khoá.
    /// Trả về null nếu đã hoàn thành hết toàn bộ chuỗi nhiệm vụ chính.
    /// </summary>
    Quest GetActiveMainQuest()
    {
        foreach (Quest quest in quests)
        {
            if (quest.type != QuestType.Main)
                continue;

            if (!quest.claimed)
                return quest;
        }

        return null;
    }

    /// <summary>
    /// Trả về nhiệm vụ KHẨN CẤP (Urgent) đang active và chưa claimed, nếu có.
    /// Trả về null nếu không có nhiệm vụ khẩn cấp nào đang diễn ra.
    /// </summary>
    Quest GetActiveUrgentQuest()
    {
        foreach (Quest quest in quests)
        {
            if (quest.type == QuestType.Urgent && quest.isActive && !quest.claimed)
                return quest;
        }

        return null;
    }

    /// <summary>
    /// Cổng kiểm soát duy nhất cho việc nhận tiến độ (SetProgress/AddProgress/CompleteQuest).
    /// Áp dụng cho MỌI loại nhiệm vụ (Main, Side, Urgent):
    /// 1) Nhiệm vụ phải được MỞ KHOÁ (đủ prerequisiteQuestIds) — đây là cơ chế liên kết
    ///    chặt chẽ giữa các nhiệm vụ: 1 nhiệm vụ Side có thể khoá 1 nhiệm vụ Main và
    ///    ngược lại, không giới hạn trong cùng 1 loại.
    /// 2) Nhiệm vụ CHÍNH còn phải đúng thứ tự giai đoạn (sequentialMainQuests) và không
    ///    có nhiệm vụ Urgent nào khác đang active (Urgent luôn được ưu tiên xử lý trước).
    /// 3) Nhiệm vụ PHỤ (đã mở khoá) luôn được cập nhật tự do vì làm song song.
    /// 4) Nhiệm vụ Urgent chỉ nhận tiến độ khi đang active.
    /// </summary>
    bool CanReceiveMainQuestProgress(Quest quest)
    {
        if (quest == null)
            return false;

        if (!IsQuestUnlocked(quest))
            return false;

        if (quest.type == QuestType.Urgent)
            return quest.isActive;

        if (quest.type != QuestType.Main)
            return true;

        if (GetActiveUrgentQuest() != null)
            return false;

        if (!sequentialMainQuests)
            return true;

        Quest activeStage = GetActiveMainQuest();

        return activeStage != null && activeStage.id == quest.id;
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

    void CreateInfoNotice(string message)
    {
        GameObject go = new GameObject("InfoNotice", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(generatedContent, false);

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = message;
        text.color = descriptionTextColor;
        text.fontSize = 22;
        text.fontStyle = FontStyles.Italic;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        if (vietnameseFont != null)
        {
            text.font = vietnameseFont;
        }

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.minHeight = 40f;
        le.preferredHeight = 40f;
        le.flexibleWidth = 1;
    }

    void CreateQuestCard(Quest quest)
    {
        GameObject card = new GameObject("QuestItem_" + quest.id, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Outline));
        card.transform.SetParent(generatedContent, false);

        Image bg = card.GetComponent<Image>();

        Sprite cardSprite = null;
        Color fallbackCardColor = sideCardColor;

        if (quest.type == QuestType.Main)
        {
            cardSprite = mainQuestCardSprite;
            fallbackCardColor = mainCardColor;
        }
        else if (quest.type == QuestType.Side)
        {
            cardSprite = sideQuestCardSprite;
            fallbackCardColor = sideCardColor;
        }
        else
        {
            cardSprite = urgentQuestCardSprite;
            fallbackCardColor = urgentCardColor;
        }

        ApplyOptionalSprite(bg, cardSprite, fallbackCardColor);
        bg.raycastTarget = true;

        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = quest.type == QuestType.Urgent ? urgentBorderColor : cardBorderColor;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;
        outline.enabled = cardSprite == null;

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

        Sprite stateSprite = GetQuestButtonSprite(quest);
        ApplyOptionalSprite(img, stateSprite, normalColor);

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = quest.claimed ? new Color32(120, 95, 70, 255) : cardBorderColor;
        outline.effectDistance = quest.IsCompleted() && !quest.claimed ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;
        outline.enabled = stateSprite == null;

        Button btn = go.GetComponent<Button>();
        ColorBlock colors = btn.colors;

        if (stateSprite != null)
        {
            // Dùng nguyên ảnh đã vẽ, không đổi màu khi hover/press.
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.disabledColor = Color.white;
            btn.transition = Selectable.Transition.None;
        }
        else
        {
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightColor;
            colors.selectedColor = highlightColor;
            colors.pressedColor = pressedColor;
            colors.disabledColor = claimedButtonColor;
        }

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

    Sprite GetQuestButtonSprite(Quest quest)
    {
        if (quest == null)
            return null;

        if (quest.claimed)
            return claimedButtonSprite;

        if (quest.IsCompleted())
            return claimButtonSprite;

        return goButtonSprite;
    }

    void ApplyOptionalSprite(Image image, Sprite sprite, Color fallbackColor)
    {
        if (image == null)
            return;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
        else
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = fallbackColor;
        }
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

        if (!string.IsNullOrEmpty(quest.skipsMainQuestId))
            SkipDuplicateMainQuest(quest.skipsMainQuestId);

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

    /// <summary>
    /// Gọi khi có sự kiện game bắt buộc người chơi phải xử lý ngay (vd: làng bị tấn công).
    /// Kích hoạt nhiệm vụ Urgent có id tương ứng -> panel sẽ chỉ hiển thị mỗi nhiệm vụ này
    /// cho tới khi được xử lý xong (theo urgentQuestTakesOverPanel).
    /// </summary>
    public void ActivateUrgentQuest(string questId)
    {
        if (!questMap.ContainsKey(questId))
        {
            Debug.LogWarning("[RoKQuestPanelUI] Không tìm thấy nhiệm vụ khẩn cấp: " + questId);
            return;
        }

        Quest quest = questMap[questId];

        if (quest.type != QuestType.Urgent)
        {
            Debug.LogWarning("[RoKQuestPanelUI] Quest '" + questId + "' không phải QuestType.Urgent, không thể kích hoạt khẩn cấp.");
            return;
        }

        quest.isActive = true;
        quest.claimed = false;

        if (debugMode)
            Debug.Log("[RoKQuestPanelUI] Kích hoạt nhiệm vụ khẩn cấp: " + questId);

        RenderQuestList();
    }

    /// <summary>
    /// Huỷ kích hoạt nhiệm vụ Urgent (vd: đợt tấn công đã bị đẩy lùi bởi hệ thống khác,
    /// không cần bắt người chơi hoàn thành nhiệm vụ khẩn cấp nữa).
    /// </summary>
    public void DeactivateUrgentQuest(string questId)
    {
        if (!questMap.ContainsKey(questId))
            return;

        Quest quest = questMap[questId];

        if (quest.type != QuestType.Urgent)
            return;

        quest.isActive = false;

        if (debugMode)
            Debug.Log("[RoKQuestPanelUI] Huỷ kích hoạt nhiệm vụ khẩn cấp: " + questId);

        RenderQuestList();
    }

    /// <summary>
    /// Đánh dấu hoàn thành ngầm một nhiệm vụ CHÍNH bị trùng hành động với nhiệm vụ Urgent
    /// vừa claim (vd: đã xây Tháp Canh khẩn cấp thì bỏ qua nhiệm vụ chính "Xây Tháp Canh"),
    /// tránh ép người chơi làm lại đúng hành động đó lần 2.
    /// Lưu ý: không cộng thêm phần thưởng riêng của nhiệm vụ bị bỏ qua — nếu muốn người chơi
    /// vẫn nhận thưởng tương đương, hãy cộng thêm phần thưởng đó ngay trong nhiệm vụ Urgent.
    /// </summary>
    void SkipDuplicateMainQuest(string questId)
    {
        if (!questMap.ContainsKey(questId))
            return;

        Quest target = questMap[questId];

        if (target.claimed)
            return;

        target.current = target.target;
        target.claimed = true;

        if (debugMode)
            Debug.Log("[RoKQuestPanelUI] Bỏ qua nhiệm vụ chính trùng lặp '" + questId + "' vì hành động đã được hoàn thành thông qua nhiệm vụ khẩn cấp.");
    }

    public void SetProgress(string questId, int value)
    {
        if (!questMap.ContainsKey(questId))
            return;

        Quest quest = questMap[questId];

        if (!CanReceiveMainQuestProgress(quest))
        {
            if (debugMode)
            {
                Quest activeStage = GetActiveMainQuest();
                string activeId = activeStage != null ? activeStage.id : "(đã xong hết chuỗi chính)";
                Debug.LogWarning($"[RoKQuestPanelUI] Bỏ qua SetProgress cho '{questId}' vì chưa tới lượt (giai đoạn hiện tại: {activeId}). Người chơi phải hoàn thành nhiệm vụ chính trước đó trước.");
            }

            return;
        }

        quest.current = Mathf.Clamp(value, 0, quest.target);
        RenderQuestList();
    }

    public void AddProgress(string questId, int amount)
    {
        if (!questMap.ContainsKey(questId))
            return;

        Quest quest = questMap[questId];

        if (!CanReceiveMainQuestProgress(quest))
        {
            if (debugMode)
            {
                Quest activeStage = GetActiveMainQuest();
                string activeId = activeStage != null ? activeStage.id : "(đã xong hết chuỗi chính)";
                Debug.LogWarning($"[RoKQuestPanelUI] Bỏ qua AddProgress cho '{questId}' vì chưa tới lượt (giai đoạn hiện tại: {activeId}). Người chơi phải hoàn thành nhiệm vụ chính trước đó trước.");
            }

            return;
        }

        quest.current = Mathf.Clamp(quest.current + amount, 0, quest.target);
        RenderQuestList();
    }

    public void CompleteQuest(string questId)
    {
        if (!questMap.ContainsKey(questId))
            return;

        Quest quest = questMap[questId];

        if (!CanReceiveMainQuestProgress(quest))
        {
            if (debugMode)
            {
                Quest activeStage = GetActiveMainQuest();
                string activeId = activeStage != null ? activeStage.id : "(đã xong hết chuỗi chính)";
                Debug.LogWarning($"[RoKQuestPanelUI] Bỏ qua CompleteQuest cho '{questId}' vì chưa tới lượt (giai đoạn hiện tại: {activeId}). Người chơi phải hoàn thành nhiệm vụ chính trước đó trước.");
            }

            return;
        }

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