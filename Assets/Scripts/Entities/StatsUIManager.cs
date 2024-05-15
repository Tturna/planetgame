using System.Collections;
using TMPro;
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

        [SerializeField] private Animator uiAnimator;
        
        [Header("Player stuff")]
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
        
        [Header("Ship stuff")]
        [SerializeField] private GameObject shipHud;
        [SerializeField] private Image shipHullBar;
        [SerializeField] private Image shipFuelBar;
        [SerializeField] private TextMeshProUGUI shipHullText;
        [SerializeField] private TextMeshProUGUI shipFuelText;
        [SerializeField] private TextMeshProUGUI shipLocationText;
        [SerializeField] private TextMeshProUGUI shipAngleText;
        [SerializeField] private TextMeshProUGUI shipVelocityText;
        [SerializeField] private TextMeshProUGUI shipBoostStatusText;
        [SerializeField] private Image shipGearIndicator1;
        [SerializeField] private Image shipGearIndicator2;
        [SerializeField] private Image shipGearIndicator3;
        [SerializeField] private Sprite gearIndicatorEmptySprite;
        [SerializeField] private Sprite gearIndicatorFullSprite;
    
        public static StatsUIManager instance;
        private Material _defaultMaterial;
        private bool _vignetteFlashing;

        private void Start()
        {
            instance = this;
            _defaultMaterial = hpBar.material;
            shipHud.SetActive(false);
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
        
        public void UpdateShipHullUI(float hull, float maxHull)
        {
            var val = hull / maxHull;
            shipHullBar.fillAmount = val;
            shipHullText.text = $"{Mathf.RoundToInt(val * 100)}%";
        }
        
        public void UpdateShipFuelUI(float fuel, float maxFuel)
        {
            var val = fuel / maxFuel;
            shipFuelBar.fillAmount = val;
            shipFuelText.text = $"{Mathf.RoundToInt(val * 100)}%";
        }

        public void UpdateShipLocationUI(Vector2 location)
        {
            shipLocationText.text = $"{Mathf.RoundToInt(location.x)}, {Mathf.RoundToInt(location.y)}";
        }
        
        public void UpdateShipAngleUI(float angle)
        {
            shipAngleText.text = $"{Mathf.RoundToInt(angle)} deg";
        }

        public void UpdateShipVelocityUI(float velocity)
        {
            shipVelocityText.text = $"{Mathf.RoundToInt(velocity)} m/s";
        }

        public void UpdateShipBoostStatusUI(bool boostAvailable)
        {
            shipBoostStatusText.text = boostAvailable ? "Boost ready" : "Boost recharging...";
            uiAnimator.SetBool("ShipBoostRecharging", !boostAvailable);
        }

        public void UpdateShipGearUI(int gear)
        {
            if (gear >= 0 && shipGearIndicator1.sprite != gearIndicatorFullSprite)
            {
                shipGearIndicator1.sprite = gearIndicatorFullSprite;
                StartCoroutine(ScaleGearIndicator(shipGearIndicator1));
            }
            else if (gear < 0)
            {
                shipGearIndicator1.sprite = gearIndicatorEmptySprite;
            }
            
            if (gear >= 1 && shipGearIndicator2.sprite != gearIndicatorFullSprite)
            {
                shipGearIndicator2.sprite = gearIndicatorFullSprite;
                StartCoroutine(ScaleGearIndicator(shipGearIndicator2));
            }
            else if (gear < 1)
            {
                shipGearIndicator2.sprite = gearIndicatorEmptySprite;
            }
            
            if (gear >= 2 && shipGearIndicator3.sprite != gearIndicatorFullSprite)
            {
                shipGearIndicator3.sprite = gearIndicatorFullSprite;
                StartCoroutine(ScaleGearIndicator(shipGearIndicator3));
            }
            else if (gear < 2)
            {
                shipGearIndicator3.sprite = gearIndicatorEmptySprite;
            }
        }

        private IEnumerator ScaleGearIndicator(Image gearIndicator)
        {
            var tr = gearIndicator.rectTransform;
            var startScale = tr.localScale;
            var timer = 0f;
            
            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                var normalTime = timer / 0.2f;

                if (normalTime < 0.5f)
                {
                    tr.localScale = Vector3.Lerp(startScale, startScale * 1.5f, normalTime * 2f);
                    yield return new WaitForEndOfFrame();
                }
                else
                {
                    tr.localScale = Vector3.Lerp(startScale * 1.5f, startScale, (normalTime - 0.5f) * 2f);
                    yield return new WaitForEndOfFrame();
                }
            }
            
            tr.localScale = startScale;
        }
        
        public void ShowShipHUD(float hull, float fuel, float maxHull, float maxFuel)
        {
            shipHud.SetActive(true);
            uiAnimator.SetTrigger("ShowShipHUD");
            instance.StartCoroutine(FillShipStatBars(hull, fuel, maxHull, maxFuel));
        }

        private IEnumerator FillShipStatBars(float hull, float fuel, float maxHull, float maxFuel)
        {
            var timer = 3.5f;
            UpdateShipHullUI(0f, maxHull);
            UpdateShipFuelUI(0f, maxFuel);

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                if (timer > 1.5f)
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                var normalTime = 1f - timer / 1.5f;
                
                var lerpHull = Mathf.Lerp(0f, hull, normalTime);
                var lerpFuel = Mathf.Lerp(0f, fuel, normalTime);
                UpdateShipHullUI(lerpHull, maxHull);
                UpdateShipFuelUI(lerpFuel, maxFuel);
                
                yield return new WaitForEndOfFrame();
            }
            
            UpdateShipHullUI(hull, maxHull);
            UpdateShipFuelUI(fuel, maxFuel);
        }

        public void HideShipHUD()
        {
            shipHud.SetActive(false);
        }
    }
}
