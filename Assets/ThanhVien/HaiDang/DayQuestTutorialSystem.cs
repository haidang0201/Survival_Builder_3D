using System.Collections;
using UnityEngine;

public class DayQuestTutorialSystem : MonoBehaviour
{
    [Header("DIALOG SYSTEM")]
    public NPCDialogue npc;

    [Header("TIME MANAGER (READ ONLY)")]
    public DayNightManager timeManager;

    bool day2Done;
    bool day3Done;
    bool day4Done;

    void Start()
    {
        StartCoroutine(CheckDayFlow());
    }

    IEnumerator CheckDayFlow()
    {
        // 🔥 wait system ready
        yield return new WaitForSeconds(1f);

        while (true)
        {
            if (timeManager == null)
                yield break;

            int currentDay = timeManager.CurrentDay;

            // ================= DAY 2 =================
            if (currentDay == 2 && !day2Done)
            {
                day2Done = true;
                StartCoroutine(Day2Quest());
            }

            // ================= DAY 3 =================
            if (currentDay == 3 && !day3Done)
            {
                day3Done = true;
                StartCoroutine(Day3Quest());
            }

            // ================= DAY 4 =================
            if (currentDay == 4 && !day4Done)
            {
                day4Done = true;
                StartCoroutine(Day4Quest());
            }

            yield return new WaitForSeconds(1f);
        }
    }

    // 💥 DAY 2 QUEST
    IEnumerator Day2Quest()
    {
        yield return npc.ShowAndWait("Ngày 2: Địch đang tấn công làng!");

        yield return npc.ShowAndWait("Kho lúa bị đe dọa!");

        yield return npc.ShowAndWait("Hãy xây tháp canh để phòng thủ!");
    }

    // 💥 DAY 3 QUEST
    IEnumerator Day3Quest()
    {
        yield return npc.ShowAndWait("Ngày 3: Địch tấn công mạnh hơn!");

        yield return npc.ShowAndWait("Cần 15 gỗ và 10 đá để mở tháp canh!");

        yield return npc.ShowAndWait("Nếu không xây, làng sẽ bị phá!");
    }

    // 💥 DAY 4 QUEST
    IEnumerator Day4Quest()
    {
        yield return npc.ShowAndWait("Ngày 4: Dân làng làm việc chăm chỉ!");

        yield return npc.ShowAndWait("Tích lũy tài nguyên 40 gỗ + 60 đá");

        yield return npc.ShowAndWait("Nâng cấp nhà Worker và mở công nghệ Tháp Canh!");
    }
}