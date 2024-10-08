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
        public enum TimeOfDay
        {
            Dawn,
            Noon,
            Dusk,
            Midnight
        }
        
        public delegate void OnFogStartedHandler(int size, float radialPosition, float maxHeight, float fogGap);
        public event OnFogStartedHandler OnFogStarted;

        public float WindStrength { get; private set; }
        [SerializeField] private bool freezeTime;
        [SerializeField] private int planetDaySeconds;
        [SerializeField] private float planetTime;
        public float PlanetNormalizedTime { get; private set; }
        public TimeOfDay AccurateTimeOfDay { get; private set; }
        public bool IsDay { get; private set; }

        [SerializeField, Tooltip("Minimum and maximum time in seconds before the wind strength is changed. Strength is chosen randomly.")]
        private Vector2 windChangeIntervalGap;

        [SerializeField] private float maxWindStrength;
        [SerializeField, Range(0, 1)] private float brightness;
        
        private Material _terrainRenderMaterial, _caveBgRenderMaterial, _spriteMaterial;
        private PlayerController player;
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
            player = PlayerController.instance;
            // TriggerOnFogStarted();

            // StartCoroutine(ChangeWind());
            
            PlanetNormalizedTime = planetTime / planetDaySeconds;
            AccurateTimeOfDay = PlanetNormalizedTime switch
            {
                < 0.25f => TimeOfDay.Midnight,
                < 0.5f => TimeOfDay.Dawn,
                < 0.75f => TimeOfDay.Noon,
                _ => TimeOfDay.Dusk
            };

            IsDay = PlanetNormalizedTime is > 0.25f and < 0.75f;
        }

        private void Update()
        {
            if (planetDaySeconds == 0)
            {
                Debug.LogWarning("Planet day seconds set to 0!");
                return;
            }
            
            // planet time -> 0 to 1 (normalized)
            // 0 -> midnight
            // 0.25 -> dawn
            // 0.5 -> noon
            // 0.75 -> dusk
            
            if (!freezeTime)
            {
                planetTime += Time.deltaTime;

                if (planetTime >= planetDaySeconds)
                {
                    planetTime = 0;
                }
                
                PlanetNormalizedTime = planetTime / planetDaySeconds;
                AccurateTimeOfDay = PlanetNormalizedTime switch
                {
                    < 0.25f => TimeOfDay.Midnight,
                    < 0.5f => TimeOfDay.Dawn,
                    < 0.75f => TimeOfDay.Noon,
                    _ => TimeOfDay.Dusk
                };

                IsDay = PlanetNormalizedTime is > 0.25f and < 0.75f;
            }
            
            if (player.IsInSpace) return;
            
            // sun light angle -> 0 to 360
            // 0 -> dawn (at planet angle 0*, rises from the right)
            // 90 -> noon
            // 180 -> dusk
            // 270 -> midnight
            
            // use offset time to sync the sun to the planet time.
            // this is required because time = 0 is midnight, but sun angle = 0 is dawn.
            var offsetTime = (planetTime - planetDaySeconds / 4f) % planetDaySeconds;
            
            // ReSharper disable once PossibleLossOfFraction
            var sunLightAngle = offsetTime / planetDaySeconds * 360f;
            
            var planetDir = player.DirectionToClosestPlanet;
            var referenceAngle = -Mathf.Atan2(-planetDir.y, -planetDir.x) * Mathf.Rad2Deg + 90;
            
            // we use +90 because reference rotation = 0 is at the top of the planet but sun angle = 0 is
            // when the sun points from the right.
            // var playerRotationReference = PlayerController.instance.transform.eulerAngles.z % 360 + 90;
            var playerRotationReference = referenceAngle % 360 + 90;
                
            var absoluteDifference = Mathf.Abs(playerRotationReference - sunLightAngle);
            var normalizedDifference = absoluteDifference > 180 ? 360 - absoluteDifference : absoluteDifference;
            var similarity = (1 + Mathf.Cos(normalizedDifference * Mathf.Deg2Rad)) / 2;
            
            // clip the brightness so it's fully dark/bright for longer.
            var x = GameUtilities.InverseLerp(0.33f, 0.95f, similarity);
            
            // easing the brightness change
            brightness = x * x * x * x;
            
            brightness = Mathf.Clamp(brightness, 0.003f, 1f);
            
            // Increase brightness as the player gets closer to the edge of the atmosphere
            var normalizedPlanetDistance = player.ClosestPlanetGen!.NormalizeDistanceFromPlanet(player.DistanceToClosestPlanet);
            const float threshold = 0.4f;
            var scaledInverseDistance = 1f - GameUtilities.InverseLerp(0f, threshold, normalizedPlanetDistance);
            brightness = Mathf.Lerp(brightness, 1f, scaledInverseDistance);
            
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
                // Reduce red tint as the player gets closer to the edge of the atmosphere
                redTintValue = Mathf.Lerp(redTintValue, 0f, scaledInverseDistance);
                
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
