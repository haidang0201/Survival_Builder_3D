using UnityEngine;
using UnityEngine.InputSystem;

/*
Script nay dung de xu ly hanh dong tuong tac khi bam phim E.

Chuc nang:
- Nhan input tu ban phim.
- Ban raycast tu camera.
- Xac dinh loai doi tuong (Tree / House).
- Goi ham Interact tren doi tuong.
*/

public class MouseInteraction : MonoBehaviour
{
    public LayerMask interactLayer;

    void Update()
    {
        // Kiem tra phim E duoc bam
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, interactLayer))
            {
                string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);

                // Phan loai doi tuong
                if (layerName == "Tree")
                    Debug.Log("Chat cay");
                else if (layerName == "House")
                    Debug.Log("Vao nha");

                // Goi ham tuong tac
                hit.collider.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}