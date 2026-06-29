using System.Collections;
using UnityEngine;

public class TutorialDay8Standalone : MonoBehaviour
{
    [Header("NPC / Dialogue")]
    public NPCDialogue npc;

    [Header("Highlight UI")]
    public UIHighlightSystem highlight;

    [Header("World Transforms")]
    public Transform towerDefenseTransform;    // vị trí tháp trên map
    public Transform enemyTarget;              // vị trí địch

    [Header("Camera Settings")]
    public float cameraMoveTime = 2f;

    [Header("DayNightManager")]
    public DayNightManager dayNightManager;    // liên kết với DayNightManager

    private bool tutorialTriggered = false;

    void Start()
    {
        // Subscribe event OnDayStart
        if (dayNightManager != null)
            dayNightManager.OnDayStart += CheckDay;
    }

    void OnDestroy()
    {
        if (dayNightManager != null)
            dayNightManager.OnDayStart -= CheckDay;
    }

    private void CheckDay()
    {
        if (dayNightManager.CurrentDay == 8 && !tutorialTriggered)
        {
            tutorialTriggered = true;
            StartTutorialDay8();
        }
    }

    public void StartTutorialDay8()
    {
        StartCoroutine(RunDay8());
    }

    private IEnumerator RunDay8()
    {
        // STEP 1: Giới thiệu đợt tấn công
        yield return npc.ShowAndWait("Ngày 8: Địch Phi Lao và Bắn Cung mở đợt tấn công lớn!");

        // STEP 2: Highlight tháp phòng thủ
        if (towerDefenseTransform != null)
            highlight.Highlight(towerDefenseTransform.GetComponent<RectTransform>());
        yield return npc.ShowAndWait("Tháp Canh quét vị trí địch!");
        highlight.ClearAll();

        // STEP 3: Camera lia đến enemy
        if (enemyTarget != null)
        {
            Camera mainCam = Camera.main;
            Vector3 startPos = mainCam.transform.position;
            Vector3 endPos = enemyTarget.position + new Vector3(0, 10, -10);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / cameraMoveTime;
                mainCam.transform.position = Vector3.Lerp(startPos, endPos, t);
                mainCam.transform.LookAt(enemyTarget);
                yield return null;
            }
        }

        // STEP 4: NPC hướng dẫn ra lệnh cho tháp + huấn luyện lính
        yield return npc.ShowAndWait("Ra lệnh cho tháp phòng thủ và đội Lính Ta ra chống trả.");

        // STEP 5: Kết thúc
        highlight.ClearAll();
        yield return npc.ShowAndWait("Thắng! Làng đã được bảo vệ thành công.");
    }
}