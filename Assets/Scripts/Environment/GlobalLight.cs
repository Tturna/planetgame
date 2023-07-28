using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Environment
{
    public class GlobalLight : MonoBehaviour
    {
        public static GlobalLight instance;
        
        private Light2D _globalLight;

        private void Start()
        {
            instance = this;
            _globalLight = GetComponent<Light2D>();
        }
        
        public void SetIntensity(float intensity)
        {
            // Maybe this clamp is unnecessary?
            // It can probably be removed if the intensity should occasionally go above 1.
            intensity = Mathf.Clamp01(intensity);
            _globalLight.intensity = intensity;
        }
    }
}
