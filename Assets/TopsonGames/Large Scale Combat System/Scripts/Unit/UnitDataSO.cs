using System.Collections.Generic;
using UnityEngine;

namespace TopsonGames
{
    [CreateAssetMenu(fileName = "UnitDataSO", menuName = "TopsonGames/Unit Data/UnitDataSO")]
    public class UnitDataSO : ScriptableObject
    {
        [Header("General Info")]
        public Sprite unitIcon;
        public string description = "A basic unit.";
        public GameObject formationPrefab;
        [Tooltip("Unit costs for your own code / updates - no function yet")]
        public int Cost;

        [Header("Combat Stats")]
        public float maxHealth = 100f;
        public float Armor = 5f;
        public float baseDamage = 10f;
        public float armorPenetration = 0f; // Armor-piercing damage
        public float chargeDamageMultiplier = 1.5f;
        public float chargeDamageDuration = 5f;
        [Range(0f, 1f)]
        public float shieldMeleeBlockChance = 0f;
        [Range(0f, 1f)]
        public float shieldArrowBlockChance = -1f;
        [Tooltip("0 when no ranged weapon")]
        public int Arrows = 0;
        public bool canRecieveKnockback = true;
        public UnitTypeSO unitType;
        public List<TargetType> attackableTargetTypes;

        [Header("Morale Settings")]
        public float baseMorale = 50f;
        public bool isGeneral = false;
        [Tooltip("Morale reduction per 10% HP lost")]
        public float moralePenaltyPerHealthLoss = 5f;
        [Tooltip("Morale impact on General Death")]
        public float generalDeathImpact = 15;
        [Tooltip("Which effect does this formation have on other (for example general nearby)")]
        public EffectSO[] effectsOnOtherFormations;
        [Tooltip("Radius of affected Formatios")]
        public float effectRadius = 40f;

        [Header("Animations")]
        public int idleAnimations = 1;
        public int attackAnimations = 1;

        [Header("Formation Settings")]
        public int formationWidth = 8;
        public float formationSpacing = 1.7f;
        public float formationMoveSpeed = 3f;
        public float formationTurnSpeed = 5f;
        public float unitRotationSpeed = 8f;

        [Header("Cohesion Settings")]
        [Tooltip("How much do falling units accelerate? Higher = faster.")]
        public float unitCatchUpFactor = 0.5f;
        [Tooltip("How strongly do leading units brake? Higher = stronger.")]
        public float unitSlowDownFactor = 0.1f;
        [Tooltip("Maximum speed bonus (e.g. 1.5 = 150% of normal speed)).")]
        public float maxSpeedMultiplier = 1.5f;
        [Tooltip("Minimum speed (e.g. 0.7 = 70% of normal speed).")]
        public float minSpeedMultiplier = 0.7f;
        public float stoppingDistance = 0.1f;

        [Header("Detection Setting")]
        public float colliderOverhang = 5f;
        public Vector2 maxColliderSize = new Vector2(10f, 10f);
        public float colliderHeight = 10f;
        public float centerSmoothingSpeed = 5f;

        [Header("Behaviour")]
        public CombatBehaviourSO combatBehaviour;
        public CombatPlacementSO placementBehaviour;

        [Header("Debugging")]
        public int Troops;

        [Space]
        [Space]
        [Header("Rome 1 HP System Only")]
        [Tooltip("Attack Skill (RTW1). 0 = Fall back on base damage.")]
        public float meleeAttackSkill = 0f;
        [Tooltip("Defence Skill (RTW1) – only counts when facing the opponent head-on; a flanking manoeuvre negates it.")]
        public float meleeDefenseSkill = 0f;
        [Tooltip("Lethality of the melee weapon (0–1). Sword ~0.4–0.5, spear lower. 0 = ruleset fallback.")]
        [Range(0f, 1f)] public float meleeWeaponLethality = 0f;
    }
}