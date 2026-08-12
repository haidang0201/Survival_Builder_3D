using TopsonGames.MeshAnimationSystem;
using UnityEngine;

namespace TopsonGames.MeshAnimationSystem
{
    public class AttachmentPoint : MonoBehaviour
    {
        public MeshAnimator targetAnimator;
        public string boneName;

        [Header("Offsets (local in the bone space)")]
        public Vector3 positionOffset;
        public Vector3 rotationOffset;

        void OnEnable()
        {
            if (MeshInstancingManager.Instance != null)
                MeshInstancingManager.Instance.RegisterAttachment(this);
        }

        void OnDisable()
        {
            if (MeshInstancingManager.Instance != null)
                MeshInstancingManager.Instance.UnregisterAttachment(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.05f);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 0.2f);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.right * 0.2f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.up * 0.2f);
        }
    }
}