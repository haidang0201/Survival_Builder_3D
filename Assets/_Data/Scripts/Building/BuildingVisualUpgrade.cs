// using UnityEngine;

// public class BuildingVisualUpgrade : MonoBehaviour
// {
//     [Header("Cấu hình Cấp độ")]
//     public int currentLevel = 1;
//     public int maxLevel = 3;

//     [Header("Kéo thả Model vào đây")]
//     [Tooltip("Ô 0 = Cấp 1, Ô 1 = Cấp 2, Ô 2 = Cấp 3")]
//     public GameObject[] levelModels;

//     private void Start()
//     {
//         // Khi nhà đẻ ra, tự động lật đúng model hiện tại
//         RefreshModel();
//     }

//     // Hàm này gắn vào Nút UI Nâng Cấp
//     public void LevelUp()
//     {
//         if (currentLevel < maxLevel)
//         {
//             currentLevel++;
//             RefreshModel();
//             Debug.Log($"<color=cyan>Đã nâng cấp lên Level {currentLevel}</color>");
//         }
//         else
//         {
//             Debug.Log("<color=yellow>Nhà đã max level!</color>");
//         }
//     }

//     private void RefreshModel()
//     {
//         // Tắt hết tất cả model đi
//         for (int i = 0; i < levelModels.Length; i++)
//         {
//             if (levelModels[i] != null)
//             {
//                 levelModels[i].SetActive(false);
//             }
//         }

//         // Chỉ bật đúng model của cấp hiện tại (Mảng bắt đầu từ 0 nên level 1 -> index 0)
//         int indexToTurnOn = currentLevel - 1;
//         if (indexToTurnOn >= 0 && indexToTurnOn < levelModels.Length)
//         {
//             if (levelModels[indexToTurnOn] != null)
//             {
//                 levelModels[indexToTurnOn].SetActive(true);
//             }
//         }
//     }
// }