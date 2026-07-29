namespace TopsonGames
{
    using UnityEngine;

    public class EngagementTrigger : MonoBehaviour
    {
        [HideInInspector]
        public int TeamID;
        Formation parentFormation;

        public void Initialize(int teamID, Formation formation = null)
        {
            this.TeamID = teamID;

            if(!parentFormation && formation != null) 
                parentFormation = formation;
        }
        public Formation GetFormation()
        {
            return parentFormation;
        }
    }
}