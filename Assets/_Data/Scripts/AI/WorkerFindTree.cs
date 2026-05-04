using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class WorkerFindTree : MonoBehaviour
{
    public NavMeshAgent agent;
    public WorkerCarryItem carrySystem;

    public float chopDistance = 2f;
    public float chopTime = 2f;

    private Tree targetTree;
    private bool isChopping = false;

    void Update()
    {
        if (carrySystem.IsCarrying()) return;

        if (targetTree == null)
        {
            FindTree();
        }
        else if (!isChopping)
        {
            MoveToTree();
        }
    }

    void FindTree()
    {
        Tree[] trees = GameObject.FindObjectsOfType<Tree>();

        float minDist = Mathf.Infinity;
        Tree bestTree = null;

        foreach (var tree in trees)
        {
            if (!tree.gameObject.activeInHierarchy) continue;

            if (!tree.TryClaim()) continue;

            float dist = Vector3.Distance(transform.position, tree.transform.position);

            if (dist < minDist)
            {
                if (bestTree != null)
                    bestTree.Release();

                minDist = dist;
                bestTree = tree;
            }
            else
            {
                tree.Release();
            }
        }

        if (bestTree != null)
        {
            targetTree = bestTree;

            if (agent.isOnNavMesh)
                agent.SetDestination(targetTree.transform.position);

            Debug.Log(gameObject.name + " chọn cây: " + targetTree.name);
        }
    }

    void MoveToTree()
    {
        if (targetTree == null) return;

        float dist = Vector3.Distance(transform.position, targetTree.transform.position);

        if (dist <= chopDistance)
        {
            StartCoroutine(ChopRoutine());
        }
    }

    IEnumerator ChopRoutine()
    {
        isChopping = true;
        agent.isStopped = true;

        Debug.Log(gameObject.name + " đang chặt...");

        yield return new WaitForSeconds(chopTime);

        if (targetTree != null)
        {
            // 🔥 LẤY GỖ ĐÚNG TỪ CÂY
            WoodPickup[] woods = targetTree.TakeDamage(1);

            if (woods != null && woods.Length > 0)
            {
                carrySystem.PickupWood(woods[0]); // lấy 1 khúc
            }

            targetTree.Release();
            targetTree = null;
        }

        agent.isStopped = false;
        isChopping = false;
    }
}