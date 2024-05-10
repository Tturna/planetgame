using System;
using UnityEngine;

namespace Entities
{
    public class Interactable : MonoBehaviour
    {
        [SerializeField] protected float interactRange;
        [SerializeField] private GameObject promptPrefab;
        [SerializeField] private Vector2 promptOffset;

        public bool hasToggleInteraction;

        private GameObject _promptObject;
        private PlayerController _player;
        private float _distanceToPlayer;
        private bool _inRange;

        public delegate void OnInteractEventHandler(GameObject sourceObject);
        public event OnInteractEventHandler Interacted;
    
        private void Start()
        {
            _player = FindObjectOfType<PlayerController>();
        }

        // This system is not using trigger collider events because they seem to be VERY unreliable.
        private void Update()
        { 
            // TODO: Consider optimizing this so it doesn't run every frame.
            // Maybe check if this interactable is in the same planet as the player.
            _distanceToPlayer = Vector2.Distance(transform.position, _player.transform.position);
            
            if (!_inRange && _distanceToPlayer <= interactRange)
            {
                _inRange = true;
                _player.AddInteractableInRange(this);
            }
            else if (_inRange && _distanceToPlayer > interactRange)
            {
                _inRange = false;
                _player.RemoveInteractableInRange(this);
                DisablePrompt();
            }
        }

        public virtual void EnablePrompt()
        {
            if (!_promptObject)
            {
                _promptObject = Instantiate(promptPrefab, transform);
                _promptObject.transform.localPosition = promptOffset;
            }
        
            _promptObject.SetActive(true);
        }

        public virtual void DisablePrompt()
        {
            if (_promptObject)
            {
                _promptObject.SetActive(false);
            }
        }

        public virtual void Interact(GameObject sourceObject)
        {
            // Debug.Log($"{sourceObject.name} interacted with {gameObject.name}.");
            OnInteracted(sourceObject);
        }

        protected virtual void OnInteracted(GameObject sourceObject)
        {
            Interacted?.Invoke(sourceObject);
        }
    }
}