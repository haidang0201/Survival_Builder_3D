using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TopsonGames.Minimap
{
    public class MinimapController : MonoBehaviour
    {
        [Header("Map Boundaries")]
        public Transform worldCorner1;
        public Transform worldCorner2;

        [Header("Setup")]
        public MinimapIcon iconPrefab;
        public RectTransform iconContainer;
        public RectTransform cameraIcon;
        public float iconEdgeOffset = 10f;

        [Header("Colors")]
        public Color playerColor = Color.blue;
        public Color enemyColor = Color.red;
        public Color neutralColor = Color.white; 

        public static MinimapController instance;

        private RectTransform minimapRect;
        private Dictionary<IMinimapTrackable, MinimapIcon> iconInstances = new Dictionary<IMinimapTrackable, MinimapIcon>();
        private Rect worldBounds;
        private Camera mainCamera;

        void Awake()
        {
            if (instance != null && instance != this) Destroy(gameObject);
            else instance = this;

            mainCamera = Camera.main;
            minimapRect = GetComponent<RectTransform>();

            if (worldCorner1 != null && worldCorner2 != null)
            {
                worldBounds = Rect.MinMaxRect(
                    Mathf.Min(worldCorner1.position.x, worldCorner2.position.x),
                    Mathf.Min(worldCorner1.position.z, worldCorner2.position.z),
                    Mathf.Max(worldCorner1.position.x, worldCorner2.position.x),
                    Mathf.Max(worldCorner1.position.z, worldCorner2.position.z)
                );
            }
        }

        void LateUpdate()
        {
            if (worldCorner1 == null || worldCorner2 == null) return;
            var keys = iconInstances.Keys.ToList();
            foreach (var trackable in keys) UpdateIcon(trackable, iconInstances[trackable]);
            UpdateCameraIcon();
        }

        public void RegisterTrackable(IMinimapTrackable trackable)
        {
            if (trackable == null || iconInstances.ContainsKey(trackable)) return;
            if (minimapRect == null) Awake();

            MinimapIcon newIcon = Instantiate(iconPrefab, iconContainer);
            newIcon.trackedObject = trackable;

            if (newIcon.iconImage != null)
            {
                Sprite customSprite = trackable.GetMinimapIcon();
                if (customSprite != null)
                {
                    newIcon.iconImage.sprite = customSprite;
                }
                int teamID = trackable.GetTeamID();
                if (teamID == FormationController.instance.TeamID) newIcon.iconImage.color = playerColor;
                else if (teamID == -1) newIcon.iconImage.color = neutralColor; 
                else newIcon.iconImage.color = enemyColor;
            }

            iconInstances.Add(trackable, newIcon);
            UpdateIcon(trackable, newIcon);
        }

        public void UnregisterTrackable(IMinimapTrackable trackable)
        {
            if (trackable != null && iconInstances.TryGetValue(trackable, out MinimapIcon icon))
            {
                if (icon != null) Destroy(icon.gameObject);
                iconInstances.Remove(trackable);
            }
        }

        private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
        {
            if (worldBounds.width == 0 || worldBounds.height == 0) return Vector2.zero;
            float normalizedX = (worldPosition.x - worldBounds.xMin) / worldBounds.width;
            float normalizedZ = (worldPosition.z - worldBounds.yMin) / worldBounds.height;
            float iconX = normalizedX * minimapRect.rect.width - minimapRect.rect.width * 0.5f;
            float iconY = normalizedZ * minimapRect.rect.height - minimapRect.rect.height * 0.5f;
            return new Vector2(iconX, iconY);
        }

        private void UpdateIcon(IMinimapTrackable trackable, MinimapIcon icon)
        {
            if (icon == null || trackable == null || (trackable as Object) == null)
            {
                if (trackable != null) iconInstances.Remove(trackable);
                if (icon != null) Destroy(icon.gameObject);
                return;
            }

            if (!trackable.IsVisibleOnMap())
            {
                icon.gameObject.SetActive(false);
                return;
            }
            icon.gameObject.SetActive(true);
            Vector2 rawPos = WorldToMinimapPosition(trackable.GetWorldPosition());
            float halfWidth = minimapRect.rect.width * 0.5f;
            float halfHeight = minimapRect.rect.height * 0.5f;

            rawPos.x = Mathf.Clamp(rawPos.x, -halfWidth + iconEdgeOffset, halfWidth - iconEdgeOffset);
            rawPos.y = Mathf.Clamp(rawPos.y, -halfHeight + iconEdgeOffset, halfHeight - iconEdgeOffset);

            icon.rectTransform.anchoredPosition = rawPos;
            float angle = trackable.GetWorldRotationY();
            icon.rectTransform.localEulerAngles = new Vector3(0, 0, -angle);

            if (icon.iconImage != null)
            {
                Sprite currentSprite = trackable.GetMinimapIcon();
                if (currentSprite != null && icon.iconImage.sprite != currentSprite)
                {
                    icon.iconImage.sprite = currentSprite;
                }

                int currentTeamID = trackable.GetTeamID();
                Color targetColor;

                if (currentTeamID == FormationController.instance.TeamID) targetColor = playerColor;
                else if (currentTeamID == -1) targetColor = neutralColor;
                else targetColor = enemyColor;

                if (icon.iconImage.color != targetColor)
                {
                    icon.iconImage.color = targetColor;
                }
            }
        }

        private void UpdateCameraIcon()
        {
            if (cameraIcon == null || mainCamera == null) return;
            Vector2 rawPosition = WorldToMinimapPosition(mainCamera.transform.position);
            float hw = minimapRect.rect.width * 0.5f;
            float hh = minimapRect.rect.height * 0.5f;
            rawPosition.x = Mathf.Clamp(rawPosition.x, -hw + iconEdgeOffset, hw - iconEdgeOffset);
            rawPosition.y = Mathf.Clamp(rawPosition.y, -hh + iconEdgeOffset, hh - iconEdgeOffset);

            cameraIcon.anchoredPosition = rawPosition;
            cameraIcon.localEulerAngles = new Vector3(0, 0, -mainCamera.transform.eulerAngles.y);
        }

        private void OnDrawGizmos()
        {
            if (worldCorner1 != null && worldCorner2 != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 p1 = worldCorner1.position;
                Vector3 p3 = worldCorner2.position;
                Vector3 p2 = new Vector3(p3.x, p1.y, p1.z);
                Vector3 p4 = new Vector3(p1.x, p1.y, p3.z);
                Gizmos.DrawLine(p1, p2); Gizmos.DrawLine(p2, p3); Gizmos.DrawLine(p3, p4); Gizmos.DrawLine(p4, p1);
            }
        }
    }
}