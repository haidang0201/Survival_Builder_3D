using UnityEngine;

public class WorkerCarryItem : MonoBehaviour
{
    [HideInInspector] public UnityEngine.Transform handPoint;
    [HideInInspector] public UnityEngine.AI.NavMeshAgent agent;

    public bool IsCarrying()            => false;
    public bool TryDeposit()            => false;
    public bool MoveToStorage()         => false;
    public void PickUpFakeItemForLoad() { }
}