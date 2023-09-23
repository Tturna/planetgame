using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace Entities
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
        [SerializeField] private Image hurtVignette;
        [SerializeField] private RectTransform vignetteFxMaskRect;
        [SerializeField] private Vector2 vignetteFxMaskMinSize;
        [SerializeField] private Vector2 vignetteFxMaskMaxSize;
        [SerializeField, Range(0f, 1f)] private float maxHurtVignetteAlpha;
        [SerializeField] private float hurtVignetteFlashTime;
        [SerializeField, Range(0f, 1f)] private float vignetteHealthThreshold;
        [SerializeField] private Material flashMaterial;
    
        public static StatsUIManager instance;
        private Material _defaultMaterial;
        private bool _vignetteFlashing;

        private void Start()
        {
            instance = this;
            _defaultMaterial = hpBar.material;
        }

        private void Update()
        {
            var capInverseHpPercent = (vignetteHealthThreshold - hpBar.fillAmount) * (1 / vignetteHealthThreshold);
            
            if (!_vignetteFlashing)
            {
                var targetAlpha = Mathf.Clamp01(capInverseHpPercent * maxHurtVignetteAlpha);
                var alpha = Mathf.Lerp(hurtVignette.color.a, targetAlpha, Time.deltaTime * 2f);
                hurtVignette.color = new Color(1f, 1f, 1f, alpha);
            }

            var targetFxSize = Vector2.Lerp(vignetteFxMaskMaxSize, vignetteFxMaskMinSize, capInverseHpPercent);
            vignetteFxMaskRect.sizeDelta =
            Vector2.Lerp(vignetteFxMaskRect.sizeDelta, targetFxSize, Time.deltaTime * 4f);
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
                StartCoroutine(FlashHurtVignette(hurtVignette.color.a));
            }

            hpBar.fillAmount = val;
        }
        
        private IEnumerator FlashHurtVignette(float currentAlpha)
        {
            var maxAlpha = maxHurtVignetteAlpha;
            const float maxAlphaDiff = .15f;
            if (maxAlpha - currentAlpha > maxAlphaDiff)
            {
                maxAlpha = currentAlpha + maxAlphaDiff;
            }
            
            _vignetteFlashing = true;
            var t = hurtVignetteFlashTime;
            while (t > 0f)
            {
                var v = GameUtilities.Remap(0f, hurtVignetteFlashTime, 1f, 0f, t);
                var alpha = Mathf.Lerp(maxAlpha, currentAlpha, v);
                hurtVignette.color = new Color(1f, 1f, 1f, alpha);
                t -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            _vignetteFlashing = false;
        }

        public void UpdateEnergyUI(float energy, float maxEnergy)
        {
            var val = energy / maxEnergy;
            energyBar.fillAmount = val;
        }
    }
}
