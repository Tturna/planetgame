using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;

namespace Entities
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerStatsManager))]
    public class PlayerController : EntityController, IDamageable
    {
        #region Serialized Fields
        
            [Header("Components")]
            [SerializeField] private Animator handsAnimator; // This component is also used by HeldItemManager
            [SerializeField] private Transform handsParent;
            [SerializeField] private SpriteRenderer headSr;
            [SerializeField] private Transform extraSpritesParent;
            [SerializeField] private SpriteRenderer jetpackSr;
            [SerializeField] private ParticleSystem jetpackParticles;
            
            [Header("Other")]
            [SerializeField] private Material flashMaterial;
        
            [Header("Movement Settings")]
            [SerializeField] private float accelerationSpeed;
            [SerializeField] private float maxMoveSpeed;
            [SerializeField] private float maxSlopeMultiplier;
            [SerializeField] private float jumpForce;
            [SerializeField, Tooltip("How long can the jump key be held to increase jump force")] private float maxJumpForceTime;
            
        #endregion

        #region Unserialized Components
        
            private SpriteRenderer _sr;
            private Animator _animator;
            private CapsuleCollider2D _collider;
            
        #endregion

        #region Interaction Variables
        
            private Interactable _closestInteractable;
            private Interactable _newClosestInteractable;
            private readonly List<Interactable> _interactablesInRange = new();
            
        #endregion

        #region Jumping Variables
        
            private bool _jumping;
            private const float JumpSafetyCooldown = 0.2f; // Used to prevent another jump as the player is jumping up
            private float _jumpCooldownTimer; // Same ^
            private float _jumpForceTimer; // Used to calculate how long the jump key can be held down to jump higher
            
        #endregion
        
        #region Other

            private Material _defaultMaterial;
        
        #endregion

        public delegate void ItemPickedUpHandler(GameObject itemObject);
        public ItemPickedUpHandler ItemPickedUp;
        
        public delegate void JumpedHandler();
        public event JumpedHandler Jumped;
        
        public delegate void GroundedHandler();
        public event GroundedHandler Grounded;

        private void OnItemPickedUp(GameObject itemObject)
        {
            ItemPickedUp?.Invoke(itemObject);
        }
        
        private void OnJumped()
        {
            Jumped?.Invoke();
        }
        
        private void OnGrounded()
        {
            Grounded?.Invoke();
        }
        
        // -----------------------------------------------------
        
        public static PlayerController instance;
        
        private Vector2 _inputVector;
        private Vector2 _oldLocalVelocity; // Used to fix landing momentum
        private Vector3 _mouseDirection;
        private int _terrainLayerMask;

        // Built-in methods
        
        private void Awake()
        {
            instance = this;
            _terrainLayerMask = 1 << LayerMask.NameToLayer("Terrain");
        }

        protected override void Start()
        {
            base.Start();

            _animator = GetComponent<Animator>();
            _sr = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CapsuleCollider2D>();
            
            _defaultMaterial = _sr.material;
        }

        private void Update()
        {
            HandleInteraction();
            HandleGroundCheck();

            if (!CanControl) return;
            
            HandleControls();

            _mouseDirection = GameUtilities.GetVectorToWorldCursor(transform.position).normalized;
            var cursorAngle = GameUtilities.GetCursorAngle(_mouseDirection, transform.right);
            
            // HandleItemAiming(_mouseDirection, cursorAngle);
            HandlePlayerFlipping(cursorAngle);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            var localVelocity = Rigidbody.GetVector(Rigidbody.velocity);
            _oldLocalVelocity = localVelocity;

            // Reduce friction when moving
            var pmat = _collider.sharedMaterial;
            
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (Mathf.Abs(_inputVector.x) > 0.1f)
            {
                pmat.friction = 0.3f;
            }
            else
            {
                pmat.friction = Mathf.Lerp(0.85f, 0.2f, localVelocity.magnitude / maxMoveSpeed);
            }
            
            _collider.sharedMaterial = pmat; // quite the meme

            if (CanControl)
            {
                var force = transform.right * (_inputVector.x * accelerationSpeed);
                
                // Figure out the slope angle of the terrain that the player is walking on
                var rayStartPoint = transform.position - transform.up * 0.4f;

                // Raycast "below". It's actually a bit to the side as well
                var rayBelowDirection = new Vector2(.1f * _inputVector.x, -.4f).normalized;
                var hitBelow = Physics2D.Raycast(rayStartPoint, rayBelowDirection, .25f, _terrainLayerMask);
                
                // Raycast a bit to the side, depending on movement direction
                var raySideDirection = new Vector2(.1f * _inputVector.x, -.2f).normalized;
                var hitSide = Physics2D.Raycast(rayStartPoint, raySideDirection, .3f, _terrainLayerMask);
                // Debug.DrawLine(rayStartPoint, rayStartPoint + (Vector3)rayBelowDirection * 1.05f, Color.green);
                // Debug.DrawLine(rayStartPoint, rayStartPoint + (Vector3)raySideDirection * 1.1f, Color.red);
                
                if (hitBelow && hitSide)
                {
                    // Move direction is the vector from the bottom raycast to the side raycast
                    var direction = (hitSide.point - hitBelow.point).normalized;
                    
                    // Check if the direction is upwards relative to the player
                    var dot = Vector3.Dot(transform.up, direction);
                    // Debug.Log(dot);
                    // Debug.DrawLine(transform.position, transform.position + (Vector3)direction, Color.magenta);

                    if (dot > 0)
                    {
                        force = direction * (accelerationSpeed * Mathf.Clamp(1f + dot * 4f, 1f, maxSlopeMultiplier));
                    }
                }
                
                // Checking for velocity.x or y doesn't work because the player can face any direction and still be moving "right" in relation to themselves
                // That's why we use a local velocity
                if ((_inputVector.x > 0 && localVelocity.x < maxMoveSpeed) ||
                    (_inputVector.x < 0 && localVelocity.x > -maxMoveSpeed))
                {
                    Rigidbody.AddForce(force);
                    // _oldLocalVelocity = Rigidbody.GetVector(Rigidbody.velocity);
                }

                _animator.SetBool("running", _inputVector.x != 0);
                handsAnimator.SetBool("running", _inputVector.x != 0);
                
                // Jumping
                if (_jumping && _jumpForceTimer < maxJumpForceTime)
                {
                    // The jump force timer is here so it syncs with physics
                    _jumpForceTimer += Time.deltaTime;
                    Rigidbody.AddForce(transform.up * (jumpForce * Time.deltaTime), ForceMode2D.Impulse);
                }
            }
        }

        protected override void OnTriggerEnter2D(Collider2D col)
        {
            base.OnTriggerEnter2D(col);
        
            if (col.transform.root.TryGetComponent<Interactable>(out var interactable))
            {
                _interactablesInRange.Add(interactable);
            }
            else if (col.gameObject.CompareTag("Item"))
            {
                OnItemPickedUp(col.transform.parent.gameObject);
                Destroy(col.transform.parent.gameObject);
            }
        }

        protected override void OnTriggerExit2D(Collider2D other)
        {
            base.OnTriggerEnter2D(other);
        
            if (other.transform.root.TryGetComponent<Interactable>(out var interactable))
            {
                interactable.DisablePrompt();

                if (interactable == _closestInteractable) _closestInteractable = null;
            
                _interactablesInRange.Remove(interactable);
            }
        }

        private void OnDrawGizmos()
        {
            var mousePos = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(mousePos, 0.5f);
        }

        // Private methods
        
        private void HandleControls()
        {
            _inputVector.x = Input.GetAxis("Horizontal");

            // Jumping
            if (Input.GetKey(KeyCode.Space))
            {
                // Check if the jump key can be held to increase jump force
                if (_jumpForceTimer >= maxJumpForceTime) return;
                
                if (!_jumping)
                {
                    _jumpCooldownTimer = JumpSafetyCooldown;
                        
                    _animator.SetBool("jumping", true);
                    handsAnimator.SetBool("jumping", true);
                        
                    var tempVel = Rigidbody.GetVector(Rigidbody.velocity);
                    tempVel.y = 0f;
                    Rigidbody.velocity = Rigidbody.GetRelativeVector(tempVel);
                    
                    OnJumped();
                }
                _jumping = true;
                    
                // Actual jumping physics and the timer are in FixedUpdate()
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                // Prevent adding jump force in the air if the jump key is released while jumping
                _jumpForceTimer = maxJumpForceTime;
            }
        }

        private void HandleGroundCheck()
        {
            // This is to make sure the raycast doesn't allow another jump as the player is moving up
            if (_jumpCooldownTimer > 0)
            {
                _jumping = true;
                _jumpCooldownTimer -= Time.deltaTime;
                return;
            }
        
            // var hit = Physics2D.Raycast(_transform.position, -_transform.up, 0.6f, 1 << LayerMask.NameToLayer("World"));

            var mask = 1 << LayerMask.NameToLayer("Terrain") | 1 << LayerMask.NameToLayer("TerrainBits");
            var hit = Physics2D.CircleCast(transform.position, 0.2f, -transform.up, 0.4f, mask);

            if (!hit) return;

            // Allow another jump when landing
            _jumpForceTimer = 0;

            if (!_jumping) return;
            _jumping = false;
            _animator.SetBool("jumping", false);
            handsAnimator.SetBool("jumping", false);
                
            // Set velocity when landing to keep horizontal momentum
            var tempVel = Rigidbody.GetVector(Rigidbody.velocity);
            tempVel.x = _oldLocalVelocity.x;
            Rigidbody.velocity = Rigidbody.GetRelativeVector(tempVel);
            
            OnGrounded();
        }

        private void HandleInteraction()
        {
            // Interaction
            if (_interactablesInRange.Count <= 0) return;
        
            // Check for closest interactable when moving
            if (_inputVector.magnitude > 0)
            {
                // Find closest interactable
                _newClosestInteractable = GetClosestInteractable();

                // Check if the closest interactable is the same as the previous closest one
                if (_newClosestInteractable != _closestInteractable)
                {
                    // Disable the prompt on the old one if there is one
                    if (_closestInteractable) _closestInteractable.DisablePrompt();
                    
                    _closestInteractable = _newClosestInteractable;
                    _closestInteractable.PromptInteraction();
                }
            }

            if (!Input.GetKeyDown(KeyCode.F)) return;
            
            _closestInteractable.Interact(gameObject);
            _closestInteractable.DisablePrompt();
        }

        private void HandlePlayerFlipping(float cursorAngle)
        {
            // if (_inputVector.x == 0) return;
            // _sr.flipX = _inputVector.x > 0;

            _sr.flipX = cursorAngle > 90;
            headSr.flipX = cursorAngle > 90;
            
            var scale = handsParent.localScale;
            // scale.x = _inputVector.x > 0 ? -1f : 1f;
            scale.x = cursorAngle < 90 ? -1f : 1f;
            handsParent.localScale = scale;
            
            // Flip the extra sprites
            scale = extraSpritesParent.localScale;
            scale.x = cursorAngle > 90 ? -1f : 1f;
            extraSpritesParent.localScale = scale;
        }

        private Interactable GetClosestInteractable()
        {
            var closest = _interactablesInRange[0];

            foreach (var interactable in _interactablesInRange)
            {
                var distToCurrent = (closest.transform.position - transform.position).magnitude;
                var distToNew = (interactable.transform.position - transform.position).magnitude;

                if (distToNew < distToCurrent)
                {
                    closest = interactable;
                }
            }

            return closest;
        }

        public override void ToggleSpriteRenderer(bool state)
        {
            _sr.enabled = state;
            handsParent.gameObject.SetActive(state);
        }

        
        // Public methods
        
        public void TakeDamage(float amount)
        {
            var died = PlayerStatsManager.ChangeHealth(-amount);
            if (died) Death();
            //TODO: damage numbers

            // Make the player flash red unless the game is run in the editor.
            // For some reason the editor lags like a motherfucker because of this.
            if (!Application.isEditor)
            {
                _sr.material = flashMaterial;
                headSr.material = flashMaterial;
                GameUtilities.instance.DelayExecute(() =>
                {
                    _sr.material = _defaultMaterial;
                    headSr.material = _defaultMaterial;
                }, 0.1f);
            }
            
            CameraController.CameraShake(0.1f, 0.1f);
            
            Debug.Log($"Took {amount} damage!");
        }

        public void Knockback(Vector3 damageSourcePosition, float amount)
        {
            if (amount == 0) return;
            
            Rigidbody.velocity = Vector2.zero;
            var knockbackDirection = (transform.position - damageSourcePosition).normalized;
            Rigidbody.AddForce(knockbackDirection * amount, ForceMode2D.Impulse);
        }

        public void Death()
        {
            Debug.Log("Death.");
        }

        /// <summary>
        /// Reset Rigidbody2D.velocity on given axis. Pass "local" as true to reset velocity relative to player orientation.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="local"></param>
        public void ResetVelocity(bool x, bool y, bool local)
        {
            if (local)
            {
                var vel = Rigidbody.GetVector(Rigidbody.velocity);
                if (x) vel.x = 0f;
                if (y) vel.y = 0f;
                Rigidbody.velocity = Rigidbody.GetRelativeVector(vel);
            }
            else
            {
                var vel = Rigidbody.velocity;
                if (x) vel.x = 0f;
                if (y) vel.y = 0f;
                Rigidbody.velocity = vel;
            }
        }

        public void AddForceTowardsCursor(float magnitude)
        {
            Rigidbody.AddForce(_mouseDirection * magnitude);
        }

        public void SetJetpackSprite(Sprite sprite)
        {
            jetpackSr.sprite = sprite;
        }

        public ParticleSystem GetJetpackParticles()
        {
            return jetpackParticles;
        }
    }
}
