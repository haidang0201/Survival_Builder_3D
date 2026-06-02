using System.Collections;
using UnityEngine;

public class AutoReleaseToPool : MonoBehaviour
{
    private Coroutine releaseRoutine;

    public void PlayAndRelease(float delay)
    {
        if (releaseRoutine != null)
            StopCoroutine(releaseRoutine);

        gameObject.SetActive(true);
        releaseRoutine = StartCoroutine(ReleaseAfterDelay(delay));
    }

    private IEnumerator ReleaseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ArrowPool.Instance != null && GetComponent<PooledItem>() != null)
            ArrowPool.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (releaseRoutine != null)
        {
            StopCoroutine(releaseRoutine);
            releaseRoutine = null;
        }
    }
}