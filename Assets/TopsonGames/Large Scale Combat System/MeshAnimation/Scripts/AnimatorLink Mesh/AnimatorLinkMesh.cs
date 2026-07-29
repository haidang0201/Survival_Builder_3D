namespace TopsonGames
{
    using TopsonGames.MeshAnimationSystem;
    using UnityEngine;

    public class AnimatorLinkMesh : AnimatorLink
    {
        [SerializeField] Unit unit;
        [SerializeField] SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] MeshAnimator meshAnimator;
        [Space]
        [SerializeField] MeshAnimation Idle, Walk, Attack, AttackRanged, RaiseShield, Death, Knockback;
        private void Start()
        {
            base.OnStart();
        }
        private void OnEnable()
        {
            if (AnimatorLinkLODManager.instance != null)
                AnimatorLinkLODManager.instance.Register(this);
        }
        private void OnDisable()
        {
            if (AnimatorLinkLODManager.instance != null)
                AnimatorLinkLODManager.instance.Unregister(this);
        }
        public override void EnableAnimator()
        {
            if (!IsAnimator) animator.Update(0f);

            IsAnimator = true;
            meshAnimator.SetActiveForInstancing(false);

            unit.SwitchRangedAnimatorGPUParent(true);
            unit.SwitchMeleeAnimatorGPUParent(true);
            unit.SwitchShieldAnimatorGPUParent(true);
            unit.SwitchSaddleAnimatorGPUParent(true);

            skinnedMeshRenderer.enabled = true;
            animator.enabled = true;
            meshAnimator.gameObject.SetActive(false);
            SetAttack(false);

       

        }
        public override void DisableAnimator()
        {
            if (IsAnimator) meshAnimator.ForceUpdateMatrix();

            IsAnimator = false;
            meshAnimator.SetActiveForInstancing(true);

            unit.SwitchRangedAnimatorGPUParent(false);
            unit.SwitchMeleeAnimatorGPUParent(false);
            unit.SwitchShieldAnimatorGPUParent(false);
            unit.SwitchSaddleAnimatorGPUParent(false);

            skinnedMeshRenderer.enabled = false;
            animator.enabled = false;
            meshAnimator.gameObject.SetActive(true);
        }
        public override void SetBlend(float value)
        {
            if (IsAnimator)
                base.SetBlend(value);
            else
            {
                var anim = meshAnimator.GetCurrentAnimation();
                if (anim == Death)
                    return;

                if ((anim == Attack || anim == AttackRanged || anim == Knockback || anim == RaiseShield) && meshAnimator.IsPlaying)
                {
                    return;
                }

                if (value < 0.1 && anim != Idle)
                {
                    meshAnimator.Play(Idle);
                }
                else if (value > 0.1 && anim != Walk)
                {
                    meshAnimator.Play(Walk);
                }
            }
        }
        public override void SetBlock(bool value)
        {
            if (IsAnimator)
                base.SetBlock(value);
            else if (meshAnimator.GetCurrentAnimation() != RaiseShield && value)
                meshAnimator.Play(RaiseShield);
        }
        public override void SetAttack(bool value)
        {
            if (IsAnimator)
                base.SetAttack(value);
            else if (value == true && meshAnimator.GetCurrentAnimation() != Death)
            {
                meshAnimator.Play(Attack);
            }
        }
        public override void SetAttackRanged(bool value)
        {
            if (IsAnimator)
                base.SetAttackRanged(value);
            else
            {
                if (value == true && meshAnimator.GetCurrentAnimation() != Death)
                    meshAnimator.Play(AttackRanged);
                else if (value == false && meshAnimator.GetCurrentAnimation() == AttackRanged)
                    meshAnimator.Play(Idle);
            }
        }
        public override void SetDeath()
        {
            base.SetDeath();
            if (!IsAnimator)
            {
                SpawnCorpse();
                MeshInstancingManager.Instance.UnregisterAnimator(meshAnimator);
                meshAnimator.SetActiveForInstancing(false);
                meshAnimator.enabled = false;

                RaycastHit hit;
                Vector3 origin = meshAnimator.transform.position;
                Vector3 direction = Vector3.down;

                if (Physics.Raycast(origin, direction, out hit, Mathf.Infinity, FormationController.instance.groundMask))
                {

                    meshAnimator.transform.position = hit.point;
                }

            }
        }
        public (Mesh mesh, Material material) GetCorpseData()
        {
            if (Death != null && Death.frames.Length > 0)
            {
                Mesh deathFrame = Death.frames[Death.frames.Length - 1];
                Material material = meshAnimator.CharacterMaterial;
                return (deathFrame, material);
            }
            return (null, null);
        }
        private void SpawnCorpse()
        {
            if (Death != null)
                meshAnimator.SnapToFrame(Death, Death.frames.Length - 1);

            var (corpseMesh, corpseMaterial) = this.GetCorpseData();
            meshAnimator.targetMeshFilter.sharedMesh = corpseMesh;
            meshAnimator.targetMeshFilter.GetComponent<MeshRenderer>().enabled = true;
            meshAnimator.gameObject.isStatic = true;
        }

        public override void SetKnockback(bool value)
        {
            if (animator)
                base.SetKnockback(value);
            else
            {
                if (value == true && meshAnimator.GetCurrentAnimation() != Knockback && meshAnimator.GetCurrentAnimation() != Death)
                    meshAnimator.Play(Knockback);
                else if (value == false && meshAnimator.GetCurrentAnimation() == Knockback)
                    meshAnimator.Play(Idle);
            }
        }
        public override void SetSwitchMelee(bool value)
        {
            if (IsAnimator)
                base.SetSwitchMelee(value);
            else
            {
                if (unit.RangedWeapon != null && unit.RangedWeapon.gameObject != null)
                    unit.RangedWeapon.gameObject.SetActive(false);

                if (unit.MeleeWeapon != null)
                    unit.MeleeWeapon.gameObject.SetActive(true);
            }

        }
        public override void SetSwitchRanged(bool value)
        {
            if (IsAnimator)
                base.SetSwitchRanged(value);
            else
            {
                if (unit.RangedWeapon != null && unit.RangedWeapon.gameObject != null)
                    unit.RangedWeapon.gameObject.SetActive(true);

                if (unit.MeleeWeapon != null)
                    unit.MeleeWeapon.gameObject.SetActive(false);
            }
        }
        public MeshAnimator GetMeshAnimator()
        {
            return meshAnimator;
        }
    }
}