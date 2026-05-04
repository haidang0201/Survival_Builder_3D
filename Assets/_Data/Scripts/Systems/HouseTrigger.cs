using UnityEngine;

public class HouseTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerPickup player = other.GetComponent<PlayerPickup>();

        if (player != null)
        {
            player.DepositWood();
        }
    }
}