namespace TopsonGames.Utilities
{
    using UnityEngine;


    public class DynamicIconLayout : MonoBehaviour
    {
        [Header("Layout Settings")]
        [Tooltip("The direction and distance to the next icon.")]
        public Vector3 spacing = new Vector3(1.2f, 0, 0);

        [Tooltip("The starting point relative to this object.")]
        public Vector3 startOffset = Vector3.zero;

        private void LateUpdate()
        {
            UpdateLayout();
        }

        public void UpdateLayout()
        {
            int visibleIconIndex = 0;


            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf)
                {
                    Vector3 newPosition = (visibleIconIndex * spacing) + startOffset;
                    child.localPosition = newPosition;
                    visibleIconIndex++;
                }
            }
        }
    }
}