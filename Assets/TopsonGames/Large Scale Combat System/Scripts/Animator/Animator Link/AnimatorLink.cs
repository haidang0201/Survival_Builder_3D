namespace TopsonGames
{
    using UnityEngine;

    public class AnimatorLink : MonoBehaviour
    {
        public Animator animator;

        int blend;
        int idle;
        int block;
        int attack;
        int death;
        int engaged;
        int attackrandomizer;
        int switchranged;
        int switchmelee;
        int attackranged;
        int knockback;
        int rowinformation;

        [HideInInspector]
        public bool IsAnimator;

        private void Awake()
        {
           OnStart();
        }
        private void OnEnable()
        {
            if(AnimatorLinkLODManager.instance != null)
                AnimatorLinkLODManager.instance.Register(this);
        }
        private void OnDisable()
        {
            if (AnimatorLinkLODManager.instance != null)
                AnimatorLinkLODManager.instance.Unregister(this);
        }
        public virtual void OnStart()
        {
            blend = Animator.StringToHash("Blend");
            idle = Animator.StringToHash("Idle");
            block = Animator.StringToHash("Block");
            attack = Animator.StringToHash("Attack");
            death = Animator.StringToHash("Death");
            engaged = Animator.StringToHash("Engaged");
            attackrandomizer = Animator.StringToHash("AttackRandomizer");
            switchranged = Animator.StringToHash("SwitchRanged");
            switchmelee = Animator.StringToHash("SwitchMelee");
            attackranged = Animator.StringToHash("AttackRanged");
            knockback = Animator.StringToHash("Knockback");
            rowinformation = Animator.StringToHash("RowInFormation");
        }
        public virtual void SetAnimatorSpeed(float speed)
        {
            animator.speed = speed;
        }
        public virtual void SetIdle(float value)
        {
            animator.SetFloat(idle, Mathf.Lerp(animator.GetFloat(idle), value, Time.deltaTime * 10));
        }
        public virtual void SetBlend(float value)
        {
            animator.SetFloat(blend, Mathf.Lerp(animator.GetFloat(blend), value, Time.deltaTime * 10));
        }
        public virtual void SetAttackRandomizer(int value)
        {
            animator.SetInteger(attackrandomizer, value);
        }
        public virtual void SetBlock(bool value)
        {
            animator.SetBool(block, value);
        }
        public virtual void SetAttack(bool value)
        {
            animator.SetBool(attack, value);
        }
        public virtual void SetAttackRanged(bool value)
        {
            if (value) animator.SetTrigger(attackranged);
            else animator.ResetTrigger(attackranged);
        }
        public virtual void SetDeath()
        {
            animator.SetBool(death, true);
        }
        public virtual void SetEngaged(bool value)
        {
            animator.SetBool(engaged, value);
        }
        public virtual void SetSwitchMelee(bool value)
        {
            if (value) animator.SetTrigger(switchmelee);
            else animator.ResetTrigger(switchmelee);
        }
        public virtual void SetSwitchRanged(bool value)
        {
            if (value) animator.SetTrigger(switchranged);
            else animator.ResetTrigger(switchranged);
        }
        public virtual void SetKnockback(bool value)
        {
            if(value) animator.SetTrigger(knockback);
            else animator.ResetTrigger(knockback);
        }
        public virtual void SetRowInFormation(int value)
        {
            animator.SetInteger(rowinformation, value);
        }

        #region Animator Instancing calls

        public virtual void EnableAnimator()
        {
            // Switch Animation systems here
            IsAnimator = true;
        }
        public virtual void DisableAnimator()
        {
            // Switch Animation systems here
            IsAnimator = false;
        }
        private void OnBecameInvisible()
        {
            DisableAnimator();
        }
        #endregion
    }

}