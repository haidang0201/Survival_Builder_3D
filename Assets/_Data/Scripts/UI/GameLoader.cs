using UnityEngine;
using System.Collections;

public class GameLoader : MonoBehaviour
{
    public LoadingUI loadingUI;

    IEnumerator Start()
    {
        loadingUI.Show();

        // load data + update progress
        yield return StartCoroutine(JsonDataManager.Ins.LoadData((progress) =>
        {
            loadingUI.SetProgress(progress);
        }));

        loadingUI.Hide();
    }
}