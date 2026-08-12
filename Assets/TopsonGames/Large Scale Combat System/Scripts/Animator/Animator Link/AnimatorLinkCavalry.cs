namespace TopsonGames
{
    using UnityEngine;

    public class AnimatorLinkCavalry : AnimatorLink
    {
        public Animator horseAnimator;
        int horseblend;
        int horsedeath;

        public override void OnStart()
        {
            base.OnStart();
            horseblend = Animator.StringToHash("Blend");
            horsedeath = Animator.StringToHash("Death");
        }
        public virtual void SetHorseAnimatorSpeed(float speed)
        {
            horseAnimator.speed = speed;
        }
        public virtual void SetHorseBlend(float value)
        {
            horseAnimator.SetFloat(horseblend, Mathf.Lerp(horseAnimator.GetFloat(horseblend), value, Time.deltaTime * 10));
        }
        public virtual void SetHorseDeath()
        {
            horseAnimator.SetBool(horsedeath, true);
        }
    }
}