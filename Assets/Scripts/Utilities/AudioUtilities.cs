using System;
using UnityEngine;

namespace Utilities
{
    public static class AudioUtilities
    {
        [Serializable]
        public struct Clip
        {
            public AudioClip audioClip;
            [Range(0f, 1f)]
            public float volume;
        }
    }
}