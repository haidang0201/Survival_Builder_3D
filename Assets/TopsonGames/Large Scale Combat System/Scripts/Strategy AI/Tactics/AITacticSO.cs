using System.Collections.Generic;
using UnityEngine;

namespace TopsonGames.AI
{
    public abstract class AITacticSO : ScriptableObject, ITactic
    {
        public abstract float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations);
        public abstract void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations);
    }
}