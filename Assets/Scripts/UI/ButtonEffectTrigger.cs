using JetBrains.Annotations;
using UnityEngine;

namespace UI
{
    public class ButtonEffectTrigger : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip highlightSound;
        [SerializeField] private AudioClip pressedSound;
        
        // All methods with [UsedImplicitly] are designed to be called from animation events.
        [UsedImplicitly]
        public void ButtonHighlighted()
        {
            audioSource.PlayOneShot(highlightSound);
        }

        [UsedImplicitly]
        public void ButtonPressed()
        {
            audioSource.PlayOneShot(pressedSound);
        }
    }
}
