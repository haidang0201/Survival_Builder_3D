namespace TopsonGames
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Army", menuName = "TopsonGames/Army")]
    public class ArmySO : ScriptableObject
    {
        [Header("Army")]
        public List<ArmyData> armyData;

        
        [ContextMenu("Execute Spawn Army")]
        public void SetToMaximumTroops()
        {
            foreach(var army in armyData)
            {
                if (army != null && army.Unit != null)
                {
                    army.Troops = army.Unit.Troops;
                }
            }
        }
    }

    [System.Serializable]
    public class ArmyData
    {
        public UnitDataSO Unit;
        [Tooltip("Current troops that are alive")]
        public int Troops = 100;
        [Tooltip("In-Game Formation, will be set once the game starts")]
        public Formation inGameFormation;
    }
}