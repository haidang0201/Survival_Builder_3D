namespace TopsonGames
{
    using UnityEngine;
    using UnityEngine.AI;

    [CreateAssetMenu(fileName = "MeleeDetectionVisualizerSO", menuName = "TopsonGames/Visualizer/MeleeDetectionVisualizerSO")]
    public class MeleeDetectionVisualizerSO : VisualizerSO
    {
        [Tooltip("For Smooth lines")]
        public float segmentsPerEdge = 7;
        [Tooltip("Ground Offset")]
        public float lineHeightOffset = 0.2f;
        public LayerMask groundLayer; 

        public override void TickVisualize(CombatBehaviourSO combatBehaviourSO, Formation formation, LineRenderer lineRenderer)
        {
            if (lineRenderer == null || formation == null)
                return;

            BoxCollider boxCollider = formation.GetUnitCenterCollider();
            if (boxCollider == null)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            Transform colliderTransform = boxCollider.transform;
            Vector3 colliderCenter = boxCollider.center;
            Vector3 colliderSize = boxCollider.size;

            Vector3 min = colliderCenter - colliderSize * 0.5f;
            Vector3 max = colliderCenter + colliderSize * 0.5f;

            Vector3[] localCorners = new Vector3[8];
            localCorners[0] = new Vector3(min.x, min.y, min.z);
            localCorners[1] = new Vector3(max.x, min.y, min.z);
            localCorners[2] = new Vector3(max.x, min.y, max.z);
            localCorners[3] = new Vector3(min.x, min.y, max.z);

            Vector3[] worldCornersBottom = new Vector3[4];
            worldCornersBottom[0] = colliderTransform.TransformPoint(localCorners[0]);
            worldCornersBottom[1] = colliderTransform.TransformPoint(localCorners[1]);
            worldCornersBottom[2] = colliderTransform.TransformPoint(localCorners[2]);
            worldCornersBottom[3] = colliderTransform.TransformPoint(localCorners[3]);

            int totalPoints = Mathf.FloorToInt(4 * segmentsPerEdge) + 1;
            lineRenderer.positionCount = totalPoints;
            Vector3[] linePoints = new Vector3[totalPoints];

            int pointIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                Vector3 startPoint = worldCornersBottom[i];
                Vector3 endPoint = worldCornersBottom[(i + 1) % 4];

                for (int j = 0; j < segmentsPerEdge; j++)
                {
                    float t = (float)j / segmentsPerEdge;
                    Vector3 interpolatedPoint = Vector3.Lerp(startPoint, endPoint, t);
                    linePoints[pointIndex++] = SampleGround(interpolatedPoint);
                }
            }
            linePoints[pointIndex] = SampleGround(worldCornersBottom[0]); 

            lineRenderer.SetPositions(linePoints);
        }

        private Vector3 SampleGround(Vector3 originalPoint)
        {
            RaycastHit hitInfo;
            Vector3 rayOrigin = new Vector3(originalPoint.x, originalPoint.y + 1000f, originalPoint.z);

            if (Physics.Raycast(rayOrigin, Vector3.down, out hitInfo, Mathf.Infinity, groundLayer))
            {
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(hitInfo.point, out navHit, 5f, NavMesh.AllAreas)) 
                {
                    return navHit.position + Vector3.up * lineHeightOffset;
                }
                else
                {
                    return hitInfo.point + Vector3.up * 2f; 
                }
            }
            return originalPoint + Vector3.up * 2f;
        }
    }

}