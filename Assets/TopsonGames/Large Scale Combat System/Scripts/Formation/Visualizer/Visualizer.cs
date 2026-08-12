namespace TopsonGames
{
    using UnityEngine;

    [RequireComponent(typeof(LineRenderer))]
    public class Visualizer : MonoBehaviour
    {
        public VisualizerSO visualizer;
        public LineRenderer lineRenderer;

        private void Start()
        {
            if(!lineRenderer)
                lineRenderer = GetComponent<LineRenderer>();
        }

        public void OnUpdateVisualizer(CombatBehaviourSO combatBehaviour,Formation formation)
        {
            visualizer.TickVisualize(combatBehaviour, formation, lineRenderer);
        }
        public void ShowLine(bool showline)
        {
            lineRenderer.enabled = showline;
        }
    }
}
