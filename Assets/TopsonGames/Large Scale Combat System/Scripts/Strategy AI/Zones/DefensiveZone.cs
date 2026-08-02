namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using TopsonGames;

    public class DefensiveZone : MonoBehaviour
    {
        [Header("Assignment & Intelligence")]
        [Tooltip("Which gate/building does this defense zone belong to? (Strategic assignment)")]
        public DestructibleTarget linkedStructure;

        [Tooltip("Optional: A sensor that monitors the area IN FRONT of this wall. (Tactical threat)")]
        public ThreatSensor linkedSensor;

        [Header("Zone Settings")]
        public float zoneWidth = 10f;
        public float zoneDepth = 3f;

        [Tooltip("Basic priority of this zone (without enemies). Higher = more important (e.g., directly above the gate).")]
        public int basePriority = 10;

        [Tooltip("Which units should be listed here?")]
        public List<UnitTypeSO> preferredUnitTypes;

        [Header("Connectivity")]
        [Tooltip("Manual or automatic (right click on script - Auto Find Neighbors (Distance)) list of neighboring zones.")]
        public List<DefensiveZone> neighbors = new List<DefensiveZone>();

        [Header("Runtime Status")]
        [SerializeField] private Formation occupyingFormation = null;

        public float GetDynamicPriority()
        {
            float prio = basePriority;

            if (linkedSensor != null)
            {
                prio += linkedSensor.GetThreatLevel() * 10f;
            }
            return prio;
        }

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
        [ContextMenu("Auto Find Neighbors (Distance)")]
        public void FindNeighborsByDistance()
        {
            neighbors.Clear();
            var allZones = FindObjectsByType<DefensiveZone>(FindObjectsSortMode.None);
            foreach (var zone in allZones)
            {
                if (zone == this) continue;
                if (Vector3.Distance(transform.position, zone.transform.position) < zoneWidth + 2.0f)
                {
                    neighbors.Add(zone);
                }
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void OnDrawGizmos()
        {
            var mn = FindAnyObjectByType<SiegeManager>();
            if (mn != null && mn.DrawZoneGizmos == false)
                return;

            Color zoneColor = Color.green;
            if (preferredUnitTypes != null && preferredUnitTypes.Count > 0)
            {
                if (preferredUnitTypes.Any(type => type != null && type.name.ToLower().Contains("archer")))
                    zoneColor = Color.cyan;
            }

            Gizmos.color = IsOccupied() ? Color.red : zoneColor;

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.matrix = rotationMatrix;

            Gizmos.DrawWireCube(Vector3.zero, new Vector3(zoneWidth, 0.1f, zoneDepth));
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * (zoneDepth / 1.5f));

            Gizmos.matrix = Matrix4x4.identity;

            if (linkedStructure != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawLine(transform.position, linkedStructure.transform.position);
            }

            Gizmos.color = Color.yellow;
            foreach (var neighbor in neighbors)
            {
                if (neighbor != null)
                    Gizmos.DrawLine(transform.position, neighbor.transform.position);
            }
        }
    }
}