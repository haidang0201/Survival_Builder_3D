namespace TopsonGames.AI.Grid
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "BattleGridUnitSettings", menuName = "TopsonGames/AI/Grid/Battle Grid Unit Settings")]
    public class BattleGridUnitSettingsSO : ScriptableObject
    {
        [System.Serializable]
        public class ThreatSetting
        {
            [Tooltip("The enemies(danger), e.g.spearmen or archers.")]
            public UnitTypeSO[] threatTypes;
            public float dangerRadiusMultiplier = 1.0f;
            public float dangerValueMultiplier = 1.0f;
        }

        [System.Serializable]
        public class ViewerGridSetting
        {
            [Tooltip("The unit itself, which is searching for a route (e.g. cavalry).")]
            public UnitTypeSO viewerType;

            [Header("Default values")]
            [Tooltip("Used against all enemies NOT specifically listed below.")]
            public float defaultRadiusMultiplier = 1.0f;
            public float defaultValueMultiplier = 1.0f;

            [Header("Specific enemies")]
            [Tooltip("Here you can define exceptions (e.g. giant spearmen, small archers).")]
            public ThreatSetting[] specificThreats;
        }

        [Header("How does a unit perceive the battlefield ? ")]
        public List<ViewerGridSetting> viewerSettings = new List<ViewerGridSetting>();

        public float GetRadiusMultiplier(UnitTypeSO viewer, UnitTypeSO threat)
        {
            if (viewer == null || threat == null) return 1.0f;

            var setting = viewerSettings.Find(s => s.viewerType == viewer);
            if (setting != null)
            {
                if (setting.specificThreats != null)
                {
                    foreach (var t in setting.specificThreats)
                    {
                        if (System.Array.Exists(t.threatTypes, type => type == threat))
                            return t.dangerRadiusMultiplier;
                    }
                }
                return setting.defaultRadiusMultiplier; 
            }
            return 1.0f;
        }

        public float GetValueMultiplier(UnitTypeSO viewer, UnitTypeSO threat)
        {
            if (viewer == null || threat == null) return 1.0f;

            var setting = viewerSettings.Find(s => s.viewerType == viewer);
            if (setting != null)
            {
                if (setting.specificThreats != null)
                {
                    foreach (var t in setting.specificThreats)
                    {
                        if (System.Array.Exists(t.threatTypes, type => type == threat))
                            return t.dangerValueMultiplier;
                    }
                }
                return setting.defaultValueMultiplier;
            }
            return 1.0f;
        }
    }
}