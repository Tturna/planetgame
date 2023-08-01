using UnityEngine;
using UnityEngine.UI;

namespace Entities.Entities
{
    public class StatsUIManager : MonoBehaviour
    {
        // [SerializeField] private Image hpIcon;
        // [SerializeField] private Image hpRing;
        // [SerializeField] private Image energyIcon;
        // [SerializeField] private Image energyRing;

        [SerializeField] private Image hpBar;
        [SerializeField] private Image energyBar;
    
        public static StatsUIManager instance;

        private void Start()
        {
            instance = this;
        }

        public void UpdateHealthUI(float health, float maxHealth)
        {
            var val = health / maxHealth;
            hpBar.fillAmount = val;
        }

        public void UpdateEnergyUI(float energy, float maxEnergy)
        {
            var val = energy / maxEnergy;
            energyBar.fillAmount = val;
        }
    }
}
