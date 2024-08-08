using Entities;
using UnityEngine;

namespace Buildings
{
    [RequireComponent(typeof(Interactable))]
    [RequireComponent(typeof(Animator))]
    public class RoomDoor : MonoBehaviour
    {
        private Interactable interactable;
        private Animator animator;
        private ParticleSystem openParticles;
        
        private static readonly int AnimIsOpenBool = Animator.StringToHash("isOpen");
        private bool isOpen;

        private void Start()
        {
            interactable = GetComponent<Interactable>();
            animator = GetComponent<Animator>();
            openParticles = GetComponentInChildren<ParticleSystem>();
            interactable.OnInteractImmediate += _ => ToggleDoor();
        }

        private void ToggleDoor()
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            if (stateInfo.normalizedTime < 1f)
            {
                return;
            }
            
            isOpen = !isOpen;
            animator.SetBool(AnimIsOpenBool, isOpen);
            
            if (isOpen && openParticles)
            {
                openParticles.Play();
            }
        }
    }
}
