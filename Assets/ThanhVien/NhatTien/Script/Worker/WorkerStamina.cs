using UnityEngine;

public class WorkerStamina : MonoBehaviour
{
    [HideInInspector] public House      house;
    [HideInInspector] public Kitchen    kitchen;
    [HideInInspector] public Animator   animator;
    [HideInInspector] public UnityEngine.AI.NavMeshAgent agent;
    [HideInInspector] public Transform  handPoint;
    [HideInInspector] public GameObject workerModel;
    [HideInInspector] public GameObject[] extraModelsToHide;

    [HideInInspector] public bool isCarryingResources = false;
    [HideInInspector] public bool isReturnPending     = false;

    public bool CanWork()                => true;
    public void SetDraining(bool active) { }
    public void OnResourcesDeposited()   { }
}