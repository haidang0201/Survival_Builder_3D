// using System;
// using UnityEngine;

// /// <summary>
// /// Quản lý số lượng Worker đã có (tuyển tay hoặc được thưởng).
// /// Gắn vào GameObject "WorkerManager" (singleton).
// ///
// /// Có 2 nguồn cộng worker, đều cộng thẳng vào WorkerCount và bắn chung event OnWorkerHired,
// /// nhưng tách hàm riêng để dễ thêm hiệu ứng/log khác nhau cho từng nguồn sau này:
// ///   - HireWorker()       : người chơi bấm nút "Tuyển" trên popup, +1 mỗi lần bấm
// ///   - GrantFreeWorkers()  : hệ thống tự thưởng (vd: "dân di cư" kéo tới khi đạt mốc tutorial)
// /// </summary>
// public class WorkerManager : MonoBehaviour
// {
//     public static WorkerManager Instance;

//     [Tooltip("Số worker tối đa có thể có (tuỳ thiết kế, để 0 = không giới hạn)")]
//     public int maxWorkers = 0;

//     public int WorkerCount { get; private set; } = 0;

//     /// <summary>Bắn ra tổng số worker hiện có mỗi khi có thêm worker (tuyển HOẶC được thưởng).</summary>
//     public event Action<int> OnWorkerHired;

//     /// <summary>Bắn riêng khi worker đến từ "dân di cư" / phần thưởng, kèm số lượng vừa được tặng.
//     /// UI có thể lắng nghe để show hiệu ứng "+2 Worker!" khác với tuyển tay.</summary>
//     public event Action<int> OnFreeWorkersGranted;

//     void Awake()
//     {
//         Instance = this;
//     }

//     /// <summary>Gắn hàm này vào OnClick của nút "Tuyển" trên Worker Popup. Mỗi lần bấm = +1 worker.</summary>
//     public void HireWorker()
//     {
//         AddWorkers(1);
//     }

//     /// <summary>
//     /// Tặng thêm worker miễn phí, không qua tuyển tay — dùng khi "dân di cư" tới làng
//     /// (vd: phần thưởng khi đạt mốc tutorial 20 Gỗ).
//     /// </summary>
//     public void GrantFreeWorkers(int amount)
//     {
//         if (amount <= 0) return;
//         AddWorkers(amount);
//         OnFreeWorkersGranted?.Invoke(amount);
//     }

//     private void AddWorkers(int amount)
//     {
//         if (maxWorkers > 0)
//             amount = Mathf.Min(amount, Mathf.Max(0, maxWorkers - WorkerCount));
//         if (amount <= 0) return;

//         WorkerCount += amount;
//         OnWorkerHired?.Invoke(WorkerCount);
//     }
// }