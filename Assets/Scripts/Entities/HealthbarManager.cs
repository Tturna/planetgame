using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utilities;

namespace Entities.Entities
{
    public class HealthbarManager : MonoBehaviour
    {
        // [SerializeField] private Sprite whiteBarNormal, actualBarNormal, bgNormal;
        // [SerializeField] private Sprite whiteBarBoss, actualBarBoss, bgBoss;
        [FormerlySerializedAs("whiteBar")] [SerializeField] private Sprite whiteBarSprite;
        [FormerlySerializedAs("redBar")] [SerializeField] private Sprite redBarSprite;
        [FormerlySerializedAs("bg")] [SerializeField] private Sprite bgSprite;

        [SerializeField] private float whiteBarSmoothing;

        private GameObject _whiteBarObject, _redBarObject, _bgObject;
        private GameObject _focusedBossHealth;
        private Image _fWhiteBar, _fActualBar, _fPortrait;
        private TextMeshProUGUI _fName, _fHpPercent;
        // private GameObject _child;

        private bool _isBoss;

        private void Update()
        {
            var scalev = _whiteBarObject.transform.localScale;
            scalev.x = Mathf.Lerp(scalev.x, _redBarObject.transform.localScale.x, whiteBarSmoothing);
            _whiteBarObject.transform.localScale = scalev;

            var posv = _whiteBarObject.transform.localPosition;
            posv.x = Mathf.Lerp(posv.x, _redBarObject.transform.localPosition.x, whiteBarSmoothing);
            _whiteBarObject.transform.localPosition = posv;

            if (!_isBoss || !_focusedBossHealth) return;

            _fWhiteBar.fillAmount = Mathf.Lerp(_fWhiteBar.fillAmount, _fActualBar.fillAmount, whiteBarSmoothing);
        }

        public void Initialize(float startingHealth, float maxHealth, bool isBoss, float healthbarDistance)
        {
            _isBoss = isBoss;
            
            var healthbar = new GameObject("Health Bar");
            healthbar.transform.SetParent(transform);
            healthbar.transform.localPosition = Vector3.down * healthbarDistance;
            
            // Create background
            _bgObject = new GameObject("Background");
            _bgObject.transform.SetParent(healthbar.transform);
            _bgObject.transform.localPosition = Vector3.zero;
            
            var bgSr = _bgObject.AddComponent<SpriteRenderer>();
            bgSr.sprite = bgSprite;
            bgSr.sortingLayerID = SortingLayer.NameToID("Healthbars");
            bgSr.sortingOrder = 0;
            
            // Create white bar
            _whiteBarObject = new GameObject("White Bar");
            _whiteBarObject.transform.SetParent(healthbar.transform);
            _whiteBarObject.transform.localPosition = Vector3.zero;
            
            var whiteSr = _whiteBarObject.AddComponent<SpriteRenderer>();
            // whiteSr.sprite = _isBoss ? bgBoss : bgNormal;
            whiteSr.sprite = whiteBarSprite;
            whiteSr.sortingLayerID = SortingLayer.NameToID("Healthbars");
            whiteSr.sortingOrder = 1;

            // Create actual bar
            _redBarObject = new GameObject("Red Bar");
            _redBarObject.transform.SetParent(healthbar.transform);
            _redBarObject.transform.localPosition = Vector3.zero;

            var redSr = _redBarObject.AddComponent<SpriteRenderer>();
            // redSr.sprite = _isBoss ? actualBarBoss : actualBarNormal;
            redSr.sprite = redBarSprite;
            redSr.sortingLayerID = SortingLayer.NameToID("Healthbars");
            redSr.sortingOrder = 2;
            
            // If this is a boss, set up the big boss UI health
            if (!isBoss) return;
            _focusedBossHealth = GameObject.Find("HUD").transform.GetChild(0).gameObject;
            _fWhiteBar = _focusedBossHealth.transform.GetChild(1).GetComponent<Image>();
            _fActualBar = _focusedBossHealth.transform.GetChild(2).GetComponent<Image>();
            _fPortrait = _focusedBossHealth.transform.GetChild(3).GetComponent<Image>();
            _fHpPercent = _focusedBossHealth.transform.GetChild(4).GetComponent<TextMeshProUGUI>();
            _fName = _focusedBossHealth.transform.GetChild(5).GetComponent<TextMeshProUGUI>();
            UpdateBossUIHealth(startingHealth, maxHealth, null);
        }
        
        public void UpdateHealthbar(float health, float maxHealth)
        {
            var scalev = _redBarObject.transform.localScale;
            scalev.x = health / maxHealth;
            _redBarObject.transform.localScale = scalev;

            var posv = _redBarObject.transform.localPosition;
            posv.x = Mathf.Lerp(_isBoss ? -0.594f : -0.3f, 0, scalev.x);
            _redBarObject.transform.localPosition = posv;
        }

        public void UpdateBossUIHealth(float health, float maxHealth, Sprite portrait)
        {
            // We set the white bar fill amount here so that it smoothly goes down
            // the correct amount if for example, there are 2 bosses alive and
            // the other one has less health and this gets hit, in which case
            // this healthbar replaces the other one but now has more health.
            _fWhiteBar.fillAmount = _fActualBar.fillAmount;
            
            _fActualBar.fillAmount = health / maxHealth;
            _fHpPercent.text = $"{Mathf.CeilToInt(health / maxHealth * 100)}%";
            _fName.text = gameObject.name;

            if (portrait) _fPortrait.sprite = portrait;

            if (health <= 0)
            {
                //TODO: Check if other bosses are alive
                _fWhiteBar.fillAmount = 0;
                GameUtilities.instance.DelayExecute(() => _focusedBossHealth.SetActive(false), 7);
            }
        }

        public void EnableBossUIHealth()
        {
            _focusedBossHealth.SetActive(true);
        }
    }
}