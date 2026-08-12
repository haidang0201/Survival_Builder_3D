namespace TopsonGames
{
    using UnityEngine;

    public class DrawArmyRectangle : MonoBehaviour
    {
        [SerializeField] UnitDataSO unit;

        private void OnDrawGizmos()
        {
            if(unit)
                Gizmos.DrawWireCube(transform.position, new Vector3(unit.formationWidth, 0.1f, unit.formationWidth));
        }
    }
}
