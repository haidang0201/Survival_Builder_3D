using UnityEngine;

public class UpgradeDebugger : MonoBehaviour
{
    private UpgradeableBuilding upgradeScript;

    void Start()
    {
        upgradeScript = GetComponent<UpgradeableBuilding>();
        if (upgradeScript == null)
        {
            Debug.LogError($"[UpgradeDebugger] Không tìm thấy component UpgradeableBuilding trên GameObject {gameObject.name}!");
        }
    }

    void Update()
    {
        if (upgradeScript == null) return;

        // Nhấn phím U để kích hoạt tiến trình nâng cấp (có đếm ngược thời gian)
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (upgradeScript.IsUpgrading)
            {
                Debug.LogWarning($"[DEBUG] Công trình [{upgradeScript.buildingName}] đang trong quá trình nâng cấp, không thể bấm thêm!");
                return;
            }

            if (upgradeScript.CurrentLevel >= upgradeScript.MaxLevel - 1)
            {
                Debug.LogWarning($"[DEBUG] Công trình [{upgradeScript.buildingName}] đã đạt cấp tối đa ({upgradeScript.MaxLevel})!");
                return;
            }

            // Gọi hàm bắt đầu đếm ngược thời gian nâng cấp
            upgradeScript.StartUpgradeProcess();
            
            // Lấy chi phí để xem thời gian cấu hình là bao nhiêu
            var cost = upgradeScript.GetNextUpgradeCost();
            Debug.Log($"[DEBUG] Bắt đầu nâng cấp công trình [{upgradeScript.buildingName}]. Thời gian chờ: {cost.upgradeDuration} giây.");
        }
    }
}