namespace TopsonGames
{
    using UnityEngine;

    [RequireComponent(typeof(Collider))]
    public class NoPlacementZone : MonoBehaviour
    {
        public string Description = "Script Blocks Formations Placement on attached Collider!";
        [Header("Settings")]
        [Tooltip("Should a visual cue be displayed in the editor?")]
        public bool showGizmo = true;
        public Color gizmoColor = new Color(1, 0, 0, 0.3f); 

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            Gizmos.color = gizmoColor;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            var box = GetComponent<BoxCollider>();
            if (box != null)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(1, 0, 0, 1f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else
            {
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }

            Gizmos.matrix = oldMatrix;
        }
    }
}