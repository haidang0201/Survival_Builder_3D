using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SimpleCutsceneTutorial : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialogPanel;
    public TextMeshProUGUI dialogText;
    public Button continueButton;

    [Header("WORLD")]
    public Transform enemy;

    [Header("CAMERA FOLLOW SETTINGS")]
    public float followDuration = 3f;   // 🔥 bạn chỉnh thời gian camera follow enemy
    public float followHeight = 5f;
    public float followDistance = -8f;

    bool canContinue = false;

    void Start()
    {
        continueButton.onClick.AddListener(OnContinue);
        continueButton.gameObject.SetActive(false);

        StartCoroutine(RunCutscene());
    }

    IEnumerator RunCutscene()
    {
        // ================= STEP 1 =================
        yield return ShowTextAndWait("⚠ Địch Cung đang tiến đến!");

        // ================= STEP 2 =================
        yield return ShowTextAndWait("⚠ Kho lúa đang bị tấn công!");

        // ================= CAMERA LIA + FOLLOW ENEMY =================
        if (enemy != null)
        {
            yield return StartCoroutine(FollowEnemy(enemy));
        }

        // ================= STEP 3 =================
        yield return ShowTextAndWait("Đây là chuyện bình thường mà, làng chưa có tháp canh nên bị cướp mỗi ngày");

        dialogPanel.SetActive(false);
    }

    // ================= TEXT CONTINUE SYSTEM =================

    IEnumerator ShowTextAndWait(string msg)
    {
        dialogPanel.SetActive(true);
        continueButton.gameObject.SetActive(false);

        dialogText.text = msg;

        canContinue = false;

        continueButton.gameObject.SetActive(true);

        yield return new WaitUntil(() => canContinue);
    }

    // ================= CONTINUE CLICK =================

    void OnContinue()
    {
        canContinue = true;
        continueButton.gameObject.SetActive(false);
    }

    // ================= CAMERA FOLLOW ENEMY =================

    IEnumerator FollowEnemy(Transform target)
    {
        float timer = 0f;

        while (timer < followDuration)
        {
            if (target == null) yield break;

            Vector3 targetPos = target.position + new Vector3(0, followHeight, followDistance);

            Camera.main.transform.position = Vector3.Lerp(
                Camera.main.transform.position,
                targetPos,
                Time.deltaTime * 2f
            );

            Camera.main.transform.LookAt(target);

            timer += Time.deltaTime;

            yield return null;
        }
    }
}