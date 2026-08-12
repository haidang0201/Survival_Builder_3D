namespace TopsonGames
{
    using UnityEngine;
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class MapExit : MonoBehaviour
    {
        [SerializeField]
        bool drawGizmos = true;
        [SerializeField]
        Color gizmoColor = new Color(0f, 1f, 0f, 0.35f);

        private void Start()
        {
            GetComponent<BoxCollider>().isTrigger = true;
            GetComponent<Rigidbody>().isKinematic = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var eg = other.GetComponent<FormationCollider>();
            if (eg != null)
            {
                var u = eg.parentFormation.GetUnits();
                for (int i = u.Count - 1; i >= 0; i--)
                {
                    var unit = u[i];
                    if (unit != null)
                    {
                        unit.HandleDeath(null, true);
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos)
                return;
            BoxCollider box = GetComponent<BoxCollider>();

            Gizmos.color = gizmoColor;

            if (box != null)
            {
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(
                    transform.position + transform.rotation * Vector3.Scale(box.center, transform.lossyScale),
                    transform.rotation,
                    transform.lossyScale
                );

                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawCube(Vector3.zero, box.size);
                Gizmos.DrawWireCube(Vector3.zero, box.size);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}