using System.Collections;
using Entities;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;
using VFX;
using Random = UnityEngine.Random;

namespace Environment
{
    public class EnvironmentManager : MonoBehaviour
    {
        public delegate void OnFogStartedHandler(int size, float radialPosition, float maxHeight, float fogGap);
        public event OnFogStartedHandler OnFogStarted;

        public float WindStrength { get; private set; }
        public float PlanetTime { get; private set; }

        public int planetDaySeconds;

        [SerializeField, Tooltip("Minimum and maximum time in seconds before the wind strength is changed. Strength is chosen randomly.")]
        private Vector2 windChangeIntervalGap;

        [SerializeField] private float maxWindStrength;
        [SerializeField, Range(0, 1)] private float brightness;
        
        private Material _terrainRenderMaterial, _caveBgRenderMaterial, _spriteMaterial;
        private static readonly int ShaderBrightness = Shader.PropertyToID("_Brightness");
        private static readonly int SunLightAngle = Shader.PropertyToID("_SunLightAngle");
        private static readonly int RedTint = Shader.PropertyToID("_RedTint");

        private void TriggerOnFogStarted()
        {
            OnFogStarted?.Invoke(Random.Range(20, 50), Random.Range(0f, 360f), 1.2f, 0.01f);
        }

        private void Start()
        {
            _terrainRenderMaterial = GameUtilities.GetTerrainMaterial();
            _caveBgRenderMaterial = GameUtilities.GetCaveBackgroundMaterial();
            _spriteMaterial = GameUtilities.GetSpriteMaterial();
            // TriggerOnFogStarted();

            // StartCoroutine(ChangeWind());
        }

        private void Update()
        {
            // planet time -> 0 to 1 (normalized)
            // 0 -> midnight
            // 0.25 -> dawn
            // 0.5 -> noon
            // 0.75 -> dusk
            
            PlanetTime += Time.deltaTime;
            
            if (PlanetTime >= planetDaySeconds)
            {
                PlanetTime = 0;
            }
            
            // sun light angle -> 0 to 360
            // 0 -> dawn (at planet angle 0*, rises from the right)
            // 90 -> noon
            // 180 -> dusk
            // 270 -> midnight
            
            // *when the player is in the original spawn point (at the top center of the planet in relation to
            // the unity coordinate system), their relative angle to the planet should be 0.
            // Because the player always faces perpendicular to the planet, the player's global angle
            // will always match the player's relative angle to the planet.
            
            // use offset time to sync the sun to the planet time.
            // this is required because time = 0 is midnight, but sun angle = 0 is dawn.
            var offsetTime = (PlanetTime - planetDaySeconds / 4f) % planetDaySeconds;
            
            // ReSharper disable once PossibleLossOfFraction
            var sunLightAngle = offsetTime / planetDaySeconds * 360f;
            
            // we use +90 because player rotation = 0 is at the top of the planet but sun angle = 0 is
            // when the sun points from the right.
            var playerRotationReference = PlayerController.instance.transform.eulerAngles.z % 360 + 90;
                
            var absoluteDifference = Mathf.Abs(playerRotationReference - sunLightAngle);
            var normalizedDifference = absoluteDifference > 180 ? 360 - absoluteDifference : absoluteDifference;
            var similarity = (1 + Mathf.Cos(normalizedDifference * Mathf.Deg2Rad)) / 2;
            
            // clip the brightness so it's fully dark/bright for longer.
            var x = GameUtilities.InverseLerp(0.33f, 0.95f, similarity);
            
            // easing the brightness change
            brightness = x * x * x * x;
            
            brightness = Mathf.Clamp(brightness, 0.003f, 1f);
            
            // red tint at dusk and dawn
            const float redMinThreshold = 0.5f;
            const float redMaxThreshold = 0.8f;
            
            if (similarity > redMinThreshold && similarity < redMaxThreshold)
            {
                var redX = GameUtilities.InverseLerp(redMinThreshold, redMaxThreshold, similarity) * 2f;

                if (redX > 1f)
                {
                    redX = 1f - (redX - 1f);
                }
                
                var redTintValue = redX / 3f;
                
                _spriteMaterial.SetFloat(RedTint, redTintValue);
                BackgroundImageManager.SetBackgroundRedTint(redTintValue);
            }
            else
            {
                _spriteMaterial.SetFloat(RedTint, 0f);
                BackgroundImageManager.SetBackgroundRedTint(0f);
            }
            
            _terrainRenderMaterial.SetFloat(SunLightAngle, sunLightAngle);
            _terrainRenderMaterial.SetFloat(ShaderBrightness, brightness);
            _caveBgRenderMaterial.SetFloat(SunLightAngle, sunLightAngle);
            _caveBgRenderMaterial.SetFloat(ShaderBrightness, brightness);
            _spriteMaterial.SetFloat(ShaderBrightness, brightness);
            BackgroundImageManager.SetBackgroundBrightness(brightness);
            ParallaxManager.SetParallaxTerrainBrightness(brightness);
            GlobalLight.instance.SetIntensity(brightness);
        }

        private IEnumerator ChangeWind()
        {
            while (true)
            {
                var delay = Random.Range(windChangeIntervalGap.x, windChangeIntervalGap.y);
                yield return new WaitForSeconds(delay);
                WindStrength = Random.Range(-maxWindStrength, maxWindStrength);
            }
        }
    }
}
