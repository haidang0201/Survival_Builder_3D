using UnityEngine;

/*
Script tao hieu ung vien (outline) cho doi tuong.

Chuc nang:
- Tao ban sao mesh de lam vien
- Scale lon hon de tao hieu ung vien
- Bat / tat vien khi can
*/

namespace Game.Player
{
    public class OutlineHighlighter : MonoBehaviour
    {
        public Material outlineMat;
        public float scaleMultiplier = 1.05f;

        private GameObject outlineObj;

        public void ShowOutline()
        {
            if (outlineObj != null) return;

            // Tao ban sao
            outlineObj = Instantiate(gameObject, transform.position, transform.rotation, transform);

            // Xoa script tren ban sao
            Destroy(outlineObj.GetComponent<OutlineHighlighter>());

            // Xoa collider
            Collider col = outlineObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Set material outline
            Renderer rend = outlineObj.GetComponent<Renderer>();
            rend.material = outlineMat;

            // Scale lon hon
            outlineObj.transform.localScale = transform.localScale * scaleMultiplier;
        }

        public void HideOutline()
        {
            if (outlineObj != null)
            {
                Destroy(outlineObj);
            }
        }
    }
}