using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StoryDayAutoSystem : MonoBehaviour
{
    [Header("DAY SYSTEM")]
    public DayNightManager dayManager;

    [Header("UI STORY PANEL")]
    public GameObject storyPanel;
    public TMP_Text speakerName;
    public TMP_Text dialogueText;

    [Header("CONTINUE BUTTON")]
    public Button continueButton;   // 🔥 FIELD MỚI

    bool day2Shown;
    bool day3Shown;
    bool day4Shown;

    bool canContinue;

    void Start()
    {
        storyPanel.SetActive(false);

        continueButton.onClick.AddListener(OnContinueClicked);

        StartCoroutine(CheckDayFlow());
    }

    IEnumerator CheckDayFlow()
    {
        while (true)
        {
            int day = dayManager.CurrentDay;

            // ================= DAY 2 =================
            if (day == 2 && !day2Shown)
            {
                day2Shown = true;
                yield return ShowStory("Ông Phó Lý",
                    "Ngày 2: Địch đang tấn công làng!\nKho lúa bị đe dọa, hãy xây pháo thủ để phòng thủ!");
            }

            // ================= DAY 3 =================
            if (day == 3 && !day3Shown)
            {
                day3Shown = true;
                yield return ShowStory("Ông Phó Lý",
                    "Ngày 3: Địch tấn công mạnh hơn!\nCần 15 gỗ và 10 đá để mở tháp canh!");
            }

            // ================= DAY 4 =================
            if (day == 4 && !day4Shown)
            {
                day4Shown = true;
                yield return ShowStory("Ông Phó Lý",
                    "Ngày 4: Dân làng làm việc chăm chỉ!\nTích lũy tài nguyên và mở khóa công nghệ Tháp Canh!");
            }

            yield return null;
        }
    }

    IEnumerator ShowStory(string speaker, string text)
    {
        storyPanel.SetActive(true);

        speakerName.text = speaker;
        dialogueText.text = text;

        continueButton.gameObject.SetActive(true);

        canContinue = false;

        yield return new WaitUntil(() => canContinue);

        continueButton.gameObject.SetActive(false);

        storyPanel.SetActive(false);
    }

    void OnContinueClicked()
    {
        canContinue = true;
    }
}