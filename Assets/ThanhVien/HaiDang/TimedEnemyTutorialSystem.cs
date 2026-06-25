using System.Collections;
using UnityEngine;
using TMPro;

public class TimedEnemyTutorialSystem : MonoBehaviour
{
    [Header("UI TIMER")]
    public TextMeshProUGUI timerText;

    [Header("UI DIALOG")]
    public GameObject dialogPanel;
    public TextMeshProUGUI dialogText;

    [Header("ENEMY")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public Transform enemyDetectPoint;

    [Header("TIMER SETTINGS")]
    public float countdown = 30f;

    bool canSpawnEnemy;
    bool enemySpawned;

    GameObject currentEnemy;

    void Start()
    {
        dialogPanel.SetActive(false);
        StartCoroutine(SpawnEnemyFlow());
    }

    void Update()
    {
        UpdateTimer();
    }

    // ================= TIMER =================

    void UpdateTimer()
    {
        if (countdown > 0)
        {
            countdown -= Time.deltaTime;

            timerText.text = "00:" + Mathf.CeilToInt(countdown).ToString("00");

            if (countdown <= 0 && !canSpawnEnemy)
            {
                canSpawnEnemy = true;
                StartCoroutine(SpawnEnemyFlow());
            }
        }
    }

    // ================= ENEMY FLOW =================

    IEnumerator SpawnEnemyFlow()
    {
        // ================= SPAWN ENEMY =================
        currentEnemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        yield return new WaitForSeconds(0.5f);

        // ================= WARNING TEXT =================
        ShowDialog("⚠ ĐỊCH XUẤT HIỆN!");

        yield return new WaitForSeconds(1.5f);

        // ================= CAMERA MOVE =================
        if (currentEnemy != null)
        {
            Camera.main.transform.position =
                currentEnemy.transform.position + new Vector3(0, 5, -8);

            Camera.main.transform.LookAt(currentEnemy.transform);
        }

        yield return new WaitForSeconds(1f);

        // ================= TUTORIAL DIALOG =================
        yield return ShowDialogStep("Kho lúa đang bị tấn công!");

        yield return ShowDialogStep("Hãy xây tháp canh để phòng thủ!");

        HideDialog();
    }

    // ================= DIALOG SYSTEM =================

    IEnumerator ShowDialogStep(string msg)
    {
        dialogPanel.SetActive(true);
        dialogText.text = msg;

        yield return new WaitForSeconds(2f);
    }

    void ShowDialog(string msg)
    {
        dialogPanel.SetActive(true);
        dialogText.text = msg;
    }

    void HideDialog()
    {
        dialogPanel.SetActive(false);
    }
}