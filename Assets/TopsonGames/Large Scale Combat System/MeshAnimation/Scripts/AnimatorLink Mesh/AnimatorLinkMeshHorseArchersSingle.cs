namespace TopsonGames
{
    using TopsonGames.MeshAnimationSystem;
    using UnityEngine;
    using System.Collections;

    public class AnimatorLinkMeshHorseArchersSingle : AnimatorLinkMeshCavalrySingle
    {
        [Header("360 Degree Ranged Animations - IDLE (Mesh)")]
        [SerializeField] MeshAnimation AttackRangedIdleF;  // 0: Forward
        [SerializeField] MeshAnimation AttackRangedIdleFR; // 1: Front Right
        [SerializeField] MeshAnimation AttackRangedIdleR;  // 2: Right
        [SerializeField] MeshAnimation AttackRangedIdleBR; // 3: Back Right
        [SerializeField] MeshAnimation AttackRangedIdleB;  // 4: Back
        [SerializeField] MeshAnimation AttackRangedIdleBL; // 5: Back Left
        [SerializeField] MeshAnimation AttackRangedIdleL;  // 6: Left
        [SerializeField] MeshAnimation AttackRangedIdleFL; // 7: Front Left

        [Header("360 Degree Ranged Animations - WALK/RUN (Mesh)")]
        [SerializeField] MeshAnimation AttackRangedWalkF;
        [SerializeField] MeshAnimation AttackRangedWalkFR;
        [SerializeField] MeshAnimation AttackRangedWalkR;
        [SerializeField] MeshAnimation AttackRangedWalkBR;
        [SerializeField] MeshAnimation AttackRangedWalkB;
        [SerializeField] MeshAnimation AttackRangedWalkBL;
        [SerializeField] MeshAnimation AttackRangedWalkL;
        [SerializeField] MeshAnimation AttackRangedWalkFL;

        [Header("Aiming System")]
        [Tooltip("The parent of the shootpoint (e.g. weapon) that is to rotate along the Y-axis")]
        public Transform shootPointParent;

        private int aimDirectionHash;
        private Vector3 currentAimTarget;
        private bool isAiming = false;
        private int currentDirectionIndex = 0;

        public override void OnStart()
        {
            base.OnStart();
            aimDirectionHash = Animator.StringToHash("AimDirection");
        }

        public override void SetBlend(float value)
        {
            if (IsAnimator)
            {
                base.SetBlend(value);
            }
            else
            {
                var anim = meshAnimator.GetCurrentAnimation();

                if (IsDirectionalAttack(anim) && meshAnimator.IsPlaying)
                {
                    bool isMoving = value > 0.1f;
                    MeshAnimation correctAnim = GetDirectionalAnimation(currentDirectionIndex, isMoving);

                    if (anim != correctAnim && correctAnim != null)
                    {
                        meshAnimator.Play(correctAnim);
                    }

                    return;
                }
                base.SetBlend(value);
            }
        }

        public void SetAttackRangedDirectional(bool value, int directionIndex)
        {
            currentDirectionIndex = directionIndex;

            if (IsAnimator)
            {
                animator.SetInteger(aimDirectionHash, directionIndex);
                if (value)
                {
                    animator.SetTrigger("AttackRanged");
                    animator.SetBool("AttackRanged", true);
                    StartCoroutine(ResetAttackRangedRoutine());
                }
                else
                {
                    animator.ResetTrigger("AttackRanged");
                    animator.SetBool("AttackRanged", false);
                }
            }
            else
            {
                if (value)
                {
                    if (meshAnimator.GetCurrentAnimation() != Death)
                    {
                        bool isMoving = unit != null && unit.agent != null && unit.agent.velocity.magnitude > 0.1f;
                        MeshAnimation animToPlay = GetDirectionalAnimation(directionIndex, isMoving);

                        if (animToPlay != null)
                        {
                            meshAnimator.Play(animToPlay);
                        }
                    }
                }
            }
        }

        private IEnumerator ResetAttackRangedRoutine()
        {
            yield return new WaitForSeconds(0.2f);
            if (animator != null && IsAnimator)
            {
                animator.ResetTrigger("AttackRanged");
                animator.SetBool("AttackRanged", false);
            }
        }

        public void SetAimTarget(Vector3 targetPos)
        {
            currentAimTarget = targetPos;
            isAiming = true;
        }

        public void StopAiming()
        {
            isAiming = false;
        }

        private void LateUpdate()
        {
            if (isAiming && shootPointParent != null)
            {
                Vector3 dir = (currentAimTarget - shootPointParent.position).normalized;
                dir.y = 0;

                if (dir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    shootPointParent.rotation = Quaternion.Slerp(shootPointParent.rotation, targetRot, Time.deltaTime * 15f);
                }
            }
        }

        private MeshAnimation GetDirectionalAnimation(int index, bool isMoving)
        {
            if (isMoving)
            {
                switch (index)
                {
                    case 0: return AttackRangedWalkF;
                    case 1: return AttackRangedWalkFR;
                    case 2: return AttackRangedWalkR;
                    case 3: return AttackRangedWalkBR;
                    case 4: return AttackRangedWalkB;
                    case 5: return AttackRangedWalkBL;
                    case 6: return AttackRangedWalkL;
                    case 7: return AttackRangedWalkFL;
                    default: return AttackRangedWalkF;
                }
            }
            else
            {
                switch (index)
                {
                    case 0: return AttackRangedIdleF;
                    case 1: return AttackRangedIdleFR;
                    case 2: return AttackRangedIdleR;
                    case 3: return AttackRangedIdleBR;
                    case 4: return AttackRangedIdleB;
                    case 5: return AttackRangedIdleBL;
                    case 6: return AttackRangedIdleL;
                    case 7: return AttackRangedIdleFL;
                    default: return AttackRangedIdleF;
                }
            }
        }

        private bool IsDirectionalAttack(MeshAnimation anim)
        {
            if (anim == null) return false;

            return anim == AttackRangedIdleF || anim == AttackRangedIdleFR || anim == AttackRangedIdleR ||
                   anim == AttackRangedIdleBR || anim == AttackRangedIdleB || anim == AttackRangedIdleBL ||
                   anim == AttackRangedIdleL || anim == AttackRangedIdleFL ||
                   anim == AttackRangedWalkF || anim == AttackRangedWalkFR || anim == AttackRangedWalkR ||
                   anim == AttackRangedWalkBR || anim == AttackRangedWalkB || anim == AttackRangedWalkBL ||
                   anim == AttackRangedWalkL || anim == AttackRangedWalkFL;
        }
    }
}