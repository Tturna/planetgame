using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Entities
{
    public class HealthbarManager : MonoBehaviour
    {
        [SerializeField] private Sprite whiteBarNormal, actualBarNormal, bgNormal;
        [SerializeField] private Sprite whiteBarBoss, actualBarBoss, bgBoss;

        private const float WhiteBarSmoothing = 0.015f;

        private GameObject _whiteBarObject, _actualBarObject;
        private GameObject _focusedBossHealth;
        private Image _fWhiteBar, _fActualBar, _fPortrait;
        private TextMeshProUGUI _fName, _fHpPercent;
        private GameObject _child;

        private bool _isBoss;

        private void Update()
        {
            var scalev = _whiteBarObject.transform.localScale;
            scalev.x = Mathf.Lerp(scalev.x, _actualBarObject.transform.localScale.x, WhiteBarSmoothing);
            _whiteBarObject.transform.localScale = scalev;

            var posv = _whiteBarObject.transform.localPosition;
            posv.x = Mathf.Lerp(posv.x, _actualBarObject.transform.localPosition.x, WhiteBarSmoothing);
            _whiteBarObject.transform.localPosition = posv;

            if (!_isBoss || !_focusedBossHealth) return;

            _fWhiteBar.fillAmount = Mathf.Lerp(_fWhiteBar.fillAmount, _fActualBar.fillAmount, WhiteBarSmoothing);
        }

        public void Initialize(float startingHealth, float maxHealth, bool isBoss)
        {
            _isBoss = isBoss;
            
            // Create background image as child of entity
            _child = new GameObject("Health Bar");
            _child.transform.SetParent(transform);
            _child.transform.localPosition = Vector3.down * 1.5f;
            
            var childSr = _child.AddComponent<SpriteRenderer>();
            childSr.sprite = _isBoss ? bgBoss : bgNormal;
            childSr.sortingOrder = 5;
            
            // Create white bar as child of bg
            _whiteBarObject = new GameObject("White Bar");
            _whiteBarObject.transform.SetParent(_child.transform);
            _whiteBarObject.transform.localPosition = Vector3.zero;

            childSr = _whiteBarObject.AddComponent<SpriteRenderer>();
            childSr.sprite = _isBoss ? whiteBarBoss : whiteBarNormal;
            childSr.sortingOrder = 6;
            
            // Create actual bar as child of bg
            _actualBarObject = new GameObject("Actual Bar");
            _actualBarObject.transform.SetParent(_child.transform);
            _actualBarObject.transform.localPosition = Vector3.zero;

            childSr = _actualBarObject.AddComponent<SpriteRenderer>();
            childSr.sprite = _isBoss ? actualBarBoss : actualBarNormal;
            childSr.sortingOrder = 7;
            
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
            var scalev = _actualBarObject.transform.localScale;
            scalev.x = health / maxHealth;
            _actualBarObject.transform.localScale = scalev;

            var posv = _actualBarObject.transform.localPosition;
            posv.x = Mathf.Lerp(_isBoss ? -0.594f : -0.28f, 0, scalev.x);
            _actualBarObject.transform.localPosition = posv;
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
                Utilities.Instance.DelayExecute(() => _focusedBossHealth.SetActive(false), 7);
            }
        }

        public void EnableBossUIHealth()
        {
            _focusedBossHealth.SetActive(true);
        }
    }
}