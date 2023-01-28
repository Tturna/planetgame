using System;
using System.Collections;
using UnityEngine;
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

        private void TriggerOnFogStarted()
        {
            OnFogStarted?.Invoke(Random.Range(20, 50), Random.Range(0f, 360f), 1.2f, 0.01f);
        }

        private void Start()
        {
            TriggerOnFogStarted();

            StartCoroutine(ChangeWind());
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
