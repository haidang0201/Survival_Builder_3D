using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/*
He thong:
- Hover doi cursor
- Click chon nhieu object
- Click lai object thi bo chon
- Click ra ngoai thi clear
- Lay WorkerID khi chon
*/

namespace Game.Player
{
    public class CursorController : MonoBehaviour
    {
        public Texture2D defaultCursor;
        public Texture2D interactCursor;

        public LayerMask interactLayer;

        private List<OutlineHighlighter> selectedObjects = new List<OutlineHighlighter>();

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var cam = Camera.main;
            if (cam == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = cam.ScreenPointToRay(mousePos);
            RaycastHit hit;

            // ===== HOVER =====
            if (Physics.Raycast(ray, out hit, 100f, interactLayer))
            {
                if (interactCursor != null)
                    Cursor.SetCursor(interactCursor, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                if (defaultCursor != null)
                    Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
            }

            // ===== CLICK =====
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (Physics.Raycast(ray, out hit, 100f, interactLayer))
                {
                    var outline = hit.collider.GetComponent<OutlineHighlighter>();

                    if (outline != null)
                    {
                        // neu da chon → bo chon
                        if (selectedObjects.Contains(outline))
                        {
                            outline.HideOutline();
                            selectedObjects.Remove(outline);
                        }
                        else
                        {
                            outline.ShowOutline();
                            selectedObjects.Add(outline);

                            // ===== LAY WORKER ID =====
                            var worker = hit.collider.GetComponent<Worker>();
                            if (worker != null)
                            {
                                Debug.Log("Selected WorkerID: " + worker.workerID);
                            }
                        }
                    }
                }
                else
                {
                    ClearAll();
                }
            }
        }

        void ClearAll()
        {
            foreach (var obj in selectedObjects)
            {
                obj.HideOutline();
            }
            selectedObjects.Clear();
        }
    }
}