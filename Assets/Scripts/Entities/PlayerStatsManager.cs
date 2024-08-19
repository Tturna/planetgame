using System.Collections.Generic;
using Inventory.Item_SOs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Entities
{
    public class PlayerStatsManager : MonoBehaviour
    {
        // TODO: Try to figure out a better system of defining stats. Maybe there is one, maybe there isn't.
        
        /*
         * Some stats that modify item stats like damage, knockback, crit chance, etc. can have a flat increase and a multiplier.
         * The flat increase is added to the base value of the item, and the sum is then multiplied by the multiplier.
         */
    
        #region Base Stats

        private const float BaseMaxHealth = 1000f;
        private const float BaseHealth = 1000f;
        private const float BaseHealthRegenDelay = 4f;
        private const float BaseHealthRegenRate = 3.34f;
        private const float BaseMaxEnergy = 100f;
        private const float BaseEnergy = 100f;
        private const float BaseEnergyRegenDelay = 2f;
        private const float BaseEnergyRegenRate = 10f;
        private const float BaseMaxJetpackCharge = 100f;
        private const float BaseJetpackCharge = 100f;
        private const float BaseJetpackRechargeDelay = 1f;
        private const float BaseJetpackRechargeRate = 40f;
        private const float BaseDefense = 0f;
        private const float BaseDamageIncrease = 0f;
        private const float BaseDamageMultiplier = 1f;
        private const float BaseMeleeDamageIncrease = 0f;
        private const float BaseMeleeDamageMultiplier = 1f;
        private const float BaseRangedDamageIncrease = 0f;
        private const float BaseRangedDamageMultiplier = 1f;
        private const float BaseDefensePenetration = 0f;
        private const float BaseCritChance = 5f;
        private const float BaseKnockbackMultiplier = 1f;
        private const float BaseMaxMoveSpeed = 4f;
        private const float BaseAccelerationSpeed = 25f;
        private const float BaseJumpHeight = 120f;
        private const float BaseAttackSpeed = 1f;

        #endregion

        private static float MaxHealth { get; set; }
        private static float Health { get; set; }
        private static float HealthRegenDelay { get; set; }
        private static float HealthRegenRate { get; set; }

        private static float MaxEnergy { get; set; }
        public static float Energy { get; private set; }
        private static float EnergyRegenDelay { get; set; }
        private static float EnergyRegenRate { get; set; }

        private static float MaxJetpackCharge { get; set; }
        public static float JetpackCharge { get; private set; }
        private static float JetpackRechargeDelay { get; set; }
        private static float JetpackRechargeRate { get; set; }
        
        public static float Defense { get; private set; }
        private static float DamageIncrease { get; set; }
        private static float DamageMultiplier { get; set; }
        private static float MeleeDamageIncrease { get; set; }
        private static float MeleeDamageMultiplier { get; set; }
        private static float RangedDamageIncrease { get; set; }
        private static float RangedDamageMultiplier { get; set; }
        public static float DefensePenetration { get; private set; }
        private static float CritChance { get; set; }
        public static float KnockbackMultiplier { get; private set; }
        public static float MaxMoveSpeed { get; private set; }
        public static float AccelerationSpeed { get; private set; }
        public static float JumpForce { get; private set; }
        public static float AttackSpeed { get; private set; }
        // TODO: Implement status effect duration multipliers
        public static float BuffDurationMultiplier { get; private set; }
        public static float DebuffDurationMultiplier { get; private set; }
        
        // Each trinket/gadget/item has an id and a list of stat modifiers
        private static readonly Dictionary<string, List<StatModifier>> StatModifierLists = new();

        private float _energyRegenTimer, _healthRegenTimer, _jetpackRechargeTimer;
        
        public static PlayerStatsManager instance;

        private void Start()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Debug.LogError("Multiple PlayerStatsManager instances found!");
            }
            
            RecalculateStats();
            
            Health = BaseHealth;
            Energy = BaseEnergy;
            JetpackCharge = BaseJetpackCharge;
        }

        private void Update()
        {
            HandleStatsRegeneration();
        }

        private void HandleStatsRegeneration()
        {
            if (_energyRegenTimer > 0)
            {
                _energyRegenTimer -= Time.deltaTime;
            }
            else
            {
                Energy = Mathf.Clamp(Energy + EnergyRegenRate * Time.deltaTime, 0, MaxEnergy);
                StatsUIManager.instance.UpdateEnergyUI(Energy, MaxEnergy);
            }

            if (_healthRegenTimer > 0)
            {
                _healthRegenTimer -= Time.deltaTime;
            }
            else
            {
                Health = Mathf.Clamp(Health + HealthRegenRate * Time.deltaTime, 0, MaxHealth);
                StatsUIManager.instance.UpdateHealthUI(Health, MaxHealth);
            }
            
            if (_jetpackRechargeTimer > 0)
            {
                _jetpackRechargeTimer -= Time.deltaTime;
            }
            else
            {
                JetpackCharge = Mathf.Clamp(JetpackCharge + JetpackRechargeRate * Time.deltaTime, 0, MaxJetpackCharge);
                //StatsUIManager.instance.UpdateJetpackUI(jetpackCharge, maxJetpackCharge);
            }
        }
        
        private static void RecalculateStats()
        {
            // Reset stats
            MaxHealth = BaseMaxHealth;
            // Health = _baseHealth;
            HealthRegenDelay = BaseHealthRegenDelay;
            HealthRegenRate = BaseHealthRegenRate;
            MaxEnergy = BaseMaxEnergy;
            // Energy = _baseEnergy;
            EnergyRegenDelay = BaseEnergyRegenDelay;
            EnergyRegenRate = BaseEnergyRegenRate;
            MaxJetpackCharge = BaseMaxJetpackCharge;
            // JetpackCharge = _baseJetpackCharge;
            JetpackRechargeDelay = BaseJetpackRechargeDelay;
            JetpackRechargeRate = BaseJetpackRechargeRate;
            Defense = BaseDefense;
            DamageIncrease = BaseDamageIncrease;
            DamageMultiplier = BaseDamageMultiplier;
            MeleeDamageIncrease = BaseMeleeDamageIncrease;
            MeleeDamageMultiplier = BaseMeleeDamageMultiplier;
            RangedDamageIncrease = BaseRangedDamageIncrease;
            RangedDamageMultiplier = BaseRangedDamageMultiplier;
            DefensePenetration = BaseDefensePenetration;
            CritChance = BaseCritChance;
            KnockbackMultiplier = BaseKnockbackMultiplier;
            MaxMoveSpeed = BaseMaxMoveSpeed;
            AccelerationSpeed = BaseAccelerationSpeed;
            JumpForce = BaseJumpHeight;
            AttackSpeed = BaseAttackSpeed;
            
            foreach (var statModifiers in StatModifierLists)
            {
                // Apply fixed increases
                foreach (var modifier in statModifiers.Value)
                {
                    switch (modifier.statModifierType)
                    {
                        case StatModifierEnum.MaxHealthIncrease:
                            MaxHealth += modifier.value;
                            break;
                        case StatModifierEnum.MaxEnergyIncrease:
                            MaxEnergy += modifier.value;
                            break;
                        case StatModifierEnum.MaxJetpackChargeIncrease:
                            MaxJetpackCharge += modifier.value;
                            break;
                        case StatModifierEnum.DefenseIncrease:
                            Defense += modifier.value;
                            break;
                        case StatModifierEnum.DamageIncrease:
                            DamageIncrease += modifier.value;
                            break;
                        case StatModifierEnum.MeleeDamageIncrease:
                            MeleeDamageIncrease += modifier.value;
                            break;
                        case StatModifierEnum.RangedDamageIncrease:
                            RangedDamageIncrease += modifier.value;
                            break;
                        case StatModifierEnum.DefensePenetrationIncrease:
                            DefensePenetration += modifier.value;
                            break;
                        // No warning here because the modifier could be a multiplier
                    }
                }
                
                // Apply multipliers
                foreach (var modifier in statModifiers.Value)
                {
                    switch (modifier.statModifierType)
                    {
                        case StatModifierEnum.MaxHealthMultiplier:
                            MaxHealth *= modifier.value;
                            break;
                        case StatModifierEnum.MaxEnergyMultiplier:
                            MaxEnergy *= modifier.value;
                            break;
                        case StatModifierEnum.MaxJetpackChargeMultiplier:
                            MaxJetpackCharge *= modifier.value;
                            break;
                        case StatModifierEnum.DefenseMultiplier:
                            Defense *= modifier.value;
                            break;
                        case StatModifierEnum.DamageMultiplier:
                            DamageMultiplier += modifier.value;
                            break;
                        case StatModifierEnum.MeleeDamageMultiplier:
                            MeleeDamageMultiplier += modifier.value;
                            break;
                        case StatModifierEnum.RangedDamageMultiplier:
                            RangedDamageMultiplier += modifier.value;
                            break;
                        case StatModifierEnum.DefensePenetrationMultiplier:
                            DefensePenetration *= modifier.value;
                            break;
                        case StatModifierEnum.CritChanceMultiplier:
                            CritChance *= modifier.value;
                            break;
                        case StatModifierEnum.KnockbackMultiplier:
                            KnockbackMultiplier += modifier.value;
                            break;
                        case StatModifierEnum.MaxMoveSpeedMultiplier:
                            MaxMoveSpeed *= modifier.value;
                            break;
                        case StatModifierEnum.AccelerationSpeedMultiplier:
                            AccelerationSpeed *= modifier.value;
                            break;
                        case StatModifierEnum.JumpHeightMultiplier:
                            JumpForce *= modifier.value;
                            break;
                        case StatModifierEnum.AttackSpeedMultiplier:
                            AttackSpeed *= modifier.value;
                            break;
                        default:
                            Debug.LogWarning("Unknown stat modifier type: " + modifier.statModifierType);
                            break;
                    }
                }
            }
        }
        
        // public stuff

        public static bool ChangeHealth(float amount)
        {
            Health = Mathf.Clamp(Health + amount, 0, MaxHealth);
            instance._healthRegenTimer = HealthRegenDelay;
            StatsUIManager.instance.UpdateHealthUI(Health, MaxHealth);

            return Health <= 0;
        }

        public static bool ChangeEnergy(float amount)
        {
            Energy = Mathf.Clamp(Energy + amount, 0, MaxEnergy);
            instance._energyRegenTimer = EnergyRegenDelay;
            StatsUIManager.instance.UpdateEnergyUI(Energy, MaxEnergy);

            return Energy <= 0;
        }
        
        public static bool ChangeJetpackCharge(float amount)
        {
            JetpackCharge = Mathf.Clamp(JetpackCharge + amount, 0, MaxJetpackCharge);
            instance._jetpackRechargeTimer = JetpackRechargeDelay;
            //StatsUIManager.instance.UpdateJetpackUI(instance.jetpackCharge, instance.maxJetpackCharge);

            return JetpackCharge <= 0;
        }

        private static float CalculateCritDamage(float damage, float itemBaseCritChance)
        {
            var rng = Random.Range(0f, 100f);
            var critChance = itemBaseCritChance + CritChance;
            return rng <= critChance ? damage * Random.Range(1.5f, 3f) : damage;
        }
        
        // Consider refactoring these functions into 1, especially if more damage types are added (energy)
        public static float CalculateMeleeDamage(float itemBaseDamage, float itemBaseCritChance)
        {
            var flatDamage = itemBaseDamage + DamageIncrease + MeleeDamageIncrease;
            var realDamage = flatDamage * DamageMultiplier * MeleeDamageMultiplier;
            return CalculateCritDamage(realDamage, itemBaseCritChance);
        }
        
        public static float CalculateRangedDamage(float itemBaseDamage, float itemBaseCritChance)
        {
            var flatDamage = itemBaseDamage + DamageIncrease + RangedDamageIncrease;
            var realDamage = flatDamage * DamageMultiplier * RangedDamageMultiplier;
            return CalculateCritDamage(realDamage, itemBaseCritChance);
        }

        public static void AddStatModifiers(List<StatModifier> modifiers, string id)
        {
            StatModifierLists.Add(id, modifiers);
            RecalculateStats();
        }

        public static void RemoveStatModifiers(string id)
        {
            StatModifierLists.Remove(id);
            RecalculateStats();
        }
    }
}
