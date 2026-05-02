using UnityEngine;

public class Tree : MonoBehaviour
{
    public int health = 3;

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            DestroyTree();
        }
    }

    void DestroyTree()
    {
        Debug.Log("Cây bị đốn!");
        Destroy(gameObject);
    }
}