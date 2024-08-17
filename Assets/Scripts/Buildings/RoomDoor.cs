using System;
using Entities;
using UnityEngine;

namespace Buildings
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(AudioSource))]
    public class RoomDoor : MonoBehaviour
    {
        [SerializeField] private float openDistance;
        [SerializeField] private AudioClip openSound;
        
        private Transform playerTransform;
        private Animator animator;
        private ParticleSystem openParticles;
        private AudioSource audioSource;
        
        private static readonly int AnimIsOpenBool = Animator.StringToHash("isOpen");
        private bool isOpen;

        private void Start()
        {
            playerTransform = PlayerController.instance.transform;
            animator = GetComponent<Animator>();
            openParticles = GetComponentInChildren<ParticleSystem>();
            audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            var distanceToPlayer = Vector3.Distance(playerTransform.position, transform.position);
            
            if (!isOpen && distanceToPlayer < openDistance)
            {
                ToggleDoor(true);
            }
            else if (isOpen && distanceToPlayer >= openDistance)
            {
                ToggleDoor(false);
            }
        }

        private void ToggleDoor(bool state)
        {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            if (stateInfo.normalizedTime < 1f)
            {
                return;
            }
            
            isOpen = state;
            animator.SetBool(AnimIsOpenBool, isOpen);
            audioSource.PlayOneShot(openSound);
            
            if (isOpen && openParticles)
            {
                openParticles.Play();
            }
        }
    }
}
