namespace TopsonGames
{
    using System.Collections.Generic;
    using UnityEngine;

    public class TargetManager : MonoBehaviour
    {
        public static TargetManager instance;

        private Dictionary<int, Dictionary<TargetType, List<DestructibleTarget>>> targetsByType =
            new Dictionary<int, Dictionary<TargetType, List<DestructibleTarget>>>();

        private void Awake()
        {
            if (instance == null)
                instance = this;
            else
                Destroy(this);
        }

        public void Register(DestructibleTarget target)
        {
            int teamID = target.TeamID;
            TargetType type = target.targetType;

            if (!targetsByType.ContainsKey(teamID))
            {
                targetsByType[teamID] = new Dictionary<TargetType, List<DestructibleTarget>>();
            }

            if (!targetsByType[teamID].ContainsKey(type))
            {
                targetsByType[teamID][type] = new List<DestructibleTarget>();
            }

            if (!targetsByType[teamID][type].Contains(target))
            {
                targetsByType[teamID][type].Add(target);
            }
        }

        public void Unregister(DestructibleTarget target)
        {
            int teamID = target.TeamID;
            TargetType type = target.targetType;

            if (targetsByType.ContainsKey(teamID) && targetsByType[teamID].ContainsKey(type))
            {
                targetsByType[teamID][type].Remove(target);
            }
        }

        public void GetPotentialTargets(int targetTeamID, List<TargetType> attackableTypes, List<DestructibleTarget> outputList)
        {
            outputList.Clear();
            if (!targetsByType.ContainsKey(targetTeamID))
            {
                return;
            }

            foreach (var type in attackableTypes)
            {
                if (targetsByType[targetTeamID].TryGetValue(type, out var targets))
                {
                    outputList.AddRange(targets);
                }
            }
        }
    }
}