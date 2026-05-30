using UnityEngine;

public class UpgradeDebugger : MonoBehaviour
{
    private UpgradeableBuilding upgradeScript;

    void Start()
    {
        upgradeScript = GetComponent<UpgradeableBuilding>();
    }

    void Update()
    {
        // Nhấn phím U để kiểm tra
        if (Input.GetKeyDown(KeyCode.U))
        {
            upgradeScript.NextLevel();
            Debug.Log($"[DEBUG] Cấp độ hiện tại: {upgradeScript.CurrentLevel + 1}");
        }
    }
}