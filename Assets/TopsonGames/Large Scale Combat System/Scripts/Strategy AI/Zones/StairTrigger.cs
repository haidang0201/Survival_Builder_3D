namespace TopsonGames
{
    using Unity.VisualScripting;
    using UnityEngine;
    using UnityEngine.AI;

    public class StairTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Unit>(out var unit))
            {
                unit.agent.radius = unit.agent.radius / 4;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<Unit>(out var unit))
            {
                unit.agent.radius = unit.navMeshRadius;
            }
        }
    }
  

}