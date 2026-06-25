using UnityEngine;

public class DayDialogTriggerSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialogPanel;
    public NPCDialogue npc;

    [Header("TIME SYSTEM")]
    public DayNightManager dayManager;

    bool day2Shown;
    bool day3Shown;
    bool day4Shown;

    void Update()
    {
        if (dayManager == null) return;

        int day = dayManager.CurrentDay;

        // ================= DAY 2 =================
        if (day == 2 && !day2Shown)
        {
            day2Shown = true;
            ShowDay2();
        }

        // ================= DAY 3 =================
        if (day == 3 && !day3Shown)
        {
            day3Shown = true;
            ShowDay3();
        }

        // ================= DAY 4 =================
        if (day == 4 && !day4Shown)
        {
            day4Shown = true;
            ShowDay4();
        }
    }

    // 💥 DAY 2 DIALOG
    void ShowDay2()
    {
        dialogPanel.SetActive(true);

        npc.Show("Ngày 2: Địch đang tấn công làng!");
    }

    // 💥 DAY 3 DIALOG
    void ShowDay3()
    {
        dialogPanel.SetActive(true);

        npc.Show("Ngày 3: Địch tấn công mạnh hơn!");
    }

    // 💥 DAY 4 DIALOG
    void ShowDay4()
    {
        dialogPanel.SetActive(true);

        npc.Show("Ngày 4: Phát triển và mở khóa công nghệ!");
    }
}