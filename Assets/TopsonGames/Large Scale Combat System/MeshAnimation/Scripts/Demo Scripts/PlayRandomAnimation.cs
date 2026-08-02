using UnityEngine;

namespace TopsonGames.MeshAnimationSystem.Demo
{
    public class PlayRandomAnimation : MonoBehaviour
    {
        [Tooltip("The MeshAnimator component to control. Will be found on this GameObject if left empty.")]
        [SerializeField] private MeshAnimator myAnimator;

        [Tooltip("The string name of the first animation to play (e.g., 'Idle'). Must match the 'Clip Name' in the MeshAnimation asset.")]
        [SerializeField] private string firstAnimationName = "Idle";

        [Tooltip("The string name of the second animation to play (e.g., 'Walk'). Must match the 'Clip Name' in the MeshAnimation asset.")]
        [SerializeField] private string secondAnimationName = "Walk";

        private bool isFirstAnimation = true;
        private float timer = 5f;

        void Start()
        {
            if (!myAnimator)
            {
                myAnimator = GetComponent<MeshAnimator>();
            }
            timer = Random.Range(1f, 11f);
        }

        private void Update()
        {
            if (myAnimator == null) return;

            timer -= Time.deltaTime;
            if (timer < 0)
            {
                if (isFirstAnimation)
                {
                    myAnimator.Play(firstAnimationName);
                }
                else
                {
                    myAnimator.Play(secondAnimationName);
                }
                isFirstAnimation = !isFirstAnimation;
                timer = Random.Range(3f, 8f); 
            }
        }
    }
}