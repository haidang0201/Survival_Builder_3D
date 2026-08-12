using UnityEngine;

namespace TopsonGames
{
    public interface ICombatPlacement
    {
        void TickUpdateEngagement(Formation formation, Formation target);
    }
}