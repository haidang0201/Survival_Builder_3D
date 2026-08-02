namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using TopsonGames;

    public class AttackerZone : MonoBehaviour
    {
        [Header("Assignment")]
        [Tooltip("Which gate/building is this attack zone aimed at?")]
        public DestructibleTarget linkedStructure; 

        [Header("Zone Settings")]
        [Tooltip("The desired width of the formation (world units). 0 = width is ignored.")]
        public float zoneWidth = 15f;
        [Tooltip("The depth of the zone (only for Gizmo).")]
        public float zoneDepth = 5f;
        [Tooltip("Preferred unit types for this zone (optional).")]
        public List<UnitTypeSO> preferredUnitTypes;
        [Tooltip("Priority of this zone (higher = more important).")]
        public int priority = 0;

        [Header("Status (For information only in the editor)")]
        [SerializeField] private Formation occupyingFormation = null;

        public void SetOccupier(Formation formation)
        {
            occupyingFormation = formation;
        }

        public Formation GetOccupier()
        {
            return occupyingFormation;
        }

        public bool IsOccupied()
        {
            return occupyingFormation != null && occupyingFormation.gameObject.activeInHierarchy;
        }

        private void OnDrawGizmos()
        {
            var mn = FindAnyObjectByType<SiegeManager>();
            if (mn != null && mn.DrawZoneGizmos == false)
                return;

            Color zoneColor = Color.yellow;
            if (preferredUnitTypes != null && preferredUnitTypes.Count > 0)
            {
                if (preferredUnitTypes.Any(type => type != null && type.name.ToLower().Contains("archer"))) zoneColor = Color.cyan;
            }

            Gizmos.color = IsOccupied() ? zoneColor * 0.5f : zoneColor;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;

            Gizmos.DrawWireCube(Vector3.zero, new Vector3(zoneWidth > 0 ? zoneWidth : 1f, 0.1f, zoneDepth));
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * (zoneDepth / 1.5f));

            Gizmos.matrix = Matrix4x4.identity;

            if (linkedStructure != null)
            {
                Gizmos.color = new Color(1, 0, 0, 0.3f); // Transparent red
                Gizmos.DrawLine(transform.position, linkedStructure.transform.position);
            }

#if UNITY_EDITOR
            string label = $"Prio: {priority}";

            if (preferredUnitTypes != null && preferredUnitTypes.Count > 0)
            {
                string unitNames = string.Join(", ", preferredUnitTypes.Where(t => t != null).Select(t => t.name));
                label += $"\nPref: {unitNames}";
            }

            if (IsOccupied())
            {
                label += $"\nOcc: {GetOccupier().name}";
            }

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;

            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.0f, label, style);
#endif
        }
    }
}