using System.Collections;
using UnityEngine;

public class GameTutorialController : MonoBehaviour
{
    public Transform towerFocus;

    void OnEnable()
    {
        GameEventSystem.Instance.OnEnemySpawn += HandleEnemy;
    }

    void OnDisable()
    {
        GameEventSystem.Instance.OnEnemySpawn -= HandleEnemy;
    }

    void HandleEnemy(Transform enemy)
    {
        StartCoroutine(EnemySequence(enemy));
    }

    IEnumerator EnemySequence(Transform enemy)
    {
        // ================= WARNING =================
        WarningUI.Instance.Show("⚠ Địch đang tấn công!");

        yield return new WaitForSeconds(0.5f);

        // ================= CAMERA LIA ENEMY =================
        Camera.main.transform.position = enemy.position + new Vector3(0, 5, -8);
        Camera.main.transform.LookAt(enemy);

        yield return new WaitForSeconds(0.3f);

        // ================= SHAKE =================
        CameraShake.Instance.Shake();

        yield return new WaitForSeconds(1f);

        WarningUI.Instance.Show("Kho lúa bị tấn công!");

        yield return new WaitForSeconds(2f);

        WarningUI.Instance.Hide();
    }
}