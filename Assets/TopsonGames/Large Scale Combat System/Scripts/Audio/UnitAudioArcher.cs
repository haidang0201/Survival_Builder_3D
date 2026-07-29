namespace TopsonGames
{
    using UnityEngine;

    public class UnitAudioArcher : UnitAudio
    {
        public AudioDefinitionSO arrowSound;

        public void OnShoot()
        {
            if (arrowSound != null)
                AudioManager.Instance.PlaySound(arrowSound, transform.position);
        }
    }

}