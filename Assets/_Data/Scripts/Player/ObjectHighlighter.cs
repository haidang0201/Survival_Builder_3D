using UnityEngine;

/*
Script nay dung de tao hieu ung highlight cho doi tuong.

Chuc nang:
- Luu material ban dau.
- Doi sang material highlight khi duoc hover.
- Tra ve material cu khi khong hover.
*/

namespace Game.Player
{
    public class ObjectHighlighter : MonoBehaviour
    {
        private Material originalMat;
        public Material highlightMat;

        private Renderer rend;

        void Start()
        {
            rend = GetComponent<Renderer>();
            originalMat = rend.material;
        }

        public void Highlight()
        {
            rend.material = highlightMat;
        }

        public void UnHighlight()
        {
            rend.material = originalMat;
        }
    }
}