using UnityEngine;
using Game.Player;

public class PlayerPickup : MonoBehaviour
{
    public PlayerInputHandler input;
    public Transform handPoint;

    private WoodPickup currentWood;

    void Update()
    {
        if (input.Interact)
        {
            // 👉 CHƯA CẦM → NHẶT
            if (currentWood == null)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 2f);

                foreach (var hit in hits)
                {
                    WoodPickup wood = hit.GetComponent<WoodPickup>();

                    if (wood != null)
                    {
                        currentWood = wood;
                        wood.Pickup(handPoint);
                        break;
                    }
                }
            }
            // 👉 ĐANG CẦM → THẢ
            else
            {
                currentWood.Drop();
                currentWood = null;
            }
        }
    }

    // 🔥 Gọi khi vào nhà
    public void DepositWood()
    {
        if (currentWood != null)
        {
            Debug.Log("Đã nộp gỗ vào nhà!");

            ObjectPool pool = currentWood.pool;

            if (pool != null)
                pool.ReturnObject(currentWood.gameObject);
            else
                currentWood.gameObject.SetActive(false);

            currentWood = null;
        }
    }
}