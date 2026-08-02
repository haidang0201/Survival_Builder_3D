namespace TopsonGames
{
    using UnityEngine;

    public interface IVisualizer
    {
        void TickVisualize(CombatBehaviourSO combatBehaviourSO, Formation formation, LineRenderer lineRenderer);
    }
}
