namespace TopsonGames
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AI;

    [CreateAssetMenu(fileName = "MusketeerVisualizerSO", menuName = "TopsonGames/Visualizer/MusketeerVisualizerSO")]
    public class MusketeerVisualizerSO : VisualizerSO
    {
        [Tooltip("For Smooth lines")]
        public float segmentsPerEdge = 7;
        [Tooltip("Ground Offset")]
        public float lineHeightOffset = 0.2f;
        public LayerMask groundLayer;

        public override void TickVisualize(CombatBehaviourSO combatBehaviourSO, Formation formation, LineRenderer lineRenderer)
        {
            if (formation == null || formation.GetUnits().Count == 0 || formation.CurrentCombatState == Formation.CombatState.Melee)
            {
                lineRenderer.enabled = false;
                return;
            }
            var musketeerBehaviour = combatBehaviourSO as MusketeerCombatSO;
            if (musketeerBehaviour == null)
            {
                lineRenderer.enabled = false;
                return;
            }

            Vector3 center = formation.CalculateUnitCenter();
            Vector3 forward = formation.GetUnits()[0].transform.forward.normalized;

            float frontOffset = musketeerBehaviour.archerFrontOffset;
            float detectionRange = musketeerBehaviour.archerDetectionRange;
            float innerAngle = musketeerBehaviour.archerInnerAngle;
            float outerAngle = musketeerBehaviour.archerOuterAngle;

            Vector3 offsetPoint = center + forward * frontOffset;
            float outerLineLength = detectionRange;

            Quaternion leftRotInner = Quaternion.AngleAxis(-innerAngle / 2f, Vector3.up);
            Quaternion rightRotInner = Quaternion.AngleAxis(innerAngle / 2f, Vector3.up);
            Quaternion leftRotOuter = Quaternion.AngleAxis(-outerAngle / 2f, Vector3.up);
            Quaternion rightRotOuter = Quaternion.AngleAxis(outerAngle / 2f, Vector3.up);

            Vector3 innerLeftOriginal = offsetPoint + (leftRotInner * forward).normalized * (0.1f + frontOffset);
            Vector3 innerRightOriginal = offsetPoint + (rightRotInner * forward).normalized * (0.1f + frontOffset);
            Vector3 outerLeftOriginal = offsetPoint + (leftRotOuter * forward).normalized * detectionRange;
            Vector3 outerRightOriginal = offsetPoint + (rightRotOuter * forward).normalized * detectionRange;

            List<Vector3> points = new();

            for (int i = 0; i <= segmentsPerEdge; i++)
            {
                float t = (float)i / segmentsPerEdge;
                points.Add(SampleGround(Vector3.Lerp(innerLeftOriginal, outerLeftOriginal, t)));
            }

            for (int i = 1; i <= segmentsPerEdge; i++)
            {
                float t = (float)i / segmentsPerEdge;
                points.Add(SampleGround(Vector3.Lerp(outerLeftOriginal, outerRightOriginal, t)));
            }

            for (int i = 1; i <= segmentsPerEdge; i++)
            {
                float t = 1f - ((float)i / segmentsPerEdge);
                points.Add(SampleGround(Vector3.Lerp(innerRightOriginal, outerRightOriginal, t)));
            }

            points.Add(SampleGround(innerLeftOriginal));

            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
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