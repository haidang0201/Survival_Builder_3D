namespace TopsonGames
{
    using UnityEngine;

    /// <summary>
    /// Rome: Total War 1 "1-HP" combat system – faithfully recreated.
    ///
    /// CORE: TWO separate rolls per strike (not one!):
    ///   1) CONNECT  = baseConnect * growth^(Attack - Defence), clamped.  (exponential, low base)
    ///   2) LETHALITY = separate roll per weapon. Only if BOTH succeed -> 1 hit point lost.
    ///
    /// As a result, most hits do NOT kill -> a more drawn-out process as in the original,
    /// the battle is decided by morale, not by sheer kills.
    ///
    /// Flank: Target loses shield + Defence skill (armour remains) -> Kill chance
    /// increases organically along the curve, rather than arbitrarily halving the armour.
    /// </summary>
    [CreateAssetMenu(fileName = "RomeOneCombatRuleset", menuName = "TopsonGames/Combat Rulesets/Rome One 1-HP System")]
    public class RomeOneHPCombatRulesetSO : CombatRulesetSO
    {
        [Header("Erklaerung")]
        [TextArea(6, 20)]
        public string calculationExplanation =
            "RTW1 ZWEI-TORE-MODELL:\n" +
            "1) CONNECT = baseConnectChance * connectGrowthPerPoint^(Attack - Defence).\n" +
            "   Low base value + exponential growth -> hardly any kills when the stats are equal, but a steep increase when you have the advantage.\n" +
            "2) LETHALITY = separate roll per weapon (sword high, arrow low).\n" +
            "   Only if both Connect AND Lethality succeed -> 1 hit point lost.\n\n" +
            "TUNING: Too lethal? Reduce baseConnectChance and/or meleeLethality.\n" +
            "Note: Kill rate = chance per strike * strike frequency (cooldown in CombatBehaviour)!";

        [Header("Connect-Throw (Attack vs Defense)")]
        [Tooltip("Connect chance when tied (Attack == Defence). RTW1 value: ~0.08–0.15.")]
        [Range(0.01f, 0.5f)] public float baseConnectChance = 0.10f;
        [Tooltip("Factor per point (Attack-Defence). 1.2 = +20% per point (RTW1 melee), 1.1 = tougher.")]
        public float connectGrowthPerPoint = 1.16f;
        [Tooltip("Limited (Attack-Defence), so that a model never reaches 0% or 100%.")]
        public float attackDefenseDiffClamp = 20f;
        [Range(0f, 0.2f)] public float minConnectChance = 0.01f;
        [Range(0.5f, 1f)] public float maxConnectChance = 0.95f;

        [Header("Lethality (Toeten vs Niederschlagen)")]
        [Tooltip("Fallback melee lethality, if the weapon does not have its own (UnitData.meleeWeaponLethality <= 0).")]
        [Range(0f, 1f)] public float meleeLethalityFallback = 0.40f;
        [Tooltip("Fallback lethality: Ranged combat. RTW1: Arrows rarely kill -> keep this low.")]
        [Range(0f, 1f)] public float rangedLethalityFallback = 0.25f;

        [Header("Hitpoints")]
        [Tooltip("ON: a fatal hit removes the ENTIRE bar (each unit = effectively 1 HP, regardless of the HP value). " +
                 "OFF: only 'hitPointDamage' is deducted -> true multi-HP (set HP as number of hit points: 1 normal, 5 for an elephant...).")]
        public bool treatAllUnitsAsSingleHitpoint = true;
        [Tooltip("Only when treatAllUnitsAsSingleHitpoint = OFF: Damage per lethal hit (1 hit point).")]
        public float hitPointDamage = 1f;

        [Header("Ranged combat")]
        [Tooltip("Converts a projectile’s raw damage (already adjusted for armour) into a chance to connect. Higher = less lethal.")]
        public float rangedDamageScaling = 18f;

        [Header("Armour / Shield / Facing")]
        [Tooltip("Angle (degrees) at which an attack is considered a frontal attack (shield + defence skill active).")]
        public float facingAngleThreshold = 60f;
        [Tooltip("Converting shieldMeleeBlockChance (0–1) to a defence point value.")]
        public float shieldDefenseWeight = 6f;

        [Header("Charge & Height (added to Attack)")]
        [Tooltip("AAttack bonus during the active charge window (additive, RTW1-style).")]
        public float chargeAttackBonus = 4f;
        public float elevationThreshold = 1.0f;
        [Tooltip("Attack bonus from above / penalty from below (additive).")]
        public float highGroundAttackBonus = 2f;
        public float lowGroundAttackMalus = 2f;

        [Header("Knockback in Survival")]
        public float knockbackForce = 0.3f;
        public float knockbackSampleRange = 1.0f;
        public float knockbackDuration = 0.25f;

        public override float ProcessIncomingDamage(Unit unit, Unit attacker, float rawDamage, bool shieldHit, CombatBehaviourSO behaviour)
        {
            if (unit == null || unit.ActiveUnitData == null) return rawDamage;

            // ================= RANGED COMBAT (no direct attacker) =================
            if (attacker == null)
            {
                float connect = Mathf.Clamp01(rawDamage / Mathf.Max(0.001f, rangedDamageScaling));
                float lethality = rangedLethalityFallback;
                return Resolve(unit, null, behaviour, connect, lethality, shieldHit);
            }

            if (attacker.ActiveUnitData == null) return rawDamage;

            // ================= CLOSE COMBAT =================
            var atkData = attacker.ActiveUnitData;
            var defData = unit.ActiveUnitData;

            // --- ATTACK side ---
            float attack = MeleeAttackSkill(atkData);

            if (attacker.GetFormation() != null && attacker.GetFormation().IsChargeBonusActive)
                attack += chargeAttackBonus;

            float heightDiff = attacker.transform.position.y - unit.transform.position.y;
            if (heightDiff > elevationThreshold) attack += highGroundAttackBonus;
            else if (heightDiff < -elevationThreshold) attack -= lowGroundAttackMalus;

            // --- DEFENCE side ---
            bool isFront = false;
            if (unit.ActiveCombatBehaviour != null)
                isFront = unit.ActiveCombatBehaviour.IsFacingTarget(unit, attacker.transform, facingAngleThreshold);

            float effectiveArmour = Mathf.Max(0f, defData.Armor - atkData.armorPenetration);
            float defense = effectiveArmour;

            if (isFront || shieldHit)
            {
                defense += MeleeDefenseSkill(defData);
                defense += defData.shieldMeleeBlockChance * shieldDefenseWeight;
            }
            float diff = Mathf.Clamp(attack - defense, -attackDefenseDiffClamp, attackDefenseDiffClamp);
            float connectChance = baseConnectChance * Mathf.Pow(connectGrowthPerPoint, diff);
            connectChance = Mathf.Clamp(connectChance, minConnectChance, maxConnectChance);

            float meleeLethality = MeleeWeaponLethality(atkData);

            return Resolve(unit, attacker, behaviour, connectChance, meleeLethality, shieldHit);
        }

        /// <summary>Two-goal resolution: Connect roll, then Lethality roll. Otherwise, Knockback.</summary>
        private float Resolve(Unit unit, Unit attacker, CombatBehaviourSO behaviour, float connectChance, float lethality, bool shieldHit)
        {
            bool connected = Random.value <= connectChance;
            bool lethal = connected && Random.value <= lethality;

            float dealt = 0f;

            if (lethal)
            {
                dealt = treatAllUnitsAsSingleHitpoint
                    ? unit.GetCurrentHealth()                                   // effectively 1 HP: all gone
                    : Mathf.Min(hitPointDamage, unit.GetCurrentHealth());       // true multi-HP: 1 hit point
            }
            else if (attacker != null && !unit.hasAppliedKnockback)
            {
                // Connection/strike failed -> parry/flinch -> slight recoil (melee only).
                Vector3 pushDir = (unit.transform.position - attacker.transform.position).normalized;
                unit.ApplyKnockback(pushDir, knockbackForce, knockbackSampleRange, knockbackDuration);
                unit.hasAppliedKnockback = true;
            }

            if (behaviour != null)
                behaviour.OnTakeDamage(dealt, unit, attacker, shieldHit);

            return dealt;
        }

        private float MeleeAttackSkill(UnitDataSO d)
            => d.meleeAttackSkill > 0f ? d.meleeAttackSkill : d.baseDamage;

        private float MeleeDefenseSkill(UnitDataSO d)
            => d.meleeDefenseSkill; // 0  (only armour and shields count then)

        private float MeleeWeaponLethality(UnitDataSO d)
            => d.meleeWeaponLethality > 0f ? d.meleeWeaponLethality : meleeLethalityFallback;
    }
}
