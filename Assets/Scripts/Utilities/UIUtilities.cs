using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Utilities
{
    public class UIUtilities : MonoBehaviour
    {
        [SerializeField] private RectTransform playerHud;
        [SerializeField] private RectTransform shipHud;
        [SerializeField] private Image deathOverlayBg;
        [SerializeField] private TextMeshProUGUI deathOverlayText;
        private static UIUtilities _instance;
        
        public delegate void OnMouseRaycastHandler(List<RaycastResult> results);
        public static event OnMouseRaycastHandler OnMouseRaycast;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private static void TriggerOnMouseRaycast()
        {
            OnMouseRaycast?.Invoke(Results);
        }
        
        private static readonly List<RaycastResult> Results = new();
        
        private void Update()
        {
            MouseRaycast();
            
            // if (Results.Count > 0)
            // {
            //     Debug.Log(Results[0].gameObject.name);
            // }
        }

        /// <summary>
        /// Get a list of UI elements under the cursor
        /// </summary>
        /// <returns></returns>
        private static void MouseRaycast()
        {
            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            
            EventSystem.current.RaycastAll(pointerData, Results);
            TriggerOnMouseRaycast();
        }

        public static List<RaycastResult> GetMouseRaycast()
        {
            return Results;
        }
        
        public static bool IsMouseOverUI()
        {
            return Results.Count > 0;
        }

        // TODO: Consider moving this death overlay stuff to another class like StatsUIManager. Also consider making StatsUIManager into just UIManager
        public static void ShowDeathOverlay()
        {
            _instance.StartCoroutine(FadeDeathOverlay());
        }

        public static void HideDeathOverlay()
        {
            var bgColor = _instance.deathOverlayBg.color;
            var textColor = _instance.deathOverlayText.color;
            bgColor.a = 0f;
            textColor.a = 0f;
            _instance.deathOverlayBg.color = bgColor;
            _instance.deathOverlayText.color = textColor;
        }
        
        private static IEnumerator FadeDeathOverlay()
        {
            var timer = 1.5f;

            while (timer > 0)
            {
                timer -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }

            const float fadeTime = 5f;
            timer = fadeTime;
            var bgColor = _instance.deathOverlayBg.color;
            var textColor = _instance.deathOverlayText.color;
            const float bgMaxAlpha = 0.5f;

            while (timer > 0)
            {
                var normalTime = 1f - timer / fadeTime;
                bgColor.a = GameUtilities.Lerp(0f, bgMaxAlpha, normalTime);
                textColor.a = normalTime;
                _instance.deathOverlayBg.color = bgColor;
                _instance.deathOverlayText.color = textColor;
                
                timer -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            
            bgColor.a = bgMaxAlpha;
            textColor.a = 1f;
            _instance.deathOverlayBg.color = bgColor;
            _instance.deathOverlayText.color = textColor;
        }

        public static void UIShake(float time, float strength)
        {
            _instance.StartCoroutine(_instance._UIShake(time, strength));
        }

        private IEnumerator _UIShake(float time, float strength)
        {
            while (time > 0f)
            {
                var rnd = Random.insideUnitCircle * strength;
                playerHud.anchoredPosition = rnd;
                shipHud.anchoredPosition = rnd;
                time -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            
            playerHud.anchoredPosition = Vector2.zero;
            shipHud.anchoredPosition = Vector2.zero;
        }
   }
}
