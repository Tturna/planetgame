using System;
using Entities;
using UnityEngine;

namespace Buildings
{
    [RequireComponent(typeof(Animator))]
    public class RoomDoor : MonoBehaviour
    {
        [SerializeField] private float openDistance;
        
        private Transform playerTransform;
        private Animator animator;
        private ParticleSystem openParticles;
        
        private static readonly int AnimIsOpenBool = Animator.StringToHash("isOpen");
        private bool isOpen;

        private void Start()
        {
            playerTransform = PlayerController.instance.transform;
            animator = GetComponent<Animator>();
            openParticles = GetComponentInChildren<ParticleSystem>();
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
            
            if (isOpen && openParticles)
            {
                openParticles.Play();
            }
        }
    }
}
