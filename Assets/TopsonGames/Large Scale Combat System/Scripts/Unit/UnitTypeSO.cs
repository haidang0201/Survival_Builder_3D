namespace TopsonGames
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "UnitTypeSO", menuName = "TopsonGames/UnitType/UnitType")]
    public class UnitTypeSO : ScriptableObject
    {
        public Sprite typeIcon;
        [Tooltip("Can this unit be placed on walls/ramparts?")]
        public bool canManWalls = true;
        [Tooltip("For SpawnPoints")]
        public Color gizmosColor;
    }

}