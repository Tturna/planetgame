using System;
using System.Collections.Generic;
using UnityEngine;
using Inventory;
using Inventory.Entities;

namespace Entities
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : EntityController, IDamageable
    {
        #region Serialized Fields
        
            [Header("Components")]
            [SerializeField] private Animator handsAnimator;
            [SerializeField] private GameObject equippedItemObject;
        
            [Header("Movement Settings")]
            [SerializeField] private float accelerationSpeed;
            [SerializeField] private float maxMoveSpeed;
            [SerializeField] private float jumpForce;
            [SerializeField, Tooltip("How long can the jump key be held to increase jump force")] private float maxJumpForceTime;

            [Header("Stats Settings")]
            [SerializeField] private float maxHealth;
            [SerializeField] private float health;
            [SerializeField] private float maxEnergy;
            [SerializeField] private float energy;
            [SerializeField] private float energyRegenDelay;
            [SerializeField] private float energyRegenRate;
            [SerializeField] private float healthRegenDelay;
            [SerializeField] private float healthRegenRate;
            
        #endregion

        #region Unserialized Components
        
            private SpriteRenderer _sr;
            private Animator _animator, _recoilAnimator;
            private Transform _itemAnchor, _recoilAnchor;
            private Transform _handsParent, _handLeft, _handRight;
            private SpriteRenderer _equippedSr;
            private CameraController _camControl;
            
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

        public static PlayerController instance;
        
        private Vector2 _inputVector;
        private Vector2 _oldLocalVelocity; // Used to fix landing momentum
        private Item _equippedItem;
        private float _energyRegenTimer, _healthRegenTimer;

        private void Awake()
        {
            instance = this;
        }

        protected override void Start()
        {
            base.Start();
            
            Physics2D.queriesHitTriggers = false;
        
            _animator = GetComponent<Animator>();
            _sr = GetComponent<SpriteRenderer>();
            _equippedSr = equippedItemObject.GetComponent<SpriteRenderer>();

            _recoilAnchor = equippedItemObject.transform.parent;
            _itemAnchor = _recoilAnchor.parent;
            _recoilAnimator = _recoilAnchor.GetComponent<Animator>();

            _camControl = GetComponentInChildren<CameraController>();
            
            _handsParent = handsAnimator.transform;
            _handLeft = _handsParent.GetChild(0).GetChild(0);
            _handRight = _handsParent.GetChild(1).GetChild(0);

            InventoryManager.SlotSelected += EquipItem;
            Physics2D.queriesHitTriggers = false;
        }

        private void Update()
        {
            HandleInteraction();
            HandleGroundCheck();

            if (!CanControl) return;
            
            HandleControls();
            HandleStatsRegeneration();

            var mouseDirection = GetDirectionToMouse();
            var cursorAngle = GetCursorAngle(mouseDirection);
            
            HandleItemAiming(mouseDirection, cursorAngle);
            HandlePlayerFlipping(cursorAngle);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (CanControl)
            {
                //transform.Translate(Vector3.right * (_inputVector.x * moveSpeed));
                
                // Checking for velocity.x or y doesn't work because the player can face any direction and still be moving "right" in relation to themselves
                // That's why we use a local velocity
                var localVelocity = Rigidbody.GetVector(Rigidbody.velocity);
                if ((_inputVector.x > 0 && localVelocity.x < maxMoveSpeed) ||
                    (_inputVector.x < 0 && localVelocity.x > -maxMoveSpeed))
                {
                    Rigidbody.AddForce(transform.right * (_inputVector.x * accelerationSpeed));
                    _oldLocalVelocity = Rigidbody.GetVector(Rigidbody.velocity);
                }

                _animator.SetBool("running", _inputVector.x != 0);
                handsAnimator.SetBool("running", _inputVector.x != 0);
            }
        }

        private void LateUpdate()
        {
            // Used to fix landing issue in HandleGroundCheck function
            // _oldVelocity = Rigidbody.velocity;
        }

        private void HandleControls()
        {
            _inputVector.x = Input.GetAxis("Horizontal");

            // Jumping
            if (Input.GetKey(KeyCode.Space))
            {
                // OLD -----------
                
                // if (_jumping) return;
                //
                // Rigidbody.AddForce(_transform.up * jumpForce, ForceMode2D.Impulse);
                // _jumping = true;
                // _jumpCooldownTimer = JumpSafetyCooldown;
                
                // ---------------

                // Check if the jump key can be held to increase jump force
                if (_jumpForceTimer < maxJumpForceTime)
                {
                    _jumpForceTimer += Time.deltaTime;

                    if (!_jumping) _jumpCooldownTimer = JumpSafetyCooldown;
                    _jumping = true;
                    
                    Rigidbody.AddForce(transform.up * (jumpForce * Time.deltaTime), ForceMode2D.Impulse);
                }
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                // Prevent adding jump force in the air if the jump key is released while jumping
                _jumpForceTimer = maxJumpForceTime;
            }
            
            // Attacking
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Attack();
            }
        }

        private void HandleItemAiming(Vector3 directionToMouse, float cursorAngle)
        {
            var trForward = transform.forward;

            var cross = Vector3.Cross(-trForward, directionToMouse);
            _itemAnchor.LookAt( transform.position - trForward, cross);

            // Flip sprite when aiming right
            // var angle = Vector3.Angle(transform.right, directionToMouse);
            // _equippedSr.flipY = angle < 90;
            
            // Flip the object when aiming right
            // We do this because otherwise the recoil animation is flipped
            var scale = _recoilAnchor.localScale;
            scale.y = cursorAngle < 90 ? -1f : 1f;
            _itemAnchor.localScale = scale;
            
            // Manually set left hand position when holding an item
            if (_equippedItem != null)
            {
                handsAnimator.SetLayerWeight(1, 0f);

                var relativeOffset = _equippedItem.itemSo.handPositionOffset;
                var itemRight = equippedItemObject.transform.right;
                var itemUp = equippedItemObject.transform.up;
                
                var x = itemRight * relativeOffset.x;
                var y = itemUp * (relativeOffset.y * (cursorAngle < 90 ? -1f : 1f));
                
                var offset = x + y;

                _handLeft.position = equippedItemObject.transform.position + offset;
            }
            else
            {
                handsAnimator.SetLayerWeight(1, 1f);
                _handLeft.localPosition = Vector3.zero;
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

            var hit = Physics2D.CircleCast(transform.position, 0.2f, -transform.up, 0.4f, 1 << LayerMask.NameToLayer("World"));
            
            if (!hit) return;
            
            _jumpForceTimer = 0;
            
            if (_jumping)
            {
                _jumping = false;
                
                // Set velocity when landing to keep horizontal momentum
                var tempVel = Rigidbody.GetVector(Rigidbody.velocity);
                tempVel.x = _oldLocalVelocity.x;
                Rigidbody.velocity = Rigidbody.GetRelativeVector(tempVel);
            }
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
            
            _closestInteractable.Interact(this);
            _closestInteractable.DisablePrompt();
        }

        private void HandleStatsRegeneration()
        {
            if (_energyRegenTimer > 0)
            {
                _energyRegenTimer -= Time.deltaTime;
            }
            else
            {
                energy = Mathf.Clamp(energy + energyRegenRate * Time.deltaTime, 0, maxEnergy);
                StatsUIManager.Instance.UpdateEnergyUI(energy, maxEnergy);
            }

            if (_healthRegenTimer > 0)
            {
                _healthRegenTimer -= Time.deltaTime;
            }
            else
            {
                health = Mathf.Clamp(health + healthRegenRate * Time.deltaTime, 0, maxEnergy);
                StatsUIManager.Instance.UpdateHealthUI(health, maxHealth);
            }
        }

        private void HandlePlayerFlipping(float cursorAngle)
        {
            // if (_inputVector.x == 0) return;
            // _sr.flipX = _inputVector.x > 0;

            _sr.flipX = cursorAngle < 90;
            
            var scale = _handsParent.localScale;
            // scale.x = _inputVector.x > 0 ? -1f : 1f;
            scale.x = cursorAngle < 90 ? -1f : 1f;
            _handsParent.localScale = scale;
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
            _handsParent.gameObject.SetActive(state);
        }

        private void EquipItem(Item item)
        {
            _equippedItem = item;
            equippedItemObject.SetActive(item != null);

            if (item != null)
            {
                equippedItemObject.GetComponent<SpriteRenderer>().sprite = item.itemSo.sprite;
            }
        
            //equippedItem.transform.localPosition = item.itemSo.defaultHandPosition;
            //equippedItem.transform.localEulerAngles = item.itemSo.defaultHandRotation;
        }

        private void Attack()
        {
            if (_equippedItem?.itemSo is not WeaponSo weaponSo) return;
            if (_equippedItem.logicScript == null) return;

            if (!(energy > weaponSo.energyCost))
            {
                NoEnergy();
                return;
            }

            // Attack
            _equippedItem.logicScript.Attack(equippedItemObject, _equippedItem, _equippedSr.flipY);

            // Update energy
            energy = Mathf.Clamp(energy - weaponSo.energyCost, 0, maxEnergy);
            _energyRegenTimer = energyRegenDelay;
            StatsUIManager.Instance.UpdateEnergyUI(energy, maxEnergy);
                
            // Recoil
            _recoilAnimator.SetLayerWeight(1, weaponSo.recoilHorizontal);
            _recoilAnimator.SetLayerWeight(2, weaponSo.recoilAngular);
            _recoilAnimator.SetFloat("recoil_shpeed_horizontal", weaponSo.recoilSpeedHorizontal);
            _recoilAnimator.SetFloat("recoil_shpeed_angular", weaponSo.recoilSpeedAngular);
            _recoilAnimator.SetTrigger("recoil");
            
            // Player recoil
            var recoilDirection = -_itemAnchor.right;
            Rigidbody.AddForce(recoilDirection * weaponSo.playerRecoilStrength, ForceMode2D.Impulse);
            
            // Camera shake
            _camControl.CameraShake(weaponSo.cameraShakeTime, weaponSo.cameraShakeStrength);
        }

        private void NoEnergy()
        {
            throw new NotImplementedException();
        }

        private Vector3 GetDirectionToMouse()
        {
            var mousePosition = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            return (mousePosition - transform.position).normalized;
        }

        private float GetCursorAngle(Vector3 directionToMouse)
        {
            return Vector3.Angle(transform.right, directionToMouse);
        }
        
        public void TakeDamage(float amount)
        {
            health = Mathf.Clamp(health - amount, 0, maxHealth);
            _healthRegenTimer = healthRegenDelay;
            StatsUIManager.Instance.UpdateHealthUI(health, maxHealth);
            
            if (health <= 0) Death();
            
            //TODO: damage numbers
            
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
    
        protected override void OnTriggerEnter2D(Collider2D col)
        {
            base.OnTriggerEnter2D(col);
        
            if (col.transform.root.TryGetComponent<Interactable>(out var interactable))
            {
                _interactablesInRange.Add(interactable);
            }
            else if (col.transform.root.TryGetComponent<ItemEntity>(out var item))
            {
                if (!InventoryManager.AddItem(item.item)) return;
            
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
    }
}
