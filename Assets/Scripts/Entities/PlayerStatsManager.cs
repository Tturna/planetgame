using System.Collections.Generic;
using UnityEngine;

namespace Entities
{
    public class PlayerStatsManager : MonoBehaviour
    {
        // Base stats
        [SerializeField] private float maxHealth;
        [SerializeField] private float health;
        [SerializeField] private float healthRegenDelay;
        [SerializeField] private float healthRegenRate;
        
        [SerializeField] private float maxEnergy;
        [SerializeField] private float energy;
        [SerializeField] private float energyRegenDelay;
        [SerializeField] private float energyRegenRate;

        [SerializeField] private float maxJetpackCharge;
        [SerializeField] private float jetpackCharge;
        [SerializeField] private float jetpackRechargeDelay;
        [SerializeField] private float jetpackRechargeRate;
        
        // 2023-9-20:
        // Accessories will add all of their modifiers when they're equipped.
        // They will remove them when they're unequipped.
        // This feels kinda dumb to do it this way because it's very repetitive.
        // My brain feels fried
        // TODO: Figure out how to do this better. Maybe a loop?
        public struct AccessoryModifierData
        {
            public float maxHealthAdder;
            public float maxHealthMultiplier;
            public float maxEnergyAdder;
            public float maxEnergyMultiplier;
            public float defenseAdder;
            public float defenseMultiplier;
            public float damageReductionMultiplier;
            public float damageAdder;
            public float damageMultiplier;
            public float defensePenetrationAdder;
            public float defensePenetrationMultiplier;
            public float critChanceMultiplier;
            public float knockbackMultiplier;
            public float moveSpeedMultiplier;
            public float jumpHeightMultiplier;
            public float attackSpeedMultiplier;
            public float miningSpeedMultiplier;
            public float miningPowerMultiplier;
            public float buildingSpeedMultiplier;
            public float jetpackRechargeMultiplier;
            public float jetpackChargeAdder;
        }
        
        // Containers for stat modifiers. Accessories will add to these.
        // Modifiers are a dictionary with a string for the id of the accessory that adds the value.

        #region Stat Modifiers
        private Dictionary<string, float> _maxHealthAdders = new();
        private Dictionary<string, float> _maxHealthMultipliers = new();
        private Dictionary<string, float> _maxEnergyAdders = new();
        private Dictionary<string, float> _maxEnergyMultipliers = new();
        private Dictionary<string, float> _defenseAdders = new();
        private Dictionary<string, float> _defenseMultipliers = new();
        private Dictionary<string, float> _damageReductionMultipliers = new();
        private Dictionary<string, float> _damageAdders = new();
        private Dictionary<string, float> _damageMultipliers = new();
        private Dictionary<string, float> _defensePenetrationAdders = new();
        private Dictionary<string, float> _defensePenetrationMultipliers = new();
        private Dictionary<string, float> _critChanceMultipliers = new();
        private Dictionary<string, float> _knockbackMultipliers = new();
        private Dictionary<string, float> _moveSpeedMultipliers = new();
        private Dictionary<string, float> _jumpHeightMultipliers = new();
        private Dictionary<string, float> _attackSpeedMultipliers = new();
        private Dictionary<string, float> _miningSpeedMultipliers = new();
        private Dictionary<string, float> _miningPowerMultipliers = new();
        private Dictionary<string, float> _buildingSpeedMultipliers = new();
        private Dictionary<string, float> _jetpackRechargeMultipliers = new();
        private Dictionary<string, float> _jetpackChargeAdders = new();
        #endregion

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
                energy = Mathf.Clamp(energy + energyRegenRate * Time.deltaTime, 0, maxEnergy);
                StatsUIManager.instance.UpdateEnergyUI(energy, maxEnergy);
            }

            if (_healthRegenTimer > 0)
            {
                _healthRegenTimer -= Time.deltaTime;
            }
            else
            {
                health = Mathf.Clamp(health + healthRegenRate * Time.deltaTime, 0, maxEnergy);
                StatsUIManager.instance.UpdateHealthUI(health, maxHealth);
            }
            
            if (_jetpackRechargeTimer > 0)
            {
                _jetpackRechargeTimer -= Time.deltaTime;
            }
            else
            {
                jetpackCharge = Mathf.Clamp(jetpackCharge + jetpackRechargeRate * Time.deltaTime, 0, maxJetpackCharge);
                //StatsUIManager.instance.UpdateJetpackUI(jetpackCharge, maxJetpackCharge);
            }
        }
        
        // public stuff

        /// <summary>
        /// Changes health by the given amount. Returns true if the remaining health is 0.
        /// </summary>
        /// <param name="amount"></param>
        public static bool ChangeHealth(float amount)
        {
            instance.health = Mathf.Clamp(instance.health + amount, 0, instance.maxHealth);
            instance._healthRegenTimer = instance.healthRegenDelay;
            StatsUIManager.instance.UpdateHealthUI(instance.health, instance.maxHealth);

            return instance.health <= 0;
        }

        /// <summary>
        /// Changes energy by the given amount. Returns true if the remaining energy is 0.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static bool ChangeEnergy(float amount)
        {
            instance.energy = Mathf.Clamp(instance.energy + amount, 0, instance.maxEnergy);
            instance._energyRegenTimer = instance.energyRegenDelay;
            StatsUIManager.instance.UpdateEnergyUI(instance.energy, instance.maxEnergy);

            return instance.energy <= 0;
        }
        
        public static bool ChangeJetpackCharge(float amount)
        {
            instance.jetpackCharge = Mathf.Clamp(instance.jetpackCharge + amount, 0, instance.maxJetpackCharge);
            instance._jetpackRechargeTimer = instance.jetpackRechargeDelay;
            //StatsUIManager.instance.UpdateJetpackUI(instance.jetpackCharge, instance.maxJetpackCharge);

            return instance.jetpackCharge <= 0;
        }

        public static float GetEnergy()
        {
            return instance.energy;
        }
        
        public static float GetJetpackCharge()
        {
            return instance.jetpackCharge;
        }

        public static void AddAccessoryModifiers(AccessoryModifierData data, string id)
        {
            instance._maxHealthAdders.Add(id, data.maxHealthAdder);
            instance._maxHealthMultipliers.Add(id, data.maxHealthMultiplier);
            instance._maxEnergyAdders.Add(id, data.maxEnergyAdder);
            instance._maxEnergyMultipliers.Add(id, data.maxEnergyMultiplier);
            instance._defenseAdders.Add(id, data.defenseAdder);
            instance._defenseMultipliers.Add(id, data.defenseMultiplier);
            instance._damageReductionMultipliers.Add(id, data.damageReductionMultiplier);
            instance._damageAdders.Add(id, data.damageAdder);
            instance._damageMultipliers.Add(id, data.damageMultiplier);
            instance._defensePenetrationAdders.Add(id, data.defensePenetrationAdder);
            instance._defensePenetrationMultipliers.Add(id, data.defensePenetrationMultiplier);
            instance._critChanceMultipliers.Add(id, data.critChanceMultiplier);
            instance._knockbackMultipliers.Add(id, data.knockbackMultiplier);
            instance._moveSpeedMultipliers.Add(id, data.moveSpeedMultiplier);
            instance._jumpHeightMultipliers.Add(id, data.jumpHeightMultiplier);
            instance._attackSpeedMultipliers.Add(id, data.attackSpeedMultiplier);
            instance._miningSpeedMultipliers.Add(id, data.miningSpeedMultiplier);
            instance._miningPowerMultipliers.Add(id, data.miningPowerMultiplier);
            instance._buildingSpeedMultipliers.Add(id, data.buildingSpeedMultiplier);
            instance._jetpackRechargeMultipliers.Add(id, data.jetpackRechargeMultiplier);
            instance._jetpackChargeAdders.Add(id, data.jetpackChargeAdder);
        }

        public static void RemoveAccessoryModifiers(string id)
        {
            instance._maxHealthAdders.Remove(id);
            instance._maxHealthMultipliers.Remove(id);
            instance._maxEnergyAdders.Remove(id);
            instance._maxEnergyMultipliers.Remove(id);
            instance._defenseAdders.Remove(id);
            instance._defenseMultipliers.Remove(id);
            instance._damageReductionMultipliers.Remove(id);
            instance._damageAdders.Remove(id);
            instance._damageMultipliers.Remove(id);
            instance._defensePenetrationAdders.Remove(id);
            instance._defensePenetrationMultipliers.Remove(id);
            instance._critChanceMultipliers.Remove(id);
            instance._knockbackMultipliers.Remove(id);
            instance._moveSpeedMultipliers.Remove(id);
            instance._jumpHeightMultipliers.Remove(id);
            instance._attackSpeedMultipliers.Remove(id);
            instance._miningSpeedMultipliers.Remove(id);
            instance._miningPowerMultipliers.Remove(id);
            instance._buildingSpeedMultipliers.Remove(id);
            instance._jetpackRechargeMultipliers.Remove(id);
            instance._jetpackChargeAdders.Remove(id);
        }
    }
}
