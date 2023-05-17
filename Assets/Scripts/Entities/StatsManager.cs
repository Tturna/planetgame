using System;
using UnityEngine;

namespace Entities
{
    public class StatsManager : MonoBehaviour
    {
        [SerializeField] private float maxHealth;
        [SerializeField] private float health;
        [SerializeField] private float maxEnergy;
        [SerializeField] private float energy;
        [SerializeField] private float energyRegenDelay;
        [SerializeField] private float energyRegenRate;
        [SerializeField] private float healthRegenDelay;
        [SerializeField] private float healthRegenRate;
        
        private float _energyRegenTimer, _healthRegenTimer;

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
                StatsUIManager.Instance.UpdateEnergyUI(energy, maxEnergy);
            }

            if (_healthRegenTimer > 0)
            {
                _healthRegenTimer -= Time.deltaTime;
            }
            else
            {
                health = Mathf.Clamp(health + healthRegenRate * Time.deltaTime, 0, maxEnergy);
                StatsUIManager.Instance.UpdateHealthUI(health, maxHealth);
            }
        }
        
        // public stuff

        /// <summary>
        /// Changes health by the given amount. Returns true if the remaining health is 0.
        /// </summary>
        /// <param name="amount"></param>
        public bool ChangeHealth(float amount)
        {
            health = Mathf.Clamp(health - amount, 0, maxHealth);
            _healthRegenTimer = healthRegenDelay;
            StatsUIManager.Instance.UpdateHealthUI(health, maxHealth);

            return health <= 0;
        }

        public float GetEnergy()
        {
            return energy;
        }

        /// <summary>
        /// Changes energy by the given amount. Returns true if the remaining energy is 0.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool ChangeEnergy(float amount)
        {
            energy = Mathf.Clamp(energy - amount, 0, maxEnergy);
            _energyRegenTimer = energyRegenDelay;
            StatsUIManager.Instance.UpdateEnergyUI(energy, maxEnergy);

            return energy <= 0;
        }
    }
}
