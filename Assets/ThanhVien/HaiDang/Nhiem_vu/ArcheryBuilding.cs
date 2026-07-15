// using System.Collections;
// using UnityEngine;

// public class ArcheryBuilding : MonoBehaviour
// {
//     [Header("Cài đặt sinh lính")]
//     public GameObject archerPrefab;       // Prefab của đơn vị cung thủ (chứa sẵn AI tự động tấn công)
//     public Transform spawnPoint;          // Vị trí lính xuất hiện
//     public float trainingTime = 3f;       // Thời gian chờ để huấn luyện 1 lính

//     private bool isTraining = false;

//     // Hàm này được gọi khi người chơi bấm nút "Huấn luyện" trên UI của nhà lính
//     public void StartTrainingArcher()
//     {
//         if (isTraining)
//         {
//             Debug.Log("Nhà lính đang bận huấn luyện, vui lòng chờ!");
//             return;
//         }

//         StartCoroutine(TrainArcherRoutine());
//     }

//     private IEnumerator TrainArcherRoutine()
//     {
//         isTraining = true;
//         Debug.Log("Bắt đầu huấn luyện cung thủ...");

//         // 1. Đợi thời gian huấn luyện (có thể thêm thanh loading UI ở đây)
//         yield return new WaitForSeconds(trainingTime);

//         // 2. Sinh lính ra bản đồ
//         Instantiate(archerPrefab, spawnPoint.position, Quaternion.identity);
//         Debug.Log("Đã sinh ra 1 cung thủ!");

//         // 3. BÁO CÁO NHIỆM VỤ: Tăng 1 điểm cho nhiệm vụ "train_archer"
//         if (RoKQuestPanelUI.Instance != null)
//         {
//             RoKQuestPanelUI.Instance.AddQuestProgress("train_archer", 1);
//         }

//         isTraining = false;

//         // Sau khi sinh ra, lính sẽ tự chạy logic AI của nó (tìm địch, bắn tự động)
//         // Việc huấn luyện đã xong, không liên quan đến quá trình chiến đấu nữa.
//     }
// }