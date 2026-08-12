namespace TopsonGames
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AI;

    [CreateAssetMenu(fileName = "HorseArcherDetectionVisualizerSO", menuName = "TopsonGames/Visualizer/HorseArcherDetectionVisualizerSO")]
    public class HorseArcherDetectionVisualizerSO : VisualizerSO
    {
        [Tooltip("For Smooth lines")]
        public float segments = 7;
        [Tooltip("Ground Offset")]
        public float lineHeightOffset = 0.2f;
        public LayerMask groundLayer;

        public override void TickVisualize(CombatBehaviourSO combatBehaviour, Formation formation, LineRenderer lineRenderer)
        {
            if (formation == null || formation.GetUnits().Count == 0 || (formation.UnitData.combatBehaviour is not HorseArcherCombatSO && formation.UnitData.combatBehaviour is not HorseArcherSingleCombatSO))
            {
                lineRenderer.enabled = false;
                return;
            }

            int points = Mathf.Max(3, Mathf.RoundToInt(segments));
            lineRenderer.positionCount = points + 1;

            float angleStep = 360f / points;

            for (int i = 0; i <= points; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 localPos = Vector3.zero;
                if (formation.UnitData.combatBehaviour is HorseArcherCombatSO)
                {
                    localPos = new Vector3(
                      Mathf.Cos(angle) * (formation.UnitData.combatBehaviour as HorseArcherCombatSO).archerDetectionRange,
                      0f,
                      Mathf.Sin(angle) * (formation.UnitData.combatBehaviour as HorseArcherCombatSO).archerDetectionRange
                    );
                }
                else
                {

                    localPos = new Vector3(
                    Mathf.Cos(angle) * (formation.UnitData.combatBehaviour as HorseArcherSingleCombatSO).archerDetectionRange,
                    0f,
                    Mathf.Sin(angle) * (formation.UnitData.combatBehaviour as HorseArcherSingleCombatSO).archerDetectionRange
                    );

                }
                Vector3 worldPos = formation.CalculateUnitCenter() + localPos;

                worldPos = SampleGround(worldPos);

                lineRenderer.SetPosition(i, worldPos);

            }
            lineRenderer.enabled = true;

        }
        private Vector3 SampleGround(Vector3 originalPoint)
        {
            Vector3 rayOrigin = new Vector3(originalPoint.x, originalPoint.y + 200f, originalPoint.z);
            RaycastHit hitInfo;

            if (Physics.Raycast(rayOrigin, Vector3.down, out hitInfo, Mathf.Infinity, groundLayer))
            {
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(hitInfo.point, out navHit, 5f, NavMesh.AllAreas))
                {
                    return navHit.position + Vector3.up * lineHeightOffset;
                }
                else
                {
                    return hitInfo.point + Vector3.up * 0.5f;
                }
            }

            return originalPoint;
        }
    }
}