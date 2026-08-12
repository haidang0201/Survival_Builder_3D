namespace TopsonGames
{
    using System.Collections.Generic;
    using UnityEngine;
    using System.Linq;

    [RequireComponent(typeof(Collider))]
    public class FormationCollider : MonoBehaviour
    {
        public BoxCollider detectionCollider;
        [SerializeField] LayerMask ColliderMask;

        [Tooltip("Opponents higher or lower than this value are ignored (On Wall / On Groumnd).")]
        public float maxEngagementHeightDifference = 3.0f;

        private List<EngagementTrigger> triggersInCollider = new List<EngagementTrigger>();
        [HideInInspector]
        public Formation parentFormation;

        public void SetUp(Formation formation)
        {
            parentFormation = formation;
        }

        public void RefreshDetectedTriggers()
        {
            if (parentFormation == null || detectionCollider == null) return;

            triggersInCollider.Clear();
            Bounds colliderBounds = detectionCollider.bounds;

            Collider[] hitColliders = Physics.OverlapBox(colliderBounds.center, colliderBounds.extents, transform.rotation, ColliderMask);

            foreach (Collider hitCollider in hitColliders)
            {
                if (hitCollider.transform.IsChildOf(this.transform.parent)) continue;

                float heightDifference = Mathf.Abs(transform.position.y - hitCollider.transform.position.y);

                if (heightDifference > maxEngagementHeightDifference)
                {
                    continue; // Opponent is on a different level (wall/floor) -> Ignore!
                }

                EngagementTrigger hitTrigger = hitCollider.GetComponent<EngagementTrigger>();

                if (hitTrigger != null && hitTrigger.TeamID != parentFormation.TeamID)
                {
                    triggersInCollider.Add(hitTrigger);
                }
            }
        }

        public List<EngagementTrigger> GetDetectedTriggers()
        {
            return triggersInCollider;
        }
    }
}