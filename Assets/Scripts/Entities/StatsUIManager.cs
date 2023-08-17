using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utilities;

namespace Entities.Entities
{
    public class StatsUIManager : MonoBehaviour
    {
        // [SerializeField] private Image hpIcon;
        // [SerializeField] private Image hpRing;
        // [SerializeField] private Image energyIcon;
        // [SerializeField] private Image energyRing;

        [SerializeField] private Image hpBar;
        [SerializeField] private Image hpBarBg;
        [SerializeField] private Image hpBarIcon;
        [SerializeField] private Image energyBar;
        [SerializeField] private Material flashMaterial;
    
        public static StatsUIManager instance;
        private Material _defaultMaterial;

        private void Start()
        {
            instance = this;
            _defaultMaterial = hpBar.material;
        }

        public void UpdateHealthUI(float health, float maxHealth)
        {
            var val = health / maxHealth;

            void SetHpUIMaterial(Material material)
            {
                hpBar.material = material;
                hpBarBg.material = material;
                hpBarIcon.material = material;
            }

            if (hpBar.fillAmount > val)
            {
                SetHpUIMaterial(flashMaterial);
                GameUtilities.instance.DelayExecute(() => SetHpUIMaterial(_defaultMaterial), 0.1f);
            }

            hpBar.fillAmount = val;
        }

        public void UpdateEnergyUI(float energy, float maxEnergy)
        {
            var val = energy / maxEnergy;
            energyBar.fillAmount = val;
        }
    }
}
