using System.Collections;
using UnityEngine;

public class WatchTowerTutorialNotifier : MonoBehaviour
{
    [Header("OPTIONS")]
    public bool notifyOnStart = true;
    public float delay = 0.15f;

    IEnumerator Start()
    {
        if (!notifyOnStart)
            yield break;

        yield return new WaitForSeconds(delay);

        StartupTwoMissionTutorial tutorial = StartupTwoMissionTutorial.Instance;

        if (tutorial == null)
            tutorial = FindObjectOfType<StartupTwoMissionTutorial>();

        if (tutorial == null)
            yield break;

        if (tutorial.IsWaitingForWatchTowerPlacement)
        {
            tutorial.NotifyWatchTowerPlaced();
            Debug.Log("[WatchTowerTutorialNotifier] Đã báo tutorial: Tháp Canh thật đã được đặt.");
        }
    }
}