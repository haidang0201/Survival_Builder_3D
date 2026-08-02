namespace TopsonGames
{
    using System.Collections.Generic;
    using TopsonGames.AI;
    using UnityEngine;

    public interface ITactic
    {
        public float Evaluate(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations);
        public void Execute(AICommander commander, List<Formation> allEnemies, List<Formation> availableFormations);
    }
}
