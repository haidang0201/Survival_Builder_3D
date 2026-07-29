namespace TopsonGames
{
    using UnityEngine;

    public abstract class VisualizerSO : ScriptableObject, IVisualizer
    {
        public abstract void TickVisualize(CombatBehaviourSO combatBehaviourSO, Formation formation, LineRenderer lineRenderer);
    }

}