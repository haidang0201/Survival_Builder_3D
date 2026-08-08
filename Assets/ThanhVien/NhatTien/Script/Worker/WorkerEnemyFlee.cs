using UnityEngine;

public class WorkerEnemyFlee : MonoBehaviour
{
    [HideInInspector] public House      house;
    [HideInInspector] public Animator   animator;
    [HideInInspector] public GameObject workerModel;
    [HideInInspector] public GameObject[] extraModelsToHide;
}