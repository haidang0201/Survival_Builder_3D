using UnityEngine;
using UnityEngine.InputSystem;

/*
Script quan ly cursor va outline.

Chuc nang:
- Hover: doi cursor
- Click: bat outline
- Click ra ngoai: tat outline
*/

namespace Game.Player
{
    public class CursorController : MonoBehaviour
    {
        public Texture2D defaultCursor;
        public Texture2D interactCursor;

        public LayerMask interactLayer;

        private OutlineHighlighter current;

        void Update()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            RaycastHit hit;

            // ===== HOVER =====
            if (Physics.Raycast(ray, out hit, 100f, interactLayer))
            {
                Cursor.SetCursor(interactCursor, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
            }

            // ===== CLICK =====
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (Physics.Raycast(ray, out hit, 100f, interactLayer))
                {
                    var outline = hit.collider.GetComponent<OutlineHighlighter>();

                    if (outline != null)
                    {
                        if (current != outline)
                        {
                            ClearOutline();
                            current = outline;
                            current.ShowOutline();
                        }
                    }
                }
                else
                {
                    ClearOutline();
                }
            }
        }

        void ClearOutline()
        {
            if (current != null)
            {
                current.HideOutline();
                current = null;
            }
        }
    }
}