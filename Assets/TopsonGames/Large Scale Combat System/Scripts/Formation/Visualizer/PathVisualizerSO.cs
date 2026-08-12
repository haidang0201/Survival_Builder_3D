namespace TopsonGames
{
    using UnityEngine;
    using UnityEngine.AI; 

    [CreateAssetMenu(fileName = "PathVisualizerSO", menuName = "TopsonGames/Visualizer/PathVisualizerSO")]
    public class PathVisualizerSO : VisualizerSO
    {
        [Tooltip("Ground Offset")]
        public float lineHeightOffset = 0.2f;
        public LayerMask groundLayer;       

        public override void TickVisualize(CombatBehaviourSO combatBehaviourSO, Formation formation, LineRenderer lineRenderer)
        {
            if (lineRenderer == null || formation == null)
                return;

            NavMeshPath path = new NavMeshPath();

            Vector3 startPos = formation.CalculateUnitCenter();
            Vector3 targetPos = formation.CalculateWaypointMarkerCenter();

            startPos = SampleGround(startPos);
            targetPos = SampleGround(targetPos);

            if (NavMesh.CalculatePath(startPos, targetPos, NavMesh.AllAreas, path))
            {
                if (path.status == NavMeshPathStatus.PathComplete && path.corners.Length >= 2)
                {
                    lineRenderer.positionCount = path.corners.Length;
                    Vector3[] smoothedCorners = new Vector3[path.corners.Length];

                    for (int i = 0; i < path.corners.Length; i++)
                    {
                        smoothedCorners[i] = SampleGround(path.corners[i]);
                    }
                    lineRenderer.SetPositions(smoothedCorners);
                    lineRenderer.enabled = true; 
                }
                else
                {
                    lineRenderer.positionCount = 0;
                    lineRenderer.enabled = false;
                }
            }
            else
            {
                lineRenderer.positionCount = 0;
                lineRenderer.enabled = false;
            }
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