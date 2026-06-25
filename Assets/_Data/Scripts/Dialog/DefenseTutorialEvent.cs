using System.Collections;
using UnityEngine;
using TMPro;

public class DefenseTutorialEvent : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelDialog;
    public TextMeshProUGUI dialogText;

    [Header("WORLD")]
    public Transform enemySpawnPoint;

    [Header("TOWER")]
    public Transform tower;
    public float attackRange = 5f;
    public float attackDamage = 1f;

    [Header("ENEMY")]
    public GameObject enemyPrefab;
    public string enemyTag = "Enemy";

    GameObject currentEnemy;

    void Start()
    {
        StartCoroutine(RunTutorial());
    }

    void Update()
    {
        AutoAttackEnemy();
    }

    // ================= MAIN FLOW =================

    IEnumerator RunTutorial()
    {
        Show("⚠ Địch đang tiến đến làng!");

        yield return new WaitForSeconds(1f);

        Show("👉 Hãy xây Pháo để phòng thủ!");

        yield return new WaitForSeconds(2f);

        Show("⚠ Đặt pháo vào vị trí phòng thủ!");

        yield return new WaitForSeconds(2f);

        // ================= SPAWN ENEMY =================
        SpawnEnemy();

        Show("⚠ Địch xuất hiện!");

        yield return new WaitForSeconds(1f);

        Show("🔥 Pháo đang tấn công!");

        yield return new WaitForSeconds(2f);

        Show("✔ Địch bị đánh bại!");

        yield return new WaitForSeconds(2f);

        Hide();
    }

    // ================= ENEMY SPAWN =================

    void SpawnEnemy()
    {
        currentEnemy = Instantiate(enemyPrefab, enemySpawnPoint.position, Quaternion.identity);
        currentEnemy.tag = enemyTag;
    }

    // ================= AUTO ATTACK =================

    void AutoAttackEnemy()
    {
        GameObject enemy = GameObject.FindGameObjectWithTag(enemyTag);

        if (enemy == null) return;

        float distance = Vector3.Distance(tower.position, enemy.transform.position);

        if (distance <= attackRange)
        {
            Attack(enemy);
        }
    }

    void Attack(GameObject enemy)
    {
        Destroy(enemy);
    }

    // ================= UI =================

    void Show(string msg)
    {
        panelDialog.SetActive(true);
        dialogText.text = msg;
    }

    void Hide()
    {
        panelDialog.SetActive(false);
    }
}