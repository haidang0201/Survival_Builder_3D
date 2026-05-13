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
            Tree tree = hit.collider.GetComponentInParent<Tree>();

            if (tree != null)
            {
                tree.TakeDamage(damage);
                Debug.Log("Chặt cây!");
            }
            else if (hit.collider.CompareTag("Tree"))
            {
                SoundTreeChop chopSound = hit.collider.GetComponentInParent<SoundTreeChop>();
                if (chopSound == null)
                {
                    chopSound = hit.collider.GetComponentInChildren<SoundTreeChop>();
                }

                if (chopSound != null)
                {
                    chopSound.PlayRandomChopSound();
                    Debug.Log("Chặt cây (sound only, chưa có script Tree).");
                }
            }
        }

        yield return new WaitForSeconds(chopCooldown);

        canChop = true;
    }
}