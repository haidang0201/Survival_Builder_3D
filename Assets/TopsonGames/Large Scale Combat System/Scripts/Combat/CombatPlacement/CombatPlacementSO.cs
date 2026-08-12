using UnityEngine;

namespace TopsonGames
{
    public abstract class CombatPlacementSO : ScriptableObject, ICombatPlacement
    {
        public abstract void TickUpdateEngagement(Formation formation, Formation target);
    }
}