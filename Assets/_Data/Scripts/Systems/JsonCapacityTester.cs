using UnityEngine;

public class JsonCapacityTester : MonoBehaviour
{
    private int mockWarehouseLevel = 1;

    void Update()
    {
        // Phím P: Kiểm tra trạng thái
        if (Input.GetKeyDown(KeyCode.P))
        {
            PrintStatus();
        }

        // Phím A: Thêm tài nguyên (Test Max Capacity)
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("[Test] Cộng 500 tài nguyên...");
            JsonDataManager.Ins.AddWood(500);
            JsonDataManager.Ins.AddStone(500);
            JsonDataManager.Ins.AddFood(500);
            PrintStatus();
        }

        // Phím U: Nâng cấp kho (Test JSON Config)
        if (Input.GetKeyDown(KeyCode.U))
        {
            mockWarehouseLevel = (mockWarehouseLevel % 3) + 1; // Xoay 1-2-3
            Debug.Log($"[Test] Nâng cấp Kho lên Cấp {mockWarehouseLevel}");
            JsonDataManager.Ins.UpdateCapacities(mockWarehouseLevel);
            PrintStatus();
        }
    }

    private void PrintStatus()
    {
        Debug.Log($"📊 [TRẠNG THÁI KHO]: Wood: {JsonDataManager.Ins.wood}/{JsonDataManager.Ins.maxWood} | " +
                  $"Stone: {JsonDataManager.Ins.stone}/{JsonDataManager.Ins.maxStone} | " +
                  $"Food: {JsonDataManager.Ins.food}/{JsonDataManager.Ins.maxFood}");
    }
}
