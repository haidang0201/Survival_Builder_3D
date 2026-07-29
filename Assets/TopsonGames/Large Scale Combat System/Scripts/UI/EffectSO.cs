namespace TopsonGames
{
    using UnityEngine;
    [CreateAssetMenu(fileName = "EffectSO", menuName = "TopsonGames/UI/Effect")]
    public class EffectSO : ScriptableObject
    {
        public Sprite EffectIcon;
        public EffectType effectType;

        [Tooltip("How much Morale is deducted when this effect is active?")]
        public float moralePenalty = 0f; 

        public void OnAddEffect(Formation formation) { }
        public void OnRemoveEffect(Formation formation) { }
        public void OnUpdateFormation(Formation formation) { }
    }
}