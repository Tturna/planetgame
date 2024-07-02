using System.Collections;
using UnityEngine;
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

        [SerializeField, Tooltip("Minimum and maximum time in seconds before the wind strength is changed. Strength is chosen randomly.")]
        private Vector2 windChangeIntervalGap;

        [SerializeField] private float maxWindStrength;
        [SerializeField, Range(0, 1)] private float brightness;
        
        private Material _terrainMaterial;
        private static readonly int TerrainShaderBrightness = Shader.PropertyToID("_Brightness");

        private void TriggerOnFogStarted()
        {
            OnFogStarted?.Invoke(Random.Range(20, 50), Random.Range(0f, 360f), 1.2f, 0.01f);
        }

        private void Start()
        {
            _terrainMaterial = GameUtilities.GetTerrainMaterial();
            // TriggerOnFogStarted();

            StartCoroutine(ChangeWind());
        }

        private void Update()
        {
            // brightness -= Time.deltaTime * 0.1f;
            
            _terrainMaterial.SetFloat(TerrainShaderBrightness, brightness);
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
