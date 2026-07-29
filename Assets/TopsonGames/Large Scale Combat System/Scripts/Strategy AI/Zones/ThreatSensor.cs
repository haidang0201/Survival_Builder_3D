namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using UnityEngine;

    [RequireComponent(typeof(Collider))]
    public class ThreatSensor : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("How often (in seconds) should scanning take place? (Performance)")]
        public float scanInterval = 0.5f;

        [Header("Status")]
        [SerializeField] private int enemyCount = 0;
        [SerializeField] private float currentThreatLevel = 0f;

        private List<Unit> unitsInSensor = new List<Unit>();
        private float timer;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                timer = scanInterval;
                ValidateUnits();
            }
        }

        private void ValidateUnits()
        {
            if (SiegeManager.instance == null) return;

            unitsInSensor.RemoveAll(u => u == null || !u.gameObject.activeInHierarchy || u.GetCurrentHealth() <= 0);
            enemyCount = 0;
            foreach (var unit in unitsInSensor)
            {
                if (unit.GetFormation() != null && unit.GetFormation().TeamID == SiegeManager.instance.attackerTeamID)
                {
                    enemyCount++;
                }
            }
            currentThreatLevel = enemyCount;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent<Unit>(out Unit unit))
            {
                if (!unitsInSensor.Contains(unit)) unitsInSensor.Add(unit);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent<Unit>(out Unit unit))
            {
                unitsInSensor.Remove(unit);
            }
        }

        public float GetThreatLevel()
        {
            return currentThreatLevel;
        }

        public int GetEnemyCount()
        {
            return enemyCount;
        }

        private void OnDrawGizmos()
        {
            var mn = FindAnyObjectByType<SiegeManager>();
            if (mn != null && mn.DrawZoneGizmos == false)
                return;

            Gizmos.color = enemyCount > 0 ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.1f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Matrix4x4 oldMatrix = Gizmos.matrix; 
            Gizmos.matrix = transform.localToWorldMatrix;

            var box = GetComponent<BoxCollider>();
            if (box != null)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            Gizmos.matrix = oldMatrix;
        }
    }
}