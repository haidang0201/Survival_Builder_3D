namespace TopsonGames
{
    using System.Collections.Generic;
    using UnityEngine;

    public class SpawnPoint : MonoBehaviour
    {
        [Tooltip("If your Formation should be rotatet 180 Degrees on the Y-Axis. Useful if it is looking in the wrong direction")]
        public bool inverseForwardRotation = true;
        [Tooltip("Allowed Unit type that can spawn here")]
        public List<UnitTypeSO> allowedUnitTypes;

    }
}
