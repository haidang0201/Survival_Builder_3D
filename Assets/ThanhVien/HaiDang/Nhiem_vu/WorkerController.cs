using UnityEngine;
using System.Collections;

public class WorkerController : MonoBehaviour
{
    public bool IsWorking { get; private set; }

    public void MoveTo(Transform target)
    {
        StartCoroutine(Move(target));
    }

    IEnumerator Move(Transform target)
    {
        IsWorking = false;

        while (Vector3.Distance(transform.position, target.position) > 0.5f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                Time.deltaTime * 3f
            );

            yield return null;
        }

        IsWorking = true;
    }
}