using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utilities
{
    public class AudioUtilities : MonoBehaviour
    {
        [Serializable]
        public struct Clip
        {
            public AudioClip audioClip;
            [Range(0f, 1f)]
            public float volume;
        }
        
        [SerializeField] private AudioSource staticAudioSource;
        [SerializeField] private AudioClip[] clips;

        private static AudioUtilities _instance;
        
        private void Awake()
        {
            _instance = this;
        }
        
        public static void PlayClip(AudioClip audioClip, float volume)
        {
            _instance.staticAudioSource.PlayOneShot(audioClip, volume);
        } 
        
        public static void PlayClip(int clipIndex, float volume)
        {
            if (clipIndex < 0 || clipIndex >= _instance.clips.Length)
            {
                Debug.LogError("Invalid audio clip index");
                return;
            }
            
            _instance.staticAudioSource.PlayOneShot(_instance.clips[clipIndex], volume);
        }
    }
}