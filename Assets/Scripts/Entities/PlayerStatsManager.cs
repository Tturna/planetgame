using System.Collections.Generic;
using Inventory.Item_SOs.Accessories;
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
        
        private Dictionary<string, List<StatModifier>> _accessoryModifierLists;

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

        // TODO: Continue making everything use stats from this stat manager
        // TODO: Consider refactoring these functions because they seems to be repetitive
        public static float GetHealth()
        {
            var totalHealth = instance.health;
            var healthMultiplier = 1f;
            
            foreach (var modifierList in instance._accessoryModifierLists.Values)
            {
                foreach (var modifier in modifierList)
                {
                    if (modifier.statModifierType == StatModifierEnum.MaxHealthIncrease)
                    {
                        totalHealth += modifier.value;
                    }
                    else if (modifier.statModifierType == StatModifierEnum.MaxHealthMultiplier)
                    {
                        healthMultiplier += modifier.value;
                    }
                }
            }
            
            return totalHealth * healthMultiplier;
        }

        public static float GetJetpackCharge()
        {
            var totalJetpackCharge = instance.jetpackCharge;
            var jetpackChargeMultiplier = 1f;
            
            foreach (var modifierList in instance._accessoryModifierLists.Values)
            {
                foreach (var modifier in modifierList)
                {
                    if (modifier.statModifierType == StatModifierEnum.MaxJetpackChargeIncrease)
                    {
                        totalJetpackCharge += modifier.value;
                    }
                    else if (modifier.statModifierType == StatModifierEnum.MaxJetpackChargeMultiplier)
                    {
                        jetpackChargeMultiplier += modifier.value;
                    }
                }
            }
            
            return totalJetpackCharge * jetpackChargeMultiplier;
        }

        public static void AddAccessoryModifiers(List<StatModifier> modifiers, string id)
        {
            instance._accessoryModifierLists.Add(id, modifiers);
        }

        public static void RemoveAccessoryModifiers(string id)
        {
            instance._accessoryModifierLists.Remove(id);
        }
    }
}
