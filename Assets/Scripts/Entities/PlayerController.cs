using System;
using System.Collections.Generic;
using System.Linq;
using Cameras;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;

namespace Entities
{
    [RequireComponent(typeof(PlayerStatsManager))]
    [RequireComponent(typeof(PlayerDeathManager))]
    public class PlayerController : EntityController, IDamageable
    {
        #region Serialized Fields
        
            [Header("Components")]
            [SerializeField] private Animator handsAnimator; // This component is also used by HeldItemManager
            [SerializeField] private Transform handsParent;
            [SerializeField] private Transform extraSpritesParent;
            [SerializeField] private SpriteRenderer jetpackSr;
            [SerializeField] private Transform bodyTr;
            [SerializeField] private ParticleSystem jetpackParticles1, jetpackParticles2;
            [SerializeField] private SpriteRenderer headSr;
            [FormerlySerializedAs("bodySr")] [SerializeField] private SpriteRenderer torsoSr;
            [FormerlySerializedAs("bodyAnimator")] [SerializeField] private Animator torsoAnimator;
            [SerializeField] private GameObject itemAnchor;
            [SerializeField] private Transform starmapCamera;
            
            [Header("Other")]
            [SerializeField] private Material flashMaterial;
        
            [Header("Movement Settings")]
            [SerializeField] private float maxSlopeMultiplier;
            [SerializeField, Tooltip("How long can the jump key be held to increase jump force")] private float maxJumpForceTime;
            
        #endregion

        #region Unserialized Components
        
            private CapsuleCollider2D _collider;
            private PlayerDeathManager _deathManager;
            
        #endregion

        #region Interaction Variables
        
            private Interactable _closestInteractable;
            private Interactable _newClosestInteractable;
            private readonly List<Interactable> _interactablesInRange = new();
            private float _interactHoldTimer;
            private bool _interacted;
            
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
        public ItemPickedUpHandler itemPickedUp;
        
        public delegate void JumpedHandler();
        public event JumpedHandler Jumped;
        
        public delegate void GroundedHandler();
        public event GroundedHandler Grounded;

        private void OnItemPickedUp(GameObject itemObject)
        {
            itemPickedUp?.Invoke(itemObject);
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
        private int _collisionLayerMask;
        private Vector2 _spawnPosition;

        // Built-in methods
        
        private void Awake()
        {
            instance = this;
            _collisionLayerMask = GameUtilities.BasicMovementCollisionMask;
        }

        protected override void Start()
        {
            base.Start();

            _collider = GetComponent<CapsuleCollider2D>();
            _deathManager = GetComponent<PlayerDeathManager>();
            
            _defaultMaterial = torsoSr.material;
            _spawnPosition = transform.position;
        }

        private void Update()
        {
            HandleInteraction();
            HandleGroundCheck();
            
            starmapCamera.rotation = Quaternion.identity;

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
                pmat.friction = Mathf.Lerp(0.85f, 0.2f, localVelocity.magnitude / PlayerStatsManager.MaxMoveSpeed);
            }
            
            _collider.sharedMaterial = pmat; // quite the meme

            if (CanControl)
            {
                var force = transform.right * (_inputVector.x * PlayerStatsManager.AccelerationSpeed);
                
                // Figure out the slope angle of the terrain that the player is walking on
                var rayStartPoint = transform.position - transform.up * 0.4f;

                // Raycast "below". It's actually a bit to the side as well
                var rayBelowDirection = new Vector2(.1f * _inputVector.x, -.4f).normalized;
                var hitBelow = Physics2D.Raycast(rayStartPoint, rayBelowDirection, .25f, _collisionLayerMask);
                
                // Raycast a bit to the side, depending on movement direction
                var raySideDirection = new Vector2(.1f * _inputVector.x, -.2f).normalized;
                var hitSide = Physics2D.Raycast(rayStartPoint, raySideDirection, .3f, _collisionLayerMask);
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
                        force = direction * (PlayerStatsManager.AccelerationSpeed * Mathf.Clamp(1f + dot * 4f, 1f, maxSlopeMultiplier));
                    }
                }
                
                // Checking for velocity.x or y doesn't work because the player can face any direction and still be moving "right" in relation to themselves
                // That's why we use a local velocity
                if ((_inputVector.x > 0 && localVelocity.x < PlayerStatsManager.MaxMoveSpeed) ||
                    (_inputVector.x < 0 && localVelocity.x > -PlayerStatsManager.MaxMoveSpeed))
                {
                    Rigidbody.AddForce(force);
                    // _oldLocalVelocity = Rigidbody.GetVector(Rigidbody.velocity);
                }

                torsoAnimator.SetBool("running", _inputVector.x != 0);
                handsAnimator.SetBool("running", _inputVector.x != 0);
                
                // Jumping
                if (_jumping && _jumpForceTimer < maxJumpForceTime)
                {
                    // The jump force timer is here so it syncs with physics
                    _jumpForceTimer += Time.deltaTime;
                    Rigidbody.AddForce(transform.up * (PlayerStatsManager.JumpForce * Time.deltaTime), ForceMode2D.Impulse);
                }
            }
        }

        protected void OnTriggerEnter2D(Collider2D col)
        {
            // if (col.transform.root.TryGetComponent<Interactable>(out var interactable))
            // {
            //     _interactablesInRange.Add(interactable);
            // }
            
            if (col.gameObject.CompareTag("Item"))
            {
                OnItemPickedUp(col.transform.parent.gameObject);
                Destroy(col.transform.parent.gameObject);
            }
        }

        // protected void OnTriggerExit2D(Collider2D other)
        // {
        //     if (other.transform.root.TryGetComponent<Interactable>(out var interactable))
        //     {
        //         interactable.DisablePrompt();
        //         
        //         if (interactable == _closestInteractable) _closestInteractable = null;
        //         
        //         _interactablesInRange.Remove(interactable);
        //     }
        // }

        private void OnDrawGizmos()
        {
            var mousePos = CameraController.instance.mainCam.ScreenToWorldPoint(Input.mousePosition);
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
                        
                    torsoAnimator.SetBool("jumping", true);
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

            var mask = GameUtilities.BasicMovementCollisionMask;
            var hit = Physics2D.CircleCast(transform.position, 0.2f, -transform.up, 0.4f, mask);

            if (!hit) return;

            // Allow another jump when landing
            _jumpForceTimer = 0;

            if (!_jumping) return;
            _jumping = false;
            torsoAnimator.SetBool("jumping", false);
            handsAnimator.SetBool("jumping", false);
                
            // Set velocity when landing to keep horizontal momentum
            var tempVel = Rigidbody.GetVector(Rigidbody.velocity);
            tempVel.x = _oldLocalVelocity.x;
            Rigidbody.velocity = Rigidbody.GetRelativeVector(tempVel);
            
            OnGrounded();
        }

        private void HandleInteraction()
        {
            if (_interactablesInRange.Count <= 0) return;
        
            // This line causes a crash if the player places a placeable interactable item next to them while not moving.
            // For example, placing a crafting station next to the player while standing still.
            // if (_inputVector.magnitude > 0 || Rigidbody.velocity.magnitude > 0.05f)
            {
                _newClosestInteractable = GetClosestInteractable();

                if (_newClosestInteractable != _closestInteractable)
                {
                    if (_closestInteractable)
                    {
                        _closestInteractable.DisablePrompt();
                        _closestInteractable.ResetInteracted();
                    }
                    
                    _closestInteractable = _newClosestInteractable;
                    _closestInteractable.EnablePrompt();
                }
            }
            
            if (!_closestInteractable) return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                _closestInteractable.DisablePrompt();
                _closestInteractable.InteractImmediate(gameObject);
            }

            if (!_closestInteractable.canHoldInteract) return;
            
            if (Input.GetKey(KeyCode.F))
            {
                _closestInteractable.InteractHolding(gameObject);
            }
            
            if (Input.GetKeyUp(KeyCode.F))
            {
                _closestInteractable.ResetInteracted();
            }
        }

        private void HandlePlayerFlipping(float cursorAngle)
        {
            // if (_inputVector.x == 0) return;
            // _sr.flipX = _inputVector.x > 0;

            torsoSr.flipX = cursorAngle > 90;
            headSr.flipX = cursorAngle > 90;
            
            var scale = handsParent.localScale;
            // scale.x = _inputVector.x > 0 ? -1f : 1f;
            scale.x = cursorAngle < 90 ? -1f : 1f;
            handsParent.localScale = scale;
            
            scale = extraSpritesParent.localScale;
            scale.x = cursorAngle > 90 ? -1f : 1f;
            extraSpritesParent.localScale = scale;
        }

        private Interactable GetClosestInteractable()
        {
            var closest = _interactablesInRange.FirstOrDefault(i => i != null);

            if (!closest)
            {
                _interactablesInRange.Clear();
                _closestInteractable = null;
                return null;
            }
            
            var closestDist = (closest.transform.position - transform.position).magnitude;

            foreach (var interactable in _interactablesInRange)
            {
                if (!interactable) continue;
                
                var distToNew = (interactable.transform.position - transform.position).magnitude;

                if (!(distToNew < closestDist)) continue;
                closest = interactable;
                closestDist = distToNew;
            }

            return closest;
        }

        public override void ToggleSpriteRenderer(bool state)
        {
            torsoSr.enabled = state;
            headSr.enabled = state;
            handsParent.gameObject.SetActive(state);
            itemAnchor.SetActive(state);
        }
        
        // Public methods
        
        public void TakeDamage(float amount)
        {
            amount = Mathf.Clamp(amount - PlayerStatsManager.Defense, 0, amount);
            var died = PlayerStatsManager.ChangeHealth(-amount);
            if (died) Death();
            //TODO: damage numbers

            // Make the player flash red unless the game is run in the editor.
            // For some reason the editor lags like a motherfucker because of this.
            if (!Application.isEditor)
            {
                torsoSr.material = flashMaterial;
                headSr.material = flashMaterial;
                GameUtilities.instance.DelayExecute(() =>
                {
                    torsoSr.material = _defaultMaterial;
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
            IsAlive = false;
            bodyTr.gameObject.SetActive(false);
            _collider.enabled = false;
            ToggleControl(false);
            TogglePhysics(false);
            
            var skullObject = _deathManager.Explode(10f);
            CameraController.SetParent(skullObject.transform);
            CameraController.SetDefaultPosition(Vector2.zero);
            CameraController.CameraShake(0.5f, 0.5f);
            
            UIUtilities.ShowDeathOverlay();
            
            GameUtilities.instance.DelayExecute(() =>
            {
                IsAlive = true;
                bodyTr.gameObject.SetActive(true);
                _collider.enabled = true;
                ToggleControl(true);
                TogglePhysics(true);
                transform.position = _spawnPosition;
                CameraController.SetParent(transform);
                CameraController.ResetDefaultPosition();
                UIUtilities.HideDeathOverlay();
            }, 10f);
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

        public (ParticleSystem, ParticleSystem) GetJetpackParticles()
        {
            return (jetpackParticles1, jetpackParticles2);
        }

        public Transform GetBodyTransform()
        {
            return bodyTr;
        }
        
        public Vector2 GetInputVector()
        {
            return _inputVector;
        }
        
        public void AddInteractableInRange(Interactable interactable)
        {
            _interactablesInRange.Add(interactable);
        }

        public void RemoveInteractableInRange(Interactable interactable)
        {
            _interactablesInRange.Remove(interactable);
            if (interactable == _closestInteractable)
            {
                _closestInteractable = null;
            }
        }
    }
}
