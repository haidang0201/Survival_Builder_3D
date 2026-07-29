using System.Collections.Generic;
using UnityEngine;

namespace TopsonGames.AI
{
    [CreateAssetMenu(fileName = "AIStrategy_Balanced", menuName = "TopsonGames/AI/Strategy")]
    public class AIStrategySO : ScriptableObject
    {
        [Tooltip("The list of tactics that this AI considers. The order can influence the priority.")]
        public List<AITacticSO> Tactics;

        [Tooltip("What percentage of troops (based on the number of formations) should initially be held in reserve? 0 = none, 1 = all.")]
        [Range(0f, 1f)]
        public float reservePercentage = 0.2f;
    }
}