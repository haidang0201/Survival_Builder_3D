namespace TopsonGames
{

    using UnityEngine;

    public class RelayAnimationEventsMesh : MonoBehaviour
    {
        [SerializeField] Unit unit;
        [SerializeField] UnitAudio unitAudio;

        public void OnAttack(int isRanged)
        {
            // 1 for ranged
            bool isRangedBool = (isRanged == 1);
            unit.Attack(isRangedBool);
            if (unitAudio) unitAudio.PlayAttackSwingSound();

        }
        public void OnEnableMeleeWeapon()
        {
            unit.RangedWeapon.gameObject.SetActive(false);
            unit.MeleeWeapon.gameObject.SetActive(true);
        }
        public void OnEnableRangedWeapon()
        {
            unit.RangedWeapon.gameObject.SetActive(true);
            unit.MeleeWeapon.gameObject.SetActive(false);
        }
        public void OnEnableShield()
        {
            if (unit.shieldCollider)
                unit.shieldCollider.enabled = true;
        }
        public void OnDisableShield()
        {
            if (unit.shieldCollider)
                unit.shieldCollider.enabled = false;
        }

        public void OnWalk()
        {
            if (unitAudio)
                unitAudio.PlayFootstepSound();
        }

        public void OnSwing()
        {
            if (unitAudio)
                unitAudio.PlayAttackSwingSound();
        }
        public void OnDeath()
        {
            if (unitAudio)
                unitAudio.PlayDeathSound();
        }
        public void OnCheer()
        {
            if (unitAudio)
                unitAudio.PlayCheerSound();
        }
    }

}