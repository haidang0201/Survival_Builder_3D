// using UnityEngine;

// public class TaskGoWork : WorkerTask
// {
//     // Tìm công trình WorkStation cho công nhân
//     protected override void FindWork()
//     {
//         BuildingCtrl workStation = BuildingManager.Ins.FindSawmill();  // Tìm công trình làm việc
//         if (workStation != null)
//         {
//             workerCtrl.workerMovement.SetTarget(workStation.transform.position);  // Di chuyển công nhân đến công trình
//         }
//     }

//     // Thực hiện công việc khi công nhân đã đến công trình
//     protected override void PerformWork()
//     {
//         if (workerCtrl.workerMovement.IsAtDestination())  // Kiểm tra công nhân đã đến công trình chưa
//         {
//             isWorking = true;
//             Debug.Log("Worker is working at the work station.");
//             workerCtrl.workerModel.SetActive(true);  // Hiển thị công nhân khi làm việc
//         }
//     }
// }