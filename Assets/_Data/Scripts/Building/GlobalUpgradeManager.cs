// using System.Collections.Generic;
// using UnityEngine;

// public class GlobalUpgradeManager : MonoBehaviour
// {
//     // Singleton để các script khác (chuột, UI) dễ dàng gọi đến từ mọi nơi
//     public static GlobalUpgradeManager Instance { get; private set; }

//     // Từ điển (Dictionary) lưu trữ: 1 Ngôi nhà (GameObject) -> Đang ở Cấp độ mấy (int)
//     private Dictionary<GameObject, int> buildingLevels = new Dictionary<GameObject, int>();

//     private void Awake()
//     {
//         if (Instance == null) Instance = this;
//         else Destroy(gameObject);
//     }

//     /// <summary>
//     /// Ném bất kỳ GameObject nhà nào vào đây, nó sẽ tự động được nâng cấp!
//     /// </summary>
//     public void UpgradeBuilding(GameObject targetBuilding)
//     {
//         // 1. Kiểm tra nhà này đã có trong danh sách chưa. Nếu nhà mới xây, mặc định nó là Cấp 1.
//         if (!buildingLevels.ContainsKey(targetBuilding))
//         {
//             buildingLevels[targetBuilding] = 1;
//         }

//         int currentLevel = buildingLevels[targetBuilding];
//         int nextLevel = currentLevel + 1;

//         // 2. Tự động nội suy tên Model Cấp tiếp theo (Yêu cầu con phải đặt đúng tên "Model_LV2", "Model_LV3"...)
//         Transform nextModel = targetBuilding.transform.Find($"Model_LV{nextLevel}");

//         if (nextModel != null)
//         {
//             // Tắt model cũ
//             Transform oldModel = targetBuilding.transform.Find($"Model_LV{currentLevel}");
//             if (oldModel != null) oldModel.gameObject.SetActive(false);

//             // Bật model mới
//             nextModel.gameObject.SetActive(true);

//             // Lưu lại cấp độ mới vào từ điển
//             buildingLevels[targetBuilding] = nextLevel;
//             Debug.Log($"<color=green>[HỆ THỐNG] Đã nâng cấp {targetBuilding.name} lên Cấp {nextLevel}!</color>");
//         }
//         else
//         {
//             Debug.Log($"<color=yellow>[HỆ THỐNG] {targetBuilding.name} đã đạt cấp tối đa!</color>");
//         }
//     }
// }