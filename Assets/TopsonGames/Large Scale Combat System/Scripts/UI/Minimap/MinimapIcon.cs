namespace TopsonGames.Minimap
{
    using UnityEngine;
    using UnityEngine.UI;

    public class MinimapIcon : MonoBehaviour
    {
        [HideInInspector]
        public IMinimapTrackable trackedObject;
        public RectTransform rectTransform;
        public Image iconImage;
    }
}
