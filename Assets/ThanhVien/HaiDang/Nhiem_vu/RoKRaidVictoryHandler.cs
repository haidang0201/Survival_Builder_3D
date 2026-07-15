using UnityEngine;

/// <summary>
/// Gắn script này vào 1 GameObject bất kỳ trong scene (hoặc chung với hệ thống
/// chiến đấu / raid của bạn). Khi trận đánh cướp kết thúc và người chơi THẮNG,
/// gọi hàm public OnFirstRaidDefeated() — script sẽ tự:
///   1. Hoàn thành nhiệm vụ "first_raid" (Đánh bại đợt cướp đầu tiên).
///   2. Tự động nhận thưởng luôn (không cần bấm nút "Nhận" trong bảng nhiệm vụ).
///   3. Hiện hộp thoại NPC thông báo chiến thắng + liệt kê phần thưởng nhận được.
///
/// Giống RoKArcherTrainingUI: các reference (questPanelUI, npcDialogUI) LUÔN tự
/// tìm lại nếu bị null, không bao giờ "mất liên kết" dù kéo tay trong Inspector
/// bị trống hoặc object bị tạo lại giữa chừng.
/// </summary>
public class RoKRaidVictoryHandler : MonoBehaviour
{
    [Header("LIÊN KẾT (để trống cũng được, script tự tìm lại)")]
    public RoKQuestPanelUI questPanelUI;
    public RoKNpcMissionDialogUI npcDialogUI;

    [Header("QUEST")]
    public string raidQuestId = "first_raid";

    [Header("NPC MESSAGE")]
    [TextArea(2, 4)]
    public string npcVictoryMessage = "Chúc mừng thống lĩnh! Chúng ta đã đẩy lùi đợt cướp đầu tiên.";

    [Header("OPTIONS")]
    [Tooltip("Bật để tự động nhận thưởng ngay sau khi thắng, không cần bấm nút Nhận trong bảng nhiệm vụ.")]
    public bool autoClaimReward = true;
    [Tooltip("Bật để hiện luôn dòng liệt kê phần thưởng trong lời thoại NPC (vd: Bạn nhận được: 200 Vàng).")]
    public bool showRewardInDialog = true;

    // =====================================================
    // LIÊN KẾT — LUÔN TỰ TÌM LẠI NẾU BỊ NULL
    // =====================================================

    RoKQuestPanelUI GetQuestPanel()
    {
        if (questPanelUI == null)
            questPanelUI = FindObjectOfType<RoKQuestPanelUI>();

        if (questPanelUI == null)
            Debug.LogWarning("[RoKRaidVictoryHandler] Không tìm thấy RoKQuestPanelUI trong scene.");

        return questPanelUI;
    }

    RoKNpcMissionDialogUI GetNpcDialog()
    {
        if (npcDialogUI == null)
            npcDialogUI = FindObjectOfType<RoKNpcMissionDialogUI>();

        if (npcDialogUI == null)
            Debug.LogWarning("[RoKRaidVictoryHandler] Không tìm thấy RoKNpcMissionDialogUI trong scene.");

        return npcDialogUI;
    }

    // =====================================================
    // PUBLIC API - GỌI TỪ HỆ THỐNG CHIẾN ĐẤU KHI THẮNG TRẬN
    // =====================================================

    /// <summary>
    /// Gọi hàm này ngay khi hệ thống chiến đấu xác nhận người chơi đã đánh bại
    /// đợt cướp đầu tiên. Ví dụ: raidBattleSystem.OnBattleEnd += () => {
    ///     if (playerWon) raidVictoryHandler.OnFirstRaidDefeated();
    /// }
    /// </summary>
    public void OnFirstRaidDefeated()
    {
        RoKQuestPanelUI panel = GetQuestPanel();
        string rewardSummary = "";

        if (panel != null)
        {
            // Lấy tóm tắt thưởng TRƯỚC khi claim (rewards vẫn còn nguyên trong quest data)
            rewardSummary = panel.GetQuestRewardSummary(raidQuestId);

            panel.CompleteQuest(raidQuestId);

            if (autoClaimReward)
                panel.AutoClaimQuestReward(raidQuestId);
        }

        ShowVictoryDialog(rewardSummary);
    }

    void ShowVictoryDialog(string rewardSummary)
    {
        RoKNpcMissionDialogUI dialog = GetNpcDialog();

        if (dialog == null)
            return;

        string message = npcVictoryMessage;

        if (showRewardInDialog && !string.IsNullOrEmpty(rewardSummary))
            message += "\nBạn nhận được: " + rewardSummary;

        StartCoroutine(dialog.ShowAndWait(message));
    }
}