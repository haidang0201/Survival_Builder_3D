using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn script này vào GameObject cha chứa cả nhóm quái của đợt "Đánh bại đợt cướp
/// đầu tiên" (raid), hoặc vào 1 GameObject quản lý riêng rồi kéo tay quái vào.
///
/// Cách hoạt động:
///   - Tự động lấy tất cả EnemyHealth trong các con của GameObject này lúc Start()
///     (hoặc bạn tự kéo tay vào field "raidEnemies" nếu muốn kiểm soát chính xác).
///   - Lắng nghe sự kiện EnemyHealth.OnEnemyDied mỗi khi có quái chết.
///   - Khi toàn bộ quái trong danh sách đã chết hết -> tự gọi
///     RoKRaidVictoryHandler.OnFirstRaidDefeated() (hoàn thành quest + NPC thông báo).
/// </summary>
public class RoKFirstRaidManager : MonoBehaviour
{
    [Header("QUÁI TRONG ĐỢT RAID NÀY")]
    [Tooltip("Để trống: script tự lấy hết EnemyHealth trong các GameObject con lúc Start(). " +
             "Hoặc kéo tay chính xác từng con quái thuộc đợt raid này vào đây.")]
    public List<EnemyHealth> raidEnemies = new List<EnemyHealth>();

    [Header("LIÊN KẾT (để trống cũng được, script tự tìm lại)")]
    public RoKRaidVictoryHandler victoryHandler;

    [Header("OPTIONS")]
    [Tooltip("Chỉ kích hoạt đúng 1 lần dù có gọi thừa.")]
    public bool onlyTriggerOnce = true;

    bool victoryTriggered = false;

    void Start()
    {
        if (raidEnemies == null || raidEnemies.Count == 0)
        {
            EnemyHealth[] found = GetComponentsInChildren<EnemyHealth>(true);
            raidEnemies = new List<EnemyHealth>(found);

            if (raidEnemies.Count == 0)
                Debug.LogWarning("[RoKFirstRaidManager] Không tìm thấy EnemyHealth nào trong con của " + gameObject.name + ".");
        }
    }

    void OnEnable()
    {
        EnemyHealth.OnEnemyDied += HandleEnemyDied;
    }

    void OnDisable()
    {
        EnemyHealth.OnEnemyDied -= HandleEnemyDied;
    }

    void HandleEnemyDied(EnemyHealth deadEnemy)
    {
        // Chỉ quan tâm nếu con quái vừa chết thuộc đợt raid này
        if (!raidEnemies.Contains(deadEnemy))
            return;

        raidEnemies.Remove(deadEnemy);

        if (raidEnemies.Count == 0)
            TriggerVictory();
    }

    void TriggerVictory()
    {
        if (onlyTriggerOnce && victoryTriggered)
            return;

        victoryTriggered = true;

        RoKRaidVictoryHandler handler = GetVictoryHandler();

        if (handler != null)
            handler.OnFirstRaidDefeated();
        else
            Debug.LogWarning("[RoKFirstRaidManager] Không tìm thấy RoKRaidVictoryHandler để báo chiến thắng.");
    }

    RoKRaidVictoryHandler GetVictoryHandler()
    {
        if (victoryHandler == null)
            victoryHandler = FindObjectOfType<RoKRaidVictoryHandler>();

        return victoryHandler;
    }
}