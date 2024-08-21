using System;
using JetBrains.Annotations;
using UnityEngine;
using Utilities;

namespace Entities
{
    public class Interactable : MonoBehaviour
    {
        [SerializeField] protected float interactRange;
        [SerializeField] private GameObject promptPrefab;
        [SerializeField] private GameObject holdIndicatorPrefab;
        [SerializeField] private Vector2 promptOffset;
        [SerializeField] private Vector2 holdIndicatorOffset;
        [SerializeField] private Material outlineMaterial;

        public bool canHoldInteract;
        // ReSharper disable once InconsistentNaming
        [NonSerialized] public Transform IndicatorParent;

        private GameObject _promptObject;
        private GameObject _holdIndicatorObject;
        private GameObject _outlineParent;
        private PlayerController _player;
        [CanBeNull] private SpriteRenderer spriteRenderer;
        private float _distanceToPlayer;
        private bool _inRange;
        private float _interactHoldTimer;
        private bool _interacted;

        public delegate void OnInteractEventHandler(GameObject sourceObject);
        public delegate void OnWithinRangeEventHandler(GameObject sourceObject);
        public delegate void OnOutOfRangeEventHandler(GameObject sourceObject);
        public event OnInteractEventHandler OnInteractImmediate;
        public event OnInteractEventHandler OnInteractHold;
        public event OnWithinRangeEventHandler OnWithinRange;
        public event OnOutOfRangeEventHandler OnOutOfRange;
    
        private void Start()
        {
            _player = PlayerController.instance;
            
            IndicatorParent = new GameObject("IndicatorParent").transform;
            IndicatorParent.SetParent(transform);
            IndicatorParent.localPosition = Vector3.zero;
            
            _promptObject = Instantiate(promptPrefab, IndicatorParent);
            _promptObject.transform.localPosition = promptOffset;
            _promptObject.SetActive(false);

            if (!_holdIndicatorObject)
            {
                if (holdIndicatorPrefab)
                {
                    _holdIndicatorObject = Instantiate(holdIndicatorPrefab, IndicatorParent);
                    _holdIndicatorObject.transform.localPosition = holdIndicatorOffset;
                    _holdIndicatorObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("Hold indicator prefab not set for interactable object.");
                }
            }

            if (TryGetComponent<SpriteRenderer>(out var sr))
            {
                spriteRenderer = sr;
            }
        }

        // This system is not using trigger collider events because they seem to be VERY unreliable.
        private void Update()
        { 
            // If this code is slow, consider implementing a quadtree to reduce the distance checks
            // to only interactables within a certain range of the player.
            _distanceToPlayer = Vector2.Distance(transform.position, _player.transform.position);
            
            if (!_inRange && _distanceToPlayer <= interactRange)
            {
                _inRange = true;
                _player.AddInteractableInRange(this);
                TriggerOnWithinRange(_player.gameObject);
            }
            else if (_inRange && _distanceToPlayer > interactRange)
            {
                _inRange = false;
                _player.RemoveInteractableInRange(this);
                DisablePrompt();
                ResetInteracted();
                TriggerOnOutOfRange(_player.gameObject);
            }
        }

        public virtual void EnablePrompt()
        {
            _promptObject.SetActive(true);

            if (!spriteRenderer) return;

            // TODO: Optimize so objects are not created and destroyed all the time
            // This could probably do with a better system altogether.
            // Consider a shader that increases the sprite size by moving vertices and
            // then downsizing the sprite to procedurally add padding to sprites so that they
            // can fit an outline. Also consider a second shader pass or multiple materials.
            
            _outlineParent = new GameObject("Outline Parent");
            _outlineParent.transform.SetParent(transform);
            _outlineParent.transform.localPosition = Vector3.zero;
            _outlineParent.transform.rotation = transform.rotation;

            for (var i = 0; i < 4; i++)
            {
                int x, y;

                if (i < 2)
                {
                    x = 0;
                    y = i * 2 - 1;
                }
                else
                {
                    x = (i - 2) * 2 - 1;
                    y = 0;
                }
                
                var outline = new GameObject("Outline");
                outline.transform.SetParent(_outlineParent.transform);
                outline.transform.localPosition = new Vector2(x / 16f, y / 16f);
                outline.transform.rotation = transform.rotation;
                
                var sr = outline.AddComponent<SpriteRenderer>();
                sr.sprite = spriteRenderer.sprite;
                sr.sortingLayerName = spriteRenderer.sortingLayerName;
                sr.sortingOrder = spriteRenderer.sortingOrder - 1;
                sr.material = outlineMaterial;
            }
        }

        public virtual void DisablePrompt()
        {
            if (_promptObject)
            {
                _promptObject.SetActive(false);
            }

            if (_outlineParent)
            {
                Destroy(_outlineParent);
            }
        }

        public virtual void InteractImmediate(GameObject sourceObject)
        {
            // Debug.Log($"{sourceObject.name} interacted with {gameObject.name}.");
            TriggerOnInteractImmediate(sourceObject);
        }
        
        public virtual void InteractHolding(GameObject sourceObject)
        {
            if (!canHoldInteract)
            {
                if (_holdIndicatorObject.activeSelf)
                {
                    _holdIndicatorObject.SetActive(false);
                }
                
                return;
            }

            if (_interacted) return;

            if (_interactHoldTimer == 0)
            {
                _holdIndicatorObject.SetActive(true);
            }

            if (_interactHoldTimer < 1f)
            {
                _interactHoldTimer += Time.deltaTime;
            }
            else
            {
                _interacted = true;
                _interactHoldTimer = 0f;
                TriggerOnInteractHold(sourceObject);
                GameUtilities.instance.DelayExecute(() =>
                {
                    _holdIndicatorObject.SetActive(false);
                }, 0.2f);
            }
        }
        
        public virtual void ResetInteracted()
        {
            _interactHoldTimer = 0f;
            _interacted = false;
            if (_holdIndicatorObject)
            {
                _holdIndicatorObject.SetActive(false);
            }
        }

        protected virtual void TriggerOnInteractImmediate(GameObject sourceObject)
        {
            OnInteractImmediate?.Invoke(sourceObject);
        }
        
        protected virtual void TriggerOnInteractHold(GameObject sourceObject)
        {
            OnInteractHold?.Invoke(sourceObject);
        }
        
        protected virtual void TriggerOnWithinRange(GameObject sourceObject)
        {
            OnWithinRange?.Invoke(sourceObject);
        }
        
        protected virtual void TriggerOnOutOfRange(GameObject sourceObject)
        {
            OnOutOfRange?.Invoke(sourceObject);
        }
    }
}