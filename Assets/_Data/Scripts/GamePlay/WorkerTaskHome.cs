// using UnityEngine;

// public class WorkerTaskHome : WorkerTask
// {
//     // Tìm tòa nhà Home cho công nhân
//     protected override void FindWork()
//     {
//         BuildingCtrl home = BuildingManager.Ins.FindHouse();  // Tìm tòa nhà Home
//         if (home != null)
//         {
//             workerCtrl.workerMovement.SetTarget(home.transform.position);  // Di chuyển công nhân về nhà
//         }
//     }

//     // Thực hiện công việc khi công nhân vào nhà
//     protected override void PerformWork()
//     {
//         if (workerCtrl.workerMovement.IsAtDestination())  // Kiểm tra công nhân đã đến nhà chưa
//         {
//             isWorking = true;
//             Debug.Log("Worker has returned home.");
//             workerCtrl.GoHome();  // Gọi hàm để ẩn công nhân khi vào nhà
//         }
//     }
// }