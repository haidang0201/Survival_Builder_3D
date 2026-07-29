namespace TopsonGames
{
    using UnityEngine;

    public class UnitAudio : MonoBehaviour
    {
        [Header("Sound Definitions")]
        public AudioDefinitionSO footstepSound;
        public AudioDefinitionSO attackSwingSound;
        public AudioDefinitionSO cheerSound;
        public AudioDefinitionSO deathSound;

        public void PlayFootstepSound()
        {
            if(footstepSound != null) 
                AudioManager.Instance.PlaySound(footstepSound, transform.position);
        }

        public void PlayAttackSwingSound()
        {
            if(attackSwingSound != null)
                AudioManager.Instance.PlaySound(attackSwingSound, transform.position);
        }
        public void PlayCheerSound()
        {
            if(cheerSound != null)
                AudioManager.Instance.PlaySound(cheerSound, transform.position);
        }
        public void PlayDeathSound()
        {
            if(deathSound != null)
                AudioManager.Instance.PlaySound(deathSound, transform.position);
        }

    }
}