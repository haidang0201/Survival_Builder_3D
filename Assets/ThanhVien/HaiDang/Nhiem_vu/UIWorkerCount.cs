using UnityEngine;

public class UIWorkerCount : MonoBehaviour
{
    public static UIWorkerCount Instance;

    [Header("Tag Worker")]
    public string workerTag = "Worker";

    public int workerCount;


    void Awake()
    {
        Instance = this;
    }


    void Update()
    {
        workerCount = CountWorkers();
    }


    public int GetWorkerCount()
    {
        return workerCount;
    }


    public int CountWorkers()
    {
        GameObject[] workers =
            GameObject.FindGameObjectsWithTag(workerTag);

        return workers.Length;
    }
}