namespace TopsonGames
{
    using TopsonGames.MeshAnimationSystem;
    using UnityEngine;
    using System.Collections;

    public class AnimatorLinkMeshHorseArchers : AnimatorLinkMeshCavalry
    {
        [Header("360 Degree Ranged Animations (Mesh)")]
        [SerializeField] MeshAnimation AttackRangedF;  // 0: Forward
        [SerializeField] MeshAnimation AttackRangedFR; // 1: Front Right
        [SerializeField] MeshAnimation AttackRangedR;  // 2: Right
        [SerializeField] MeshAnimation AttackRangedBR; // 3: Back Right
        [SerializeField] MeshAnimation AttackRangedB;  // 4: Back
        [SerializeField] MeshAnimation AttackRangedBL; // 5: Back Left
        [SerializeField] MeshAnimation AttackRangedL;  // 6: Left
        [SerializeField] MeshAnimation AttackRangedFL; // 7: Front Left

        [Header("Aiming System")]
        [Tooltip("The parent of the shootpoint (e.g. weapon) that is to rotate along the Y-axis")]
        public Transform shootPointParent;

        private int aimDirectionHash;
        private Vector3 currentAimTarget;
        private bool isAiming = false;

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
                    return;
                base.SetBlend(value);
            }
        }

        public void SetAttackRangedDirectional(bool value, int directionIndex)
        {
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
                    if (meshAnimator.GetCurrentAnimation() == null || meshAnimator.GetCurrentAnimation() != Death)
                    {
                        MeshAnimation animToPlay = GetDirectionalAnimation(directionIndex);

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

        private MeshAnimation GetDirectionalAnimation(int index)
        {
            switch (index)
            {
                case 0: return AttackRangedF;
                case 1: return AttackRangedFR;
                case 2: return AttackRangedR;
                case 3: return AttackRangedBR;
                case 4: return AttackRangedB;
                case 5: return AttackRangedBL;
                case 6: return AttackRangedL;
                case 7: return AttackRangedFL;
                default: return AttackRangedF;
            }
        }

        private bool IsDirectionalAttack(MeshAnimation anim)
        {
            if (anim == null) return false;
            return anim == AttackRangedF || anim == AttackRangedFR || anim == AttackRangedR ||
                   anim == AttackRangedBR || anim == AttackRangedB || anim == AttackRangedBL ||
                   anim == AttackRangedL || anim == AttackRangedFL;
        }
    }
}