// using UnityEngine;

// public class Day8EventHandler : MonoBehaviour
// {
//     [Header("DayNight Manager")]
//     public DayNightManager dayNightManager;

//     [Header("Tutorial Manager Day 8")]
//     public TutorialManagerDay8 tutorialDay8;

//     private int lastDay = 0;

//     void Start()
//     {
//         if (dayNightManager == null || tutorialDay8 == null)
//         {
//             Debug.LogWarning("[Day8EventHandler] Chưa gán DayNightManager hoặc TutorialManagerDay8");
//             enabled = false;
//             return;
//         }

//         // Subscribe event OnDayStart
//         dayNightManager.OnDayStart += OnDayChanged;
//     }

//     void OnDestroy()
//     {
//         if (dayNightManager != null)
//             dayNightManager.OnDayStart -= OnDayChanged;
//     }

//     private void OnDayChanged()
//     {
//         int currentDay = dayNightManager.CurrentDay;

//         // Chỉ trigger khi ngày mới = 8
//         if (currentDay == 8 && currentDay != lastDay)
//         {
//             lastDay = currentDay;

//             // Gọi tutorial day 8
//             tutorialDay8.StartTutorialDay8();
//         }
//     }
// }