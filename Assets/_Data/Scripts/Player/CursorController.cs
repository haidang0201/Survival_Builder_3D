using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /*
    Script nay dung de dieu khien con tro chuot trong game.

    Chuc nang:
    - Lay vi tri chuot.
    - Ban raycast tu camera.
    - Neu trung doi tuong co the tuong tac thi doi cursor.
    - Dong thoi highlight doi tuong.
    */

    public class CursorController : MonoBehaviour
    {
        public Texture2D defaultCursor;
        public Texture2D interactCursor;

        public LayerMask interactLayer;

        private ObjectHighlighter current;

        void Update()
        {
            // Lay vi tri chuot tu Input System moi
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // Ban ray tu camera theo vi tri chuot
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, interactLayer))
            {
                // Doi icon cursor khi hover vao object
                Cursor.SetCursor(interactCursor, Vector2.zero, CursorMode.Auto);

                var highlighter = hit.collider.GetComponent<ObjectHighlighter>();

                if (highlighter != null)
                {
                    if (current != highlighter)
                    {
                        ClearHighlight();
                        current = highlighter;
                        current.Highlight();
                    }
                }
            }
            else
            {
                // Tra ve cursor mac dinh
                Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
                ClearHighlight();
            }
        }

        void ClearHighlight()
        {
            if (current != null)
            {
                current.UnHighlight();
                current = null;
            }
        }
    }
}