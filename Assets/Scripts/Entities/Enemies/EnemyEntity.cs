using System;
using System.Collections;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Entities.Entities.Enemies
{
    [RequireComponent(typeof(HealthbarManager))]
    [RequireComponent(typeof(DamageNumberManager))]
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
        private DamageNumberManager _damageNumberManager;
        private Shader _defaultShader, _flashShader;
        private MovementPattern _movementPattern;
        private Action<MovementPattern.MovementFunctionData> _movementFunction;
        private MovementPattern.MovementFunctionData _movementFunctionData;
        
        private float _calculationTimer, _evasionTimer, _attackTimer;
        private float _distanceToPlayer;
        private float _health, _maxHealth;
        private bool _aggravated, _canMove = true;

        protected override void Start()
        {
            base.Start();
            
            _player = PlayerController.instance;
            _animator = GetComponent<Animator>();
            _sr = GetComponent<SpriteRenderer>();
            _healthbarManager = GetComponent<HealthbarManager>();
            _damageNumberManager = GetComponent<DamageNumberManager>();
            
            _animator.runtimeAnimatorController = enemySo.overrideAnimator;

            _evasionTimer = enemySo.evasionTime;
            _attackTimer = enemySo.attackInterval;
            _health = enemySo.health;
            _maxHealth = enemySo.maxHealth;
            
            _movementPattern = enemySo.movementPattern;
            _movementFunction = _movementPattern.GetMovement();
            _movementPattern.Init();
            _movementFunctionData = new MovementPattern.MovementFunctionData();

            _healthbarManager.Initialize(_health, _maxHealth, enemySo.isBoss, enemySo.healthbarDistance);

            var hitboxChild = new GameObject("Hitbox");
            hitboxChild.transform.SetParent(transform);
            hitboxChild.transform.localPosition = Vector3.zero;
            hitboxChild.layer = 8;

            var hitbox = hitboxChild.AddComponent<BoxCollider2D>();
            hitbox.offset = enemySo.hitboxOffset;
            hitbox.size = enemySo.hitboxSize;
            hitbox.edgeRadius = enemySo.hitboxEdgeRadius;
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
            else
            {
                // TODO: Idle behavior
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

            if (!enemySo.isBoss) return;
            _healthbarManager.EnableBossUIHealth();
            _healthbarManager.UpdateBossUIHealth(_health, _maxHealth, enemySo.bossPortrait);
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
                _attackTimer -= Time.fixedDeltaTime;
            }
            else if (!_animator.GetBool("jumping") && Attack())
            {
                _attackTimer = enemySo.attackInterval;
            }
            
            if (!_canMove) return;
            
            // Movement
            _animator.SetBool("moving", true);
            
            // var angleDiff = Vector3.SignedAngle(transform.position, _player.transform.position, Vector3.back);
            // Debug.Log($"Signed angle: {angleDiff}");

            var posDiff = GetVectorToPlayer();
            var dot = Vector3.Dot(posDiff.normalized, transform.right);
            
            relativeMoveDirection = dot > 0 ? Vector3.right : Vector3.left;
            _sr.flipX = relativeMoveDirection == (enemySo.flipSprite ? Vector3.left : Vector3.right);
            
            _movementFunctionData.enemySo = enemySo;
            _movementFunctionData.rb = Rigidbody;
            _movementFunctionData.anim = _animator;
            _movementFunctionData.playerTr = _player.transform;
            _movementFunctionData.distanceToPlayer = _distanceToPlayer;
            _movementFunctionData.dotToPlayer = dot;
            _movementFunctionData.relativeMoveDirection = relativeMoveDirection;
            
            _movementFunction.Invoke(_movementFunctionData);
        }

        private bool Attack()
        {
            if (enemySo.attacks.Length == 0) return false;

            AttackPattern pattern = null;
            var usedIndices = new bool[enemySo.attacks.Length];
            var rng = Random.Range(0, enemySo.attacks.Length);

            foreach (var attackPattern in enemySo.attacks)
            {
                var ap = attackPattern;

                if (enemySo.useRandomAttack)
                {
                    if (usedIndices[rng])
                    {
                        rng = (rng + 1) % enemySo.attacks.Length;
                        continue;
                    }
                    ap = enemySo.attacks[rng];
                }

                if (!enemySo.alwaysAttack && ap.attackDistance < _distanceToPlayer)
                {
                    if (enemySo.useRandomAttack) usedIndices[rng] = true;
                    continue;
                }

                pattern = ap;
                break;
            }
            
            if (pattern == null) return false;
            
            pattern.GetAttack().Invoke(this, transform.right * relativeMoveDirection.x);
            _animator.SetInteger("attackIndex", pattern.GetIndex());
            _animator.SetTrigger("attack");

            if (pattern.preventsMovement)
            {
                _animator.SetBool("moving", false);
                _canMove = false;

                StartCoroutine(DelayEnableMovement());
            }
            
            return true;
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
            
            _damageNumberManager.CreateDamageNumber(amount);
            
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
