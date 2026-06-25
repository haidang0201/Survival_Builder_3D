using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyWarningZone : MonoBehaviour
{
    [Header("NPC DIALOG")]
    public NPCDialogue npc;

    [Header("HUD")]
    public GameObject warningPanel;
    public TMPro.TextMeshProUGUI warningText;

    [Header("CONTINUE BUTTON (IMPORTANT)")]
    public Button continueButton;   // 🔥 FIELD MỚI

    [Header("SETTINGS")]
    public string enemyTag = "Enemy";

    bool triggered;
    bool canContinue;

    void Start()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinue);
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag(enemyTag))
        {
            triggered = true;
            StartCoroutine(EnemySequence());
        }
    }

    IEnumerator EnemySequence()
    {
        // ================= HUD =================
        ShowHUD("⚠ Địch đang tấn công Kho Lúa!");

        yield return WaitContinue();

        // ================= DIALOG 1 =================
        npc.Show("Kho lúa đang bị tấn công!");

        yield return WaitContinue();

        // ================= DIALOG 2 =================
        npc.Show("Kho lúa đã bị bọn chúng cướp hết rồi...");

        yield return WaitContinue();

        // ================= DIALOG 3 =================
        npc.Show("Hãy chăm chỉ làm việc để chống lại chúng nhé cậu!");

        yield return WaitContinue();

        // ================= FINAL LINE =================
        npc.Show("Đến ngày tiếp theo, tôi không muốn thấy cậu tệ như vậy");

        yield return WaitContinue();

        HideHUD();
    }

    // ================= CONTINUE CONTROL =================

    void OnContinue()
    {
        canContinue = true;
    }

    IEnumerator WaitContinue()
    {
        canContinue = false;

        // chờ player bấm nút Continue
        yield return new WaitUntil(() => canContinue);
    }

    // ================= HUD =================

    void ShowHUD(string msg)
    {
        if (warningPanel != null)
            warningPanel.SetActive(true);

        if (warningText != null)
            warningText.text = msg;
    }

    void HideHUD()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);
    }
}