using UnityEngine;

/*
Script dai dien cho doi tuong co the tuong tac.

Chuc nang:
- Chua ham Interact.
- Duoc goi khi nguoi choi bam phim tuong tac.
*/

namespace Game.Systems
{
    public class Interactable : MonoBehaviour
    {
        public void Interact()
        {
            Debug.Log("Da tuong tac voi " + gameObject.name);
        }
    }
}