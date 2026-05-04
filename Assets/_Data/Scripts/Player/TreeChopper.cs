using UnityEngine;
using System.Collections;
using Game.Player;

public class TreeChopper : MonoBehaviour
{
    public float chopCooldown = 1f;
    public int damage = 1;

    public PlayerInputHandler inputHandler; // 🔥 lấy input từ đây

    private bool canChop = true;

    void Update()
    {
        if (inputHandler.Chop && canChop)
        {
            StartCoroutine(ChopRoutine());
        }
    }

    IEnumerator ChopRoutine()
    {
        canChop = false;

        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 2f))
        {
            Tree tree = hit.collider.GetComponent<Tree>();

            if (tree != null)
            {
                tree.TakeDamage(damage);
                Debug.Log("Chặt cây!");
            }
        }

        yield return new WaitForSeconds(chopCooldown);

        canChop = true;
    }
}