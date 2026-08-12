namespace TopsonGames
{
    using System.Collections;
    using TopsonGames.MeshAnimationSystem;
    using UnityEngine;
    using UnityEngine.AI;
    using UnityEngine.Events;

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(DestructibleTarget))]
    public class Unit : MonoBehaviour, IDamageProcessor
    {
        [Header("Components")]
        public NavMeshAgent agent;
        public Transform visualsTransform;
        public GameObject PositionIndicator;
        public GameObject Waypoint;
        public GameObject WaypointIndicator;
        public GameObject WaypointMarker;
        public AnimatorLink animatorLink;
        public DestructibleTarget Destructible { get; private set; }

        [Header("Weapons")]
        public GameObject MeleeWeapon;
        public RangedWeapon RangedWeapon;
        public Collider shieldCollider;

        [Header("Unit Data Override (Optional)")]
        [Tooltip("Insert a specific UnitDataSO (e.g., spear carrier stats) here. If empty, the formation's stats will be used.")]
        public UnitDataSO specificUnitData;

        public UnitDataSO ActiveUnitData
        {
            get
            {
                if (specificUnitData != null) return specificUnitData;
                if (parentFormation != null && parentFormation.UnitData != null) return parentFormation.UnitData;
                return null;
            }
        }

        public CombatBehaviourSO ActiveCombatBehaviour
        {
            get
            {
                var data = ActiveUnitData;
                return data != null ? data.combatBehaviour : null;
            }
        }

        [Header("Swith Animation Systems (Can be left Empty)")]
        public Transform MeleeAnimatorParent;
        public Transform MeleeGPUParent;
        public Transform RangedAnimatorParent;
        public Transform RangedGPUParent;
        public Transform ShieldAnimatorParent;
        public Transform ShieldGPUParent;
        public Transform SaddleAnimatorParent;
        public Transform SaddleGPUParent;

        [Header("Death")]
        [SerializeField] MonoBehaviour[] disableOnDeath;
        public UnityEvent OnRevieveDamage;
        public UnityEvent OnDeath;
        public UnityEvent OnDoDamage;

        [Header("Debugging")]
        [SerializeField] DestructibleTarget currentTarget;
        [SerializeField] DestructibleTarget closestEnemy;
        public enum UnitState { Idle, Moving, Fighting }
        public UnitState currentState = UnitState.Idle;

        private Coroutine movementRoutine;
        private Coroutine arrowRoutine;
        private Coroutine knockbackRoutine;
        private float movementTimer = 0.5f;
        private Formation parentFormation;

        [HideInInspector] public Vector3 lastTargetPosition;
        [HideInInspector] public float attackTimer;
        [HideInInspector] public float findEnemyTimer = 1f;
        [HideInInspector] public float switchAnimationTimer = 0.2f;
        [HideInInspector] public int currentIdleAnimation = 0;
        [HideInInspector] public bool hasAppliedKnockback = false;
        public int rowInFormation;
        [HideInInspector] public float navMeshRadius;

        private void Awake()
        {
            Destructible = GetComponent<DestructibleTarget>();
        }

        public void OnStart(Transform waypointIndicatorParent, Formation parent)
        {
            this.parentFormation = parent;

            agent.speed = parent.UnitData.formationMoveSpeed;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.autoBraking = true;
      //      agent.radius = parent.UnitData.formationSpacing * 0.30f;
            agent.updateRotation = false;
            agent.stoppingDistance = parent.UnitData.stoppingDistance;
            navMeshRadius = agent.radius;

            animatorLink.SetAnimatorSpeed(Random.Range(0.85f, 1.15f));

            if (ActiveUnitData != null)
                Destructible.Initialize(parent.TeamID, ActiveUnitData.maxHealth, this);
            else
                Destructible.Initialize(parent.TeamID, 100f, this);

            if (FormationController.instance.waypointContainer != null)
                Waypoint.transform.SetParent(FormationController.instance.waypointContainer);
            else
                Waypoint.transform.SetParent(null);

            WaypointMarker.transform.SetParent(waypointIndicatorParent);
            WaypointIndicator.transform.SetParent(waypointIndicatorParent);

            if (shieldCollider) shieldCollider.enabled = false;

            if (ActiveCombatBehaviour != null)
                ActiveCombatBehaviour.InitializeUnit(this, parentFormation);
        }

        public void OnUpdate()
        {
            if (agent == null || !agent.isOnNavMesh || Destructible.currentHealth <= 0) return;

            var behaviour = ActiveCombatBehaviour;
            if (behaviour == null) return;

            switch (currentState)
            {
                case UnitState.Idle:
                    agent.enabled = true;
                    behaviour.TickIdle(this, agent, parentFormation, movementRoutine);
                    hasAppliedKnockback = false;
                    break;
                case UnitState.Moving:
                    agent.enabled = true;
                    behaviour.TickMovement(this, agent, parentFormation, movementRoutine);
                    hasAppliedKnockback = false;
                    break;
                case UnitState.Fighting:
                    {
                        if (currentTarget == null || currentTarget.currentHealth <= 0)
                        {
                            currentTarget = null;
                            currentState = UnitState.Idle;
                            ResetTarget();
                            animatorLink.SetAttack(false);
                            break;
                        }
                    }
                    behaviour.TickCombat(this, attackTimer, movementRoutine);
                    break;
            }
            behaviour.OnUpdateUnit(this, parentFormation);
        }

        public float ProcessDamage(float rawDamage, Unit attacker, bool shieldHit = false)
        {
            if (GameManager.instance != null && GameManager.instance.damageModifier != null && GameManager.instance.damageModifier.activeCombatRuleset != null)
            {
                return GameManager.instance.damageModifier.activeCombatRuleset.ProcessIncomingDamage(this, attacker, rawDamage, shieldHit, ActiveCombatBehaviour);
            }
            float blockChance = ActiveUnitData != null ? ActiveUnitData.shieldMeleeBlockChance : 0f;
            if (!shieldHit && Random.Range(0f, 1f) < blockChance)
            {
                if (ActiveCombatBehaviour != null)
                    ActiveCombatBehaviour.OnTakeDamage(0, this, attacker, true);
                return 0f;
            }

            if (ActiveCombatBehaviour != null)
                ActiveCombatBehaviour.OnTakeDamage(rawDamage, this, attacker, shieldHit);

            return rawDamage;
        }

        public void Attack(bool isRanged = false)
        {
            if (!currentTarget) return;

            if (isRanged)
            {
                Unit targetUnit = currentTarget.GetOwner<Unit>();
                if (targetUnit != null)
                {
                    if (RangedWeapon != null && RangedWeapon.Weapon != null)
                    {
                        RangedWeapon.Attack(targetUnit, parentFormation.ArcherTarget, parentFormation);
                    }
                }
            }
            else
            {
                UnitDataSO defenderData = null;
                Unit targetUnit = currentTarget.GetOwner<Unit>();
                if (targetUnit != null)
                {
                    defenderData = targetUnit.ActiveUnitData;
                }

                float totalDamage = GameManager.instance.damageModifier.GetTotalDamage(ActiveUnitData, defenderData);

                if (parentFormation != null && targetUnit != null && targetUnit.GetFormation() != null)
                    totalDamage *= GameManager.instance.damageModifier.FlankMultiplier(parentFormation.CalculateAverageRotation(), targetUnit.GetFormation().CalculateAverageRotation());

                if (parentFormation.IsChargeBonusActive)
                    totalDamage *= ActiveUnitData.chargeDamageMultiplier;

                currentTarget.TakeDamage(totalDamage, this);
                OnDoDamage.Invoke();
            }
        } 

        public void HandleDeath(Unit Attacker, bool preventCorpse = false)
        {
            if (ActiveCombatBehaviour != null)
                ActiveCombatBehaviour.OnDeath(this, Attacker, parentFormation);

            OnDeath.Invoke();
            if(!preventCorpse)
            {
                animatorLink.SetDeath();
            }
            else
            {
                animatorLink.animator.gameObject.SetActive(false);
                if (animatorLink.GetType() == typeof(AnimatorLinkMesh))
                {
                    MeshAnimator ma = (animatorLink as AnimatorLinkMesh).GetMeshAnimator();
                    if (ma)
                    {
                        MeshInstancingManager.Instance.UnregisterAnimator(ma);
                        ma.SetActiveForInstancing(false);
                        ma.enabled = false;
                    }
                }
            }

            this.enabled = agent.enabled = animatorLink.enabled = GetComponent<Collider>().enabled = false;
            parentFormation.UnitDeath(this);

            WaypointIndicator.SetActive(false);
            PositionIndicator.SetActive(false);
            Waypoint.SetActive(false);
            WaypointMarker.SetActive(false);

            foreach (var script in disableOnDeath)
            {
                script.enabled = false;
            }
            visualsTransform.SetParent(null);
            Destroy(gameObject);
        }

        public bool IsAgentMoving() { return currentState == UnitState.Moving; }

        public void MoveTo(Vector3 destination)
        {
            movementTimer -= Time.deltaTime;
            if (movementTimer < 0 && agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
                agent.SetDestination(destination);
                movementTimer = 0.5f;
            }
        }

        public void StopMovement()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            currentState = UnitState.Idle;
        }

        public void ResetTarget()
        {
            agent.enabled = true;
            currentTarget = closestEnemy = null;
            currentState = UnitState.Idle;
            if (movementRoutine != null)
            {
                StopCoroutine(movementRoutine);
                movementRoutine = null;
            }
            animatorLink.SetAttack(false);
        }

        public void ManageMovementRoutine()
        {
            if (movementRoutine == null)
                movementRoutine = StartCoroutine(MeleeMovementRoutine());
        }

        public void ManageArrowRoutine()
        {
            if (shieldCollider == null) return;
            if (arrowRoutine != null) StopCoroutine(arrowRoutine);
            arrowRoutine = StartCoroutine(ArrowAnimationRoutine());
        }

        public void CancelArrowRoutine()
        {
            if (arrowRoutine != null)
            {
                StopCoroutine(arrowRoutine);
                arrowRoutine = null;
                animatorLink.SetBlock(false);
            }
        }

        public bool IsFacingArcherTarget(Transform Target, float angleThreshold = 20f)
        {
            if (Target == null) return false;
            Vector3 directionToTarget = (Target.transform.position - transform.position).normalized;
            directionToTarget.y = 0;
            Vector3 unitForward = transform.forward;
            unitForward.y = 0;
            if (directionToTarget == Vector3.zero || unitForward == Vector3.zero) return false;
            float angle = Vector3.Angle(unitForward, directionToTarget);
            return angle <= angleThreshold;
        }

        void SwitchToRanged()
        {
            if (RangedWeapon != null && RangedWeapon.Weapon != null)
                RangedWeapon.Weapon.SetActive(true);

            if (MeleeWeapon != null)
                MeleeWeapon.SetActive(false);
        }

        void SwitchToMelee()
        {
            if (RangedWeapon != null && RangedWeapon.Weapon != null)
                RangedWeapon.Weapon.SetActive(false);

            if (MeleeWeapon != null)
                MeleeWeapon.SetActive(true);
        }

        public void ReportArrows()
        {
            if (ActiveCombatBehaviour != null)
                ActiveCombatBehaviour.OnReportArrowsUnit(this, arrowRoutine);
        }

        public void ApplyKnockback(Vector3 knockbackDirection, float force, float knockbackNavMeshSampleRange, float knockbackDuration)
        {
            if (parentFormation.UnitData.canRecieveKnockback == false)
                return;
            if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
            knockbackRoutine = StartCoroutine(DoKnockback(knockbackDirection, force, knockbackNavMeshSampleRange, knockbackDuration));
        }

        private IEnumerator DoKnockback(Vector3 knockbackDirection, float force, float knockbackNavMeshSampleRange, float knockbackDuration)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) yield break;
            animatorLink.SetKnockback(false);
            animatorLink.SetKnockback(true);
            knockbackDirection.y = 0;
            knockbackDirection.Normalize();
            agent.enabled = false;
            float timer = 0f;
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = startPosition + knockbackDirection * force;
            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, knockbackNavMeshSampleRange, NavMesh.AllAreas))
            {
                targetPosition = hit.position;
            }
            else
            {
                if (NavMesh.SamplePosition(transform.position, out hit, knockbackNavMeshSampleRange, NavMesh.AllAreas))
                {
                    targetPosition = hit.position;
                }
                else
                {
                    agent.enabled = true;
                    yield break;
                }
            }
            while (timer < knockbackDuration)
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, (timer / knockbackDuration) * 5);
                timer += Time.deltaTime;
                yield return null;
            }
            animatorLink.SetKnockback(false);
            transform.position = targetPosition;
            agent.enabled = true;
            knockbackRoutine = null;
        }

        IEnumerator MeleeMovementRoutine()
        {
            while (currentState == UnitState.Fighting && currentTarget != null)
            {
                if (ActiveCombatBehaviour != null)
                    ActiveCombatBehaviour.OnMovementTick(this);
                yield return null;
            }
            movementRoutine = null;
        }

        public IEnumerator ArrowAnimationRoutine()
        {
            float shieldTime = ActiveCombatBehaviour != null ? ActiveCombatBehaviour.raiseShieldTime : 2f;
            if (shieldCollider) shieldCollider.enabled = true;
            animatorLink.SetBlock(true);
            yield return new WaitForSeconds(shieldTime);
            animatorLink.SetBlock(false);
            if (shieldCollider) shieldCollider.enabled = false;
            arrowRoutine = null;
        }

        public IEnumerator SwitchRangedMelee(bool becomesRange, float waitTime)
        {
            if (becomesRange && (RangedWeapon == null || RangedWeapon.Weapon == null))
            {
                yield break;
            }

            yield return new WaitForSeconds(waitTime);

            if (becomesRange) SwitchToRanged();
            else SwitchToMelee();
        }


        public void SwitchMeleeAnimatorGPUParent(bool switchToAnimator)
        {
            if (MeleeWeapon == null) return;
            if (switchToAnimator && MeleeAnimatorParent)
            {
                MeleeWeapon.transform.parent = MeleeAnimatorParent;
                MeleeWeapon.transform.SetPositionAndRotation(MeleeAnimatorParent.position, MeleeAnimatorParent.rotation);
            }
            else if (MeleeGPUParent)
            {
                MeleeWeapon.transform.parent = MeleeGPUParent;
                MeleeWeapon.transform.SetPositionAndRotation(MeleeGPUParent.position, MeleeGPUParent.rotation);
            }
        }

        public void SwitchRangedAnimatorGPUParent(bool switchToAnimator)
        {
            if (RangedWeapon == null || RangedWeapon.Weapon == null) return;

            if (switchToAnimator && RangedAnimatorParent)
            {
                RangedWeapon.transform.parent = RangedAnimatorParent;
                RangedWeapon.transform.SetPositionAndRotation(RangedAnimatorParent.position, RangedAnimatorParent.rotation);
            }
            else if (RangedGPUParent)
            {
                RangedWeapon.transform.parent = RangedGPUParent;
                RangedWeapon.transform.SetPositionAndRotation(RangedGPUParent.position, RangedGPUParent.rotation);
            }
        }

        public void SwitchShieldAnimatorGPUParent(bool switchToAnimator)
        {
            if (shieldCollider == null) return;
            if (switchToAnimator && ShieldAnimatorParent)
            {
                shieldCollider.transform.parent = ShieldAnimatorParent;
                shieldCollider.transform.SetPositionAndRotation(ShieldAnimatorParent.position, ShieldAnimatorParent.rotation);
            }
            else if (ShieldGPUParent)
            {
                shieldCollider.transform.parent = ShieldGPUParent;
                shieldCollider.transform.SetPositionAndRotation(ShieldGPUParent.position, ShieldGPUParent.rotation);
            }
        }

        public void SwitchSaddleAnimatorGPUParent(bool switchToAnimator)
        {
            if (switchToAnimator && SaddleAnimatorParent)
            {
                transform.parent = SaddleAnimatorParent;
                transform.SetPositionAndRotation(SaddleAnimatorParent.position, SaddleAnimatorParent.rotation);
            }
            else if (SaddleGPUParent)
            {
                transform.transform.parent = SaddleGPUParent;
                transform.transform.SetPositionAndRotation(SaddleGPUParent.position, SaddleGPUParent.rotation);
            }
        }

        public Formation GetFormation()
        {
            return parentFormation;
        }

        public DestructibleTarget GetClosestEnemy()
        {
            return closestEnemy;
        }

        public DestructibleTarget GetCurrentTarget()
        {
            return currentTarget;
        }

        public void SetCurrentTarget(DestructibleTarget target)
        {
            currentTarget = target;
        }

        public void SetClosestEnemy(DestructibleTarget target)
        {
            closestEnemy = target;
        }

        public float GetCurrentHealth()
        {
            return Destructible.currentHealth;
        }
    }
}