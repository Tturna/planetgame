using System.Collections;
using Entities.Entities;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Entities
{
    [RequireComponent(typeof(HealthbarManager))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    
    // Sealed - can't be inherited
    public sealed class EnemyEntity : EntityController, IDamageable
    {
        [SerializeField] private EnemySo enemySo;
        
        public Vector3 relativeMoveDirection, globalMoveDirection;
        
        private PlayerController _player;
        private Animator _animator;
        private SpriteRenderer _sr;
        private HealthbarManager _healthbarManager;
        private Shader _defaultShader, _flashShader;
        
        private float _calculationTimer, _evasionTimer, _attackTimer;
        private float _distanceToPlayer;
        private float _health, _maxHealth;
        private bool _jumping, _aggravated, _canMove = true;

        protected override void Start()
        {
            base.Start();
            
            _player = PlayerController.instance;
            _animator = GetComponent<Animator>();
            _sr = GetComponent<SpriteRenderer>();
            _healthbarManager = GetComponent<HealthbarManager>();
            
            _animator.runtimeAnimatorController = enemySo.overrideAnimator;

            _evasionTimer = enemySo.evasionTime;
            _attackTimer = enemySo.attackInterval;
            _health = enemySo.health;
            _maxHealth = enemySo.maxHealth;
            
            _healthbarManager.Initialize(_health, _maxHealth, enemySo.isBoss, enemySo.healthbarDistance);

            var hitboxChild = new GameObject("Hitbox");
            hitboxChild.transform.SetParent(transform);
            hitboxChild.transform.localPosition = Vector3.zero;
            hitboxChild.layer = 8;

            var hitbox = hitboxChild.AddComponent<BoxCollider2D>();
            hitbox.offset = enemySo.hitboxOffset;
            hitbox.size = enemySo.hitboxSize;
            hitbox.isTrigger = true;
            
            _defaultShader = _sr.material.shader;
            _flashShader = Shader.Find("GUI/Text Shader");
        }

        private void Update()
        {
            if (!CalculatePlayerRelation()) return;

            CheckAggro();
            
            globalMoveDirection = relativeMoveDirection.x > 0 ? transform.right : -transform.right;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            if (_aggravated)
            {
                AggroBehavior();
            }
        }

        private bool CalculatePlayerRelation()
        {
            // Timer
            // _calculationTimer -= Time.deltaTime;
            // if (!(_calculationTimer <= 0)) return false;
            // _calculationTimer = calculationInterval;
            
            // Calculation    
            _distanceToPlayer = (transform.position - _player.transform.position).magnitude;
            return true;
        }

        private void CheckAggro()
        {
            if (_aggravated)
            {
                if (_distanceToPlayer > enemySo.aggroRange)
                {
                    _evasionTimer -= Time.deltaTime;

                    if (!(_evasionTimer <= 0)) return;
                    
                    Deaggro();
                    _evasionTimer = enemySo.evasionTime;
                }
                else
                {
                    _evasionTimer = enemySo.evasionTime;
                }
            }
            else if (_distanceToPlayer < enemySo.aggroRange)
            {
                Aggro();
            }
        }

        private void Aggro()
        {
            _aggravated = true;
            
            if (enemySo.isBoss)
            {
                _healthbarManager.EnableBossUIHealth();
                _healthbarManager.UpdateBossUIHealth(_health, _maxHealth, enemySo.bossPortrait);
            }
        }

        private void Deaggro()
        {
            _aggravated = false;
            _animator.SetBool("moving", false);
        }

        private void AggroBehavior()
        {
            if (_attackTimer > 0)
            {
                if (!_canMove) return;
                
                // Movement
                _animator.SetBool("moving", true);
                
                var angleDiff = Vector3.SignedAngle(transform.position, _player.transform.position, Vector3.back);
                relativeMoveDirection = angleDiff > 0 ? Vector3.right : Vector3.left;
                Rigidbody.AddRelativeForce(relativeMoveDirection * enemySo.moveSpeed);

                _sr.flipX = relativeMoveDirection == (enemySo.flipSprite ? Vector3.left : Vector3.right);

                _attackTimer -= Time.fixedDeltaTime;
            }
            else
            {
                _attackTimer = enemySo.attackInterval;
                Attack();
            }
        }

        private void Attack()
        {
            var idx = 0;
            if (enemySo.longAttacks.Length > 0 && _distanceToPlayer > enemySo.attackRangeThreshold)
            {
                // Long range or secondary attack
                var count = enemySo.longAttacks.Length;
                
                if (count > 0)
                {
                    idx = Random.Range(0, count);
                }

                var pattern = enemySo.longAttacks[idx];
                pattern.GetAttack().Invoke(this);
                _animator.SetInteger("attackIndex", pattern.GetIndex());
            }
            else
            {
                // Short range or primary attack
                var count = enemySo.shortAttacks.Length;
                
                if (count > 0)
                {
                    idx = Random.Range(0, count);
                }
                
                var pattern = enemySo.shortAttacks[idx];
                pattern.GetAttack().Invoke(this);
                _animator.SetInteger("attackIndex", pattern.GetIndex());
            }
            
            _animator.SetTrigger("attack");
            _animator.SetBool("moving", false);
            _canMove = false;

            StartCoroutine(DelayEnableMovement());
        }

        public void TakeDamage(float amount)
        {
            if (_health <= 0) return;
            
            _health = Mathf.Clamp(_health - amount, 0, _maxHealth);
            _healthbarManager.UpdateHealthbar(_health, _maxHealth);
            
            // Update boss health UI
            if (enemySo.isBoss)
            {
                _healthbarManager.UpdateBossUIHealth(_health, _maxHealth, enemySo.bossPortrait);
            }
            
            //TODO: damage numbers
            
            if (_health <= 0)
            {
                Death();
                return;
            }

            // Flash white
            // _sr.material.SetFloat("_FlashAmount", .75f);
            // GameUtilities.instance.DelayExecute(() => _sr.material.SetFloat("_FlashAmount", 0), 0.1f);

            _sr.sharedMaterial.shader = _flashShader;
            GameUtilities.instance.DelayExecute(() => _sr.sharedMaterial.shader = _defaultShader, 0.1f);
        }

        public void Knockback(Vector3 damageSourcePosition, float amount)
        {
            if (amount == 0) return;
            
            Rigidbody.velocity = Vector2.zero;
            var knockbackDirection = (transform.position - damageSourcePosition).normalized;
            Rigidbody.AddForce(knockbackDirection * amount, ForceMode2D.Force);
        }

        public void Death()
        {
            Destroy(gameObject);
        }

        public Vector3 GetVectorToPlayer()
        {
            return _player.transform.position - transform.position;
        }

        private IEnumerator DelayEnableMovement()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length + enemySo.attackRecoveryTime);
            _canMove = true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, enemySo.aggroRange);
        }

        protected override void OnTriggerEnter2D(Collider2D col)
        {
            base.OnTriggerEnter2D(col);

            // Damage player on contact
            if (col.TryGetComponent<PlayerController>(out var player))
            {
                player.TakeDamage(enemySo.contactDamage);
                player.Knockback(transform.position, enemySo.knockback);
            }
        }
    }
}
