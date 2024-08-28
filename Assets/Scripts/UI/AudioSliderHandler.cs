using UnityEngine;
using UnityEngine.Audio;

namespace UI
{
    public class AudioSliderHandler : MonoBehaviour
    {
        [SerializeField] private AudioMixer audioMixer;
        
        private void SetVolumeParameter(string parameterName, float level)
        {
            audioMixer.SetFloat(parameterName, Mathf.Log10(level) * 20);
        }
        
        public void SetMasterVolume(float level)
        {
            SetVolumeParameter("MasterVolume", level);
        }
        
        public void SetSfxVolume(float level)
        {
            SetVolumeParameter("SFXVolume", level);
        }
        
        public void SetMusicVolume(float level)
        {
            SetVolumeParameter("MusicVolume", level);
        }
    }
}
