using System.Collections.Generic;
using UnityEngine;

namespace Environment
{
    [RequireComponent(typeof(EnvironmentManager))]
    public class FogManager : MonoBehaviour
    {
        [SerializeField] private Sprite[] fogSprites;
        
        private EnvironmentManager _envManager;
        
        private Planet _planet;
        private readonly List<GameObject> _fogObjects = new();
        private GameObject _fogParent;
    
        private void Start()
        {
            _planet = gameObject.transform.root.GetComponent<Planet>();
            _envManager = GetComponent<EnvironmentManager>();

            _envManager.OnFogStarted += CreateFog;
        }

        private void CreateFog(int size, float radialPosition, float maxHeight, float fogGap)
        {
            // The fog is coming
            
            _fogObjects.Clear();
            
            _fogParent = new GameObject("FogParent");
            _fogParent.transform.SetParent(transform.root);
            _fogParent.transform.localPosition = Vector3.zero;
            
            for (var i = 0; i < size; i++)
            {
                var fog = new GameObject($"Fog {i}");
                var sr = fog.AddComponent<SpriteRenderer>();
                sr.sprite = fogSprites[Random.Range(0, fogSprites.Length)];
                
                fog.transform.SetParent(_fogParent.transform);
                _fogObjects.Add(fog);

                var x = Mathf.Cos((radialPosition + i * fogGap) % 360f) * _planet.radius + Random.Range(0f, maxHeight);
                var y = Mathf.Sin((radialPosition + i * fogGap) % 360f) * _planet.radius + Random.Range(0f, maxHeight);

                fog.transform.localPosition = new Vector3(x, y, 0);
            }
        }

        public void StopFog()
        {
            
        }

        private void Update()
        {
            var windStrength = _envManager.WindStrength;
            
            foreach (var fogObject in _fogObjects)
            {
                var pos = fogObject.transform.position;
                var dirToPlanet = (_planet.transform.position - pos).normalized;
                fogObject.transform.LookAt(pos + Vector3.forward, -dirToPlanet);
                
                fogObject.transform.Translate(Vector3.right * (windStrength * (Time.deltaTime * 0.1f)), Space.Self);
            }
        }
    }
}
