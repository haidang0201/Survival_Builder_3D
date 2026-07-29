namespace TopsonGames
{
    using UnityEngine;
    using TopsonGames.Minimap;
    using UnityEngine.AI;
    using System.Collections.Generic;
    using TopsonGames.AI;

    [RequireComponent(typeof(EngagementTrigger))]
    [RequireComponent(typeof(DestructibleTarget))]
    [RequireComponent(typeof(NavMeshObstacle))]
    [RequireComponent(typeof(Collider))]
    public class GateController : MonoBehaviour, IMinimapTrackable
    {
        private DestructibleTarget destructible;
        private NavMeshObstacle obstacle;
        private Collider physicalCollider;

        [Header("Visuals")]
        public GameObject intactVisuals;
        public GameObject destroyedVisuals;
        public bool isDestroyed = false;
        public Sprite minimapIcon;

        [Header("Tactical Awareness")]
        [Tooltip("Sensor that counts attackers BEFORE the goal.")]
        public ThreatSensor attackerSensor;

        [Tooltip("Radius in which defenders are counted BEHIND/AT the goal.")]
        public float defenseDetectionRadius = 30f;

        [SerializeField] private int defenderCount = 0;
        [SerializeField] private int attackerCount = 0;

        private void Awake()
        {
            destructible = GetComponent<DestructibleTarget>();
            obstacle = GetComponent<NavMeshObstacle>();
            physicalCollider = GetComponent<Collider>();
            obstacle.enabled = true;
            obstacle.carving = true;
        }

        private void Start()
        {
            destructible.Initialize(SiegeManager.instance.defenderTeamID, destructible.maxHealth, this);
            this.GetComponent<EngagementTrigger>().Initialize(destructible.TeamID);

            if (MinimapController.instance != null) MinimapController.instance.RegisterTrackable(this);

            if (destructible.targetType != TargetType.Gate) destructible.targetType = TargetType.Gate;

            if (intactVisuals) intactVisuals.SetActive(true);
            if (destroyedVisuals) destroyedVisuals.SetActive(false);
            destructible.OnDeath.AddListener(HandleDestruction);
        }

        private void Update()
        {
            if (isDestroyed) return;
            if (Time.frameCount % 20 == 0)
            {
                UpdateCounts();
            }
        }

        private void UpdateCounts()
        {
            defenderCount = 0;
            if (FormationController.instance != null)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, defenseDetectionRadius, FormationController.instance.unitMask);
                foreach (var hit in hits)
                {
                    Unit unit = hit.GetComponentInParent<Unit>();
                    if (unit != null && unit.GetFormation() != null && unit.GetFormation().TeamID == destructible.TeamID)
                    {
                        defenderCount++;
                    }
                }
            }

            if (attackerSensor != null)
            {
                attackerCount = attackerSensor.GetEnemyCount();
            }
            else
            {
                attackerCount = 0;
                if (FormationController.instance != null)
                {
                    Collider[] hits = Physics.OverlapSphere(transform.position, defenseDetectionRadius, FormationController.instance.unitMask);
                    foreach (var hit in hits)
                    {
                        Unit unit = hit.GetComponentInParent<Unit>();
                        if (unit != null && unit.GetFormation() != null && unit.GetFormation().TeamID != destructible.TeamID)
                        {
                            attackerCount++;
                        }
                    }
                }
            }
        }

        public int GetDefenderCount() => defenderCount;
        public int GetAttackerCount() => attackerCount;

        public void HandleDestruction(DestructibleTarget target = null)
        {
            if (isDestroyed) return;
            isDestroyed = true;
            Debug.Log($"[GateController] Goal {gameObject.name} has been destroyed!", this);

            obstacle.enabled = false;
            physicalCollider.enabled = false;

            if (intactVisuals) intactVisuals.SetActive(false);
            if (destroyedVisuals) destroyedVisuals.SetActive(true);
        }

        private void OnDisable()
        {
            if (MinimapController.instance != null) MinimapController.instance.UnregisterTrackable(this);
        }

        public Vector3 GetWorldPosition() => transform.position;
        public float GetWorldRotationY() => 0;
        public bool IsVisibleOnMap() => gameObject.activeInHierarchy;
        public int GetTeamID() => destructible.TeamID;
        public Sprite GetMinimapIcon() => minimapIcon;

        private void OnDrawGizmos()
        {
            if (SiegeManager.instance != null && SiegeManager.instance.DrawZoneGizmos == false)
                return;

            Gizmos.color = new Color(0, 0, 1, 0.2f);
            Gizmos.DrawWireSphere(transform.position, defenseDetectionRadius);
            if (attackerSensor != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, attackerSensor.transform.position);
            }
        }
    }
}