namespace TopsonGames
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    [CreateAssetMenu(fileName = "DamageModifierSO", menuName = "TopsonGames/DamageModifier/DamageModifier")]
    public class DamageModifierSO : ScriptableObject
    {
        [Header("Combat Ruleset (Plug & Play)")]
        [Tooltip("Zieht hier euer gewünschtes Kampfsystem rein (Standard oder Rome 1)")]
        public CombatRulesetSO activeCombatRuleset;

        [Header("Standard Modifiers")]
        public UnitModifier[] unitModifiers;
        [Tooltip("Minimum Damage that every Unit makes even with high Armor / low armor piercing")]
        public float minimumDamage = 1;
        public float flankingModifier = 1.5f;

        public float GetUnitModifier(UnitTypeSO Attacker, UnitTypeSO Defender)
        {
            if (unitModifiers == null || unitModifiers.Length == 0 || Defender == null || Attacker == null)
                return 1;

            foreach (UnitModifier unitModifier in unitModifiers)
            {
                if (unitModifier.Unit == Attacker)
                {
                    foreach (TypeModifier typeModifier in unitModifier.Units)
                    {
                        if (typeModifier.UnitTypes.Contains(Defender))
                            return typeModifier.Modifier;
                    }
                }
            }
            return 1;
        }

        public float CalculatArmorDamage(float attackerBaseDamage, float attackerArmorPenetration, float defenderArmor)
        {
            float effectiveArmor = Mathf.Max(0f, defenderArmor - attackerArmorPenetration);
            float damageAfterArmor = attackerBaseDamage - effectiveArmor;

            damageAfterArmor = Mathf.Max(minimumDamage, damageAfterArmor);

            return damageAfterArmor;
        }

        public float GetTotalDamage(UnitDataSO Attacker, UnitDataSO Defender)
        {
            if (Defender == null)
                return CalculatArmorDamage(Attacker.baseDamage, Attacker.armorPenetration, 0f);
            else
                return GetUnitModifier(Attacker.unitType, Defender.unitType) * CalculatArmorDamage(Attacker.baseDamage, Attacker.armorPenetration, Defender.Armor);
        }

        public List<UnitTypeSO> GetGoodAgainst(UnitTypeSO unittype)
        {
            var goodAgainstList = new List<UnitTypeSO>();
            if (unitModifiers == null || unittype == null) return goodAgainstList;
            foreach (var unitModifier in unitModifiers)
            {
                if (unitModifier.Unit == unittype)
                {
                    foreach (var typeModifier in unitModifier.Units)
                    {
                        if (typeModifier.Modifier > 1.0f)
                        {
                            goodAgainstList.AddRange(typeModifier.UnitTypes);
                        }
                    }
                    break;
                }
            }
            return goodAgainstList.Distinct().ToList();
        }

        public List<UnitTypeSO> GetBadAgainst(UnitTypeSO unittype)
        {
            var badAgainstList = new List<UnitTypeSO>();
            if (unitModifiers == null || unittype == null) return badAgainstList;
            foreach (var unitModifier in unitModifiers)
            {
                if (unitModifier.Unit == unittype)
                {
                    foreach (var typeModifier in unitModifier.Units)
                    {
                        if (typeModifier.Modifier < 1.0f)
                        {
                            badAgainstList.AddRange(typeModifier.UnitTypes);
                        }
                    }
                    break;
                }
            }
            return badAgainstList.Distinct().ToList();
        }

        public float FlankMultiplier(Quaternion attackerRotation, Quaternion targetRotation)
        {
            Vector3 attackerForward = attackerRotation * Vector3.forward;
            Vector3 targetForward = targetRotation * Vector3.forward;

            float dot = Vector3.Dot(attackerForward, targetForward);
            const float flankThreshold = 0.5f;
            bool flanked = dot < flankThreshold;

            if (flanked) return flankingModifier;
            else return 1;
        }

        public float GetMaxModifierAgainstCategory(UnitTypeSO attacker, List<UnitTypeSO> targetCategory)
        {
            if (unitModifiers == null || attacker == null || targetCategory == null || targetCategory.Count == 0)
                return 1f;

            float maxModifier = 1f;

            foreach (UnitModifier unitMod in unitModifiers)
            {
                if (unitMod.Unit == attacker)
                {
                    foreach (TypeModifier typeMod in unitMod.Units)
                    {
                        foreach (UnitTypeSO targetType in targetCategory)
                        {
                            if (System.Array.Exists(typeMod.UnitTypes, t => t == targetType))
                            {
                                if (typeMod.Modifier > maxModifier)
                                    maxModifier = typeMod.Modifier;
                            }
                        }
                    }
                    break;
                }
            }
            return maxModifier;
        }
    }

    [System.Serializable]
    public class UnitModifier
    {
        public UnitTypeSO Unit;
        public TypeModifier[] Units;
    }

    [System.Serializable]
    public class TypeModifier
    {
        public float Modifier = 1;
        public UnitTypeSO[] UnitTypes;
    }
}