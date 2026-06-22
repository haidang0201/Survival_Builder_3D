using UnityEngine;

/// <summary>
/// Quản lý world: mở khoá mỏ đá, kích hoạt enemy.
/// Gắn vào GameObject "WorldManager".
/// </summary>
public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance;

    [Header("World Objects")]
    [Tooltip("Mỏ đá trong scene – ban đầu SetActive = false")]
    public GameObject stoneMine;

    [Header("Enemy UI")]
    [Tooltip("UI counter kẻ thù trên Top HUD – ban đầu SetActive = false")]
    public GameObject enemyCounterUI;

    void Awake() { Instance = this; }

    /// <summary>Tutorial Step 3 gọi hàm này.</summary>
    public void UnlockStoneMine()
    {
        if (stoneMine != null) stoneMine.SetActive(true);
        ResourceMa.Instance.UnlockStone();
    }

    /// <summary>Tutorial Step 6 gọi hàm này.</summary>
    public void TriggerEnemyWarning()
    {
        if (enemyCounterUI != null) enemyCounterUI.SetActive(true);
        // TODO: bắt đầu wave kẻ thù thật ở đây
    }
}