using UnityEngine;

/*
Outline bang cach doi material (khong clone mesh)

Chuc nang:
- Luu material goc
- Doi sang material outline khi duoc chon
- Co the bat nhieu object cung luc
*/

namespace Game.Player
{
    public class OutlineHighlighter : MonoBehaviour
    {
        public Material outlineMat;

        private Material originalMat;
        private Renderer rend;

        void Awake()
        {
            rend = GetComponent<Renderer>();
            originalMat = rend.material;
        }

        public void ShowOutline()
        {
            if (outlineMat != null)
                rend.material = outlineMat;
        }

        public void HideOutline()
        {
            rend.material = originalMat;
        }
    }
}