using UnityEngine;
using UnityEngine.UI;

public class StatsUIManager : MonoBehaviour
{
    [SerializeField] private Image hpIcon;
    [SerializeField] private Image hpRing;
    [SerializeField] private Image energyIcon;
    [SerializeField] private Image energyRing;
    
    public static StatsUIManager Instance;

    private void Start()
    {
        Instance = this;
    }

    public void UpdateHealthUI(float health, float maxHealth)
    {
        var val = health / maxHealth;
        hpIcon.fillAmount = val;
        hpRing.fillAmount = val;
    }

    public void UpdateEnergyUI(float energy, float maxEnergy)
    {
        var val = Mathf.Lerp(0, 0.75f, energy / maxEnergy);
        energyIcon.fillAmount = energy / maxEnergy;
        energyRing.fillAmount = val;
    }
}
