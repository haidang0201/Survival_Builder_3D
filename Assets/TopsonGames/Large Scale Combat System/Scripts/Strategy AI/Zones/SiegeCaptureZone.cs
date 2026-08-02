namespace TopsonGames.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using TopsonGames.Minimap;
    using UnityEngine;
    using UnityEngine.Events;

    [RequireComponent(typeof(Collider))]
    public class SiegeCaptureZone : MonoBehaviour, IMinimapTrackable
    {
        public enum CaptureStatus { Neutral, CapturingAttacker, ControlledAttacker, CapturingDefender, ControlledDefender }

        [Header("Zone Settings")]
        public float captureTime = 30.0f;
        public int initialOwnerTeamID = 1;
        public bool isVictoryPoint = true;
        [Tooltip("Does this zone count towards the overall victory score?")]
        public bool contributesToResult = true;
        public int priority = 10;

        [Header("Tactical Points")]
        public List<SiegeChokePoint> chokePoints;
        public List<Transform> tacticalApproachPoints;

        [Header("Minimap")]
        public Sprite minimapIcon;

        [Header("Events")]
        public UnityEvent<int> OnOwnerChanged;
        public UnityEvent OnContested;

        [Header("Capture Status (Runtime)")]
        [SerializeField] private CaptureStatus status;
        [SerializeField] private float captureProgress = 0f;
        [SerializeField] private int controllingTeamID = 0;

        [SerializeField] public int currentAttackerCount = 0;
        [SerializeField] public int currentDefenderCount = 0;

        private List<Unit> unitsInZone = new List<Unit>();
        private Collider zoneCollider;

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            if (zoneCollider == null || !zoneCollider.isTrigger)
            {
                Debug.LogError($"SiegeCaptureZone '{name}' requires a collider with 'Is Trigger' = true!", this);
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            initialOwnerTeamID = SiegeManager.instance.defenderTeamID;
            controllingTeamID = initialOwnerTeamID;

            if (SiegeManager.instance != null)
            {
                if (initialOwnerTeamID == SiegeManager.instance.attackerTeamID)
                    status = CaptureStatus.ControlledAttacker;
                else if (initialOwnerTeamID == SiegeManager.instance.defenderTeamID)
                    status = CaptureStatus.ControlledDefender;
                else
                    status = CaptureStatus.Neutral;
            }
            else
            {
                status = CaptureStatus.Neutral;
            }


            if (status == CaptureStatus.ControlledAttacker)
            {
                captureProgress = captureTime;
            }
            else
            {
                captureProgress = 0f;
            }
            if (MinimapController.instance != null) MinimapController.instance.RegisterTrackable(this);
        }

        private void Update()
        {
            if (SiegeManager.instance == null) return;

            unitsInZone.RemoveAll(u => u == null || !u.gameObject.activeInHierarchy || u.GetCurrentHealth() <= 0);

            currentAttackerCount = 0;
            currentDefenderCount = 0;
            foreach (var unit in unitsInZone)
            {
                if (unit.GetFormation() == null) continue;
                if (unit.GetFormation().TeamID == SiegeManager.instance.attackerTeamID) currentAttackerCount++;
                else if (unit.GetFormation().TeamID == SiegeManager.instance.defenderTeamID) currentDefenderCount++;
            }

            bool attackersPresent = currentAttackerCount > 0;
            bool defendersPresent = currentDefenderCount > 0;

            if (attackersPresent && !defendersPresent)
            {
                if (controllingTeamID != SiegeManager.instance.attackerTeamID)
                {
                    if (status != CaptureStatus.CapturingAttacker) OnContested.Invoke();
                    status = CaptureStatus.CapturingAttacker;
                    captureProgress += Time.deltaTime;
                    if (captureProgress >= captureTime)
                    {
                        captureProgress = captureTime;
                        ChangeOwner(SiegeManager.instance.attackerTeamID, CaptureStatus.ControlledAttacker);
                    }
                }
            }
            else if (!attackersPresent && defendersPresent)
            {
                if (controllingTeamID != SiegeManager.instance.defenderTeamID)
                {
                    if (status != CaptureStatus.CapturingDefender) OnContested.Invoke();
                    status = CaptureStatus.CapturingDefender;
                    captureProgress -= Time.deltaTime;
                    if (captureProgress <= 0)
                    {
                        captureProgress = 0;
                        ChangeOwner(SiegeManager.instance.defenderTeamID, CaptureStatus.ControlledDefender);
                    }
                }
            }
            else
            {
                if (status == CaptureStatus.CapturingAttacker) status = CaptureStatus.ControlledDefender;
                if (status == CaptureStatus.CapturingDefender) status = CaptureStatus.ControlledAttacker;
            }
        }

        private void ChangeOwner(int newTeamID, CaptureStatus newStatus)
        {
            if (controllingTeamID != newTeamID)
            {
                controllingTeamID = newTeamID;
                status = newStatus;
                OnOwnerChanged.Invoke(newTeamID);
                Debug.Log($"Zone {name} captured by Team {newTeamID}!");
            }
        }

        public void ResetChokePointUsage()
        {
            if (chokePoints != null) foreach (var cp in chokePoints) if (cp != null) cp.ResetUsage();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent<Unit>(out Unit unit))
            {
                if (unit != null && unit.GetFormation() != null && !unitsInZone.Contains(unit)) unitsInZone.Add(unit);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent<Unit>(out Unit unit))
            {
                if (unit != null) unitsInZone.Remove(unit);
            }
        }
        private void OnDisable()
        {
            if (MinimapController.instance != null) MinimapController.instance.UnregisterTrackable(this);
        }
        public CaptureStatus GetStatus() => status;
        public int GetControllingTeam() => controllingTeamID;
        public bool IsVictoryPoint() => isVictoryPoint;
        public Vector3 GetZoneCenter() => transform.position;
        public int GetPriority() => priority;
        public bool ContributesToResult() => contributesToResult;
        public int GetAttackerCount() => currentAttackerCount;
        public int GetDefenderCount() => currentDefenderCount;

        public float GetCaptureProgressNormalized()
        {
            if (captureTime <= 0) return 1f;
            return Mathf.Clamp01(captureProgress / captureTime);
        }
        public Vector3 GetWorldPosition()
        {
            return transform.position;
        }

        public float GetWorldRotationY()
        {
            return 0;
        }

        public bool IsVisibleOnMap()
        {
            return gameObject.activeInHierarchy;
        }

        public int GetTeamID()
        {
            return controllingTeamID;
        }

        public Sprite GetMinimapIcon()
        {
            return minimapIcon;
        }


        private void OnDrawGizmos()
        {
            var mn = FindAnyObjectByType<SiegeManager>();
            if (mn != null && mn.DrawZoneGizmos == false)
                return;

            if (chokePoints != null) foreach (var cp in chokePoints) if (cp != null) { Gizmos.color = Color.yellow; Gizmos.DrawLine(transform.position, cp.transform.position); }
            if (tacticalApproachPoints != null) foreach (var point in tacticalApproachPoints) if (point != null) { Gizmos.color = Color.gray; Gizmos.DrawWireSphere(point.position, 0.5f); Gizmos.DrawLine(transform.position, point.position); }

            Matrix4x4 oldMatrix = Gizmos.matrix; 

            Gizmos.matrix = transform.localToWorldMatrix;

            var box = GetComponent<BoxCollider>();
            if (box != null)
            {

                Gizmos.color = controllingTeamID == initialOwnerTeamID ? new Color(0f, 0.3f, 1f, 0.2f) : new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawCube(box.center, box.size);

                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            Gizmos.matrix = oldMatrix;

            Gizmos.matrix = oldMatrix;
        }

    }
}