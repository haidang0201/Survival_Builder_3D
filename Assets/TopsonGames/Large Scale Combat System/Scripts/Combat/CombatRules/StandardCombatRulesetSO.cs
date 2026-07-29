namespace TopsonGames
{
    using Unity.VisualScripting.Antlr3.Runtime.Tree;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CreateAssetMenu(fileName = "StandardCombatRuleset", menuName = "TopsonGames/Combat Rulesets/Standard HP System")]
    public class StandardCombatRulesetSO : CombatRulesetSO
    {
        [Header("Erklärung für den Inspector")]
        [TextArea(10, 15)]
        public string calculationExplanation =
            "STANDARD HP SYSTEM (Modern Total War Style):\n\n" +
            "Calculates normal damage, subtracts it from HP, but offers optional multipliers for tactics.\n" +
            "Set all multipliers to 1 if you want the very simple basic system without tactical bonuses.\n\n" +
            "1. ELEVATION:\n" +
            "- Increases or decreases damage based on the attacker’s elevation.\n\n" +
            "2. FLANKING (Direction):\n" +
            "- If a unit is attacked from the rear or flank, it cannot use its shield to defend itself.\n" +
            "- Additionally, a damage multiplier for flank hits (e.g. 1.5 for +50% damage) can be set.";

        [Header("Elevation Mechanics")]
        [Tooltip("At what difference in altitude, measured in metres, does the altitude bonus/penalty come into effect??")]
        public float elevationThreshold = 1.0f;

        [Tooltip("Damage multiplier for attacks from above (default: 1.0 = no bonus)")]
        public float highGroundDamageMultiplier = 1.0f;

        [Tooltip("Damage multiplier for attacks from below (default: 1.0 = no penalty)")]
        public float lowGroundDamageMultiplier = 1.0f;

        [Header("Directional Mechanics (Flanking)")]
        [Tooltip("Angle in degrees at which an attack is classified as a 'frontal attack' (shield active)")]
        public float facingAngleThreshold = 60f;

        [Tooltip("Damage multiplier when a unit is flanked or struck from behind(default: 1.0)")]
        public float flankedDamageMultiplier = 1.0f;

        public override float ProcessIncomingDamage(Unit unit, Unit attacker, float rawDamage, bool shieldHit, CombatBehaviourSO behaviour)
        {
            float finalDamage = rawDamage;

            if (attacker != null)
            {
                float heightDifference = attacker.transform.position.y - unit.transform.position.y;
                if (heightDifference > elevationThreshold)
                {
                    finalDamage *= highGroundDamageMultiplier; 
                }
                else if (heightDifference < -elevationThreshold)
                {
                    finalDamage *= lowGroundDamageMultiplier;  
                }
            }

            bool isAttackedFromFront = false;
            if (unit.ActiveCombatBehaviour != null && attacker != null)
            {
                isAttackedFromFront = unit.ActiveCombatBehaviour.IsFacingTarget(unit, attacker.transform, facingAngleThreshold);
            }

            if (!isAttackedFromFront && !shieldHit)
            {
                finalDamage *= flankedDamageMultiplier;
            }
            else
            {
                float blockChance = unit.ActiveUnitData != null ? unit.ActiveUnitData.shieldMeleeBlockChance : 0f;

                if (Random.Range(0f, 1f) < blockChance)
                {
                    if (behaviour != null)
                    {
                        behaviour.OnTakeDamage(0f, unit, attacker, true);
                    }
                    return 0f;
                }
            }

            if (behaviour != null)
            {
                behaviour.OnTakeDamage(finalDamage, unit, attacker, shieldHit);
            }

            return finalDamage;
        }
    }
}