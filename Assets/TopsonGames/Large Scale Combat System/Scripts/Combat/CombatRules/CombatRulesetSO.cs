namespace TopsonGames
{
    using UnityEngine;

    public abstract class CombatRulesetSO : ScriptableObject
    {
        [TextArea]
        public string rulesetDescription;

        public abstract float ProcessIncomingDamage(Unit unit, Unit attacker, float rawDamage, bool shieldHit, CombatBehaviourSO behaviour);
    }
}