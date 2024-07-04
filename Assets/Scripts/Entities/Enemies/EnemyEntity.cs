using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Entities.Enemies
{
    [RequireComponent(typeof(HealthbarManager))]
    [RequireComponent(typeof(DamageNumberManager))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    
    // Sealed - can't be inherited
    public sealed class EnemyEntity : EntityController, IDamageable
    {
        [SerializeField] private Shader flashShader;
        
        public EnemySo enemySo;
        [HideInInspector] public Vector3 relativeMoveDirection;
        [HideInInspector] public float currentKnockback; // This exists so that attack patterns can change the contact knockback temporarily.
        
        private PlayerController _player;
        private Animator _animator;
        private SpriteRenderer _sr;
        private HealthbarManager _healthbarManager;
        private DamageNumberManager _damageNumberManager;
        private ParticleSystem _deathPs;
        
        private Shader _defaultShader;
        private MovementPattern _movementPattern;
        private Action<MovementPattern.MovementFunctionData> _movementFunction;
        private MovementPattern.MovementFunctionData _movementFunctionData;
        
        private float _calculationTimer, _evasionTimer, _attackTimer, _idleTimer, _idleActionTimer;
        private Vector2 _directionToPlayer;
        private float _distanceToPlayer;
        private float _health, _maxHealth;
        private bool _aggravated, _canMove = true;
        
        private static readonly int AnimMoving = Animator.StringToHash("moving");
        private static readonly int AnimJumping = Animator.StringToHash("jumping");
        private static readonly int AnimAttackIndex = Animator.StringToHash("attackIndex");
        private static readonly int AnimAttack = Animator.StringToHash("attack");

        public delegate void DeathHandler(EnemySo enemySo);
        public event DeathHandler OnDeath;
        
        private void TriggerOnDeath()
        {
            OnDeath?.Invoke(enemySo);
        }

        protected override void Start()
        {
            base.Start();
            
            _player = PlayerController.instance;
            _animator = GetComponent<Animator>();
            _sr = GetComponent<SpriteRenderer>();
            _healthbarManager = GetComponent<HealthbarManager>();
            _damageNumberManager = GetComponent<DamageNumberManager>();
            _deathPs = GetComponentInChildren<ParticleSystem>();
            
            _animator.runtimeAnimatorController = enemySo.overrideAnimator;

            _evasionTimer = enemySo.evasionTime;
            _attackTimer = enemySo.attackInterval;
            _health = enemySo.health;
            _maxHealth = enemySo.maxHealth;
            
            _movementPattern = enemySo.movementPattern;
            _movementFunction = _movementPattern.GetMovement();
            _movementPattern.Init();
            _movementFunctionData = new MovementPattern.MovementFunctionData();

            _healthbarManager.Initialize(_health, _maxHealth, enemySo);

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
            
            currentKnockback = enemySo.knockback;
        }

        private void Update()
        {
            if (!CalculatePlayerRelation()) return;

            CheckAggro();
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
                IdleBehavior();
            }
        }

        private bool CalculatePlayerRelation()
        {
            // Timer
            // _calculationTimer -= Time.deltaTime;
            // if (!(_calculationTimer <= 0)) return false;
            // _calculationTimer = calculationInterval;
            
            // Calculation    
            var diffToPlayer = _player.transform.position - transform.position;
            _directionToPlayer = diffToPlayer.normalized;
            _distanceToPlayer = diffToPlayer.magnitude;
            return true;
        }

        private void CheckAggro()
        {
            if (_aggravated)
            {
                if (!_player.IsAlive)
                {
                    Deaggro();
                    _evasionTimer = 0f;
                }
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
            _healthbarManager.ToggleBossUIHealth(true);
            _healthbarManager.UpdateBossUIHealth(_health, _maxHealth, enemySo);
        }

        private void Deaggro()
        {
            _aggravated = false;
            _animator.SetBool(AnimMoving, false);
        }

        private void AggroBehavior()
        {
            if (_attackTimer > 0)
            {
                _attackTimer -= Time.fixedDeltaTime;
            }
            else if (!_animator.GetBool(AnimJumping) && Attack())
            {
                _attackTimer = enemySo.attackInterval;
            }
            
            if (!_canMove) return;
            
            _animator.SetBool(AnimMoving, true);
            Move(GetVectorToPlayer());
        }

        private void IdleBehavior()
        {
            if (_idleTimer > 0)
            {
                _idleTimer -= Time.fixedDeltaTime;

                // ReSharper disable once InvertIf
                if (_idleTimer <= 0)
                {
                    _idleActionTimer = Random.Range(3f, 6f);
                    relativeMoveDirection = Random.Range(0, 2) == 0 ? Vector3.right : Vector3.left;
                }
            }
            else if (_idleActionTimer > 0)
            {
                _idleActionTimer -= Time.fixedDeltaTime;
                
                _animator.SetBool(AnimMoving, true);
                Move(relativeMoveDirection);
            }
            else
            {
                _idleTimer = Random.Range(3f, 6f);
                _animator.SetBool(AnimMoving, false);
            }
        }

        private void Move(Vector3 positionDifferenceToTarget)
        {
            var posDiff = positionDifferenceToTarget;
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

                if (!enemySo.alwaysAttack && ap.attackRange < _distanceToPlayer)
                {
                    if (enemySo.useRandomAttack) usedIndices[rng] = true;
                    continue;
                }

                pattern = ap;
                break;
            }
            
            if (pattern == null) return false;
            
            pattern.GetAttack().Invoke(this, _directionToPlayer);
            _animator.SetInteger(AnimAttackIndex, pattern.GetIndex());
            _animator.SetTrigger(AnimAttack);

            if (!pattern.preventsMovement) return true;
            _animator.SetBool(AnimMoving, false);
            _canMove = false;

            StartCoroutine(DelayEnableMovement());
            return true;
        }

        public void TakeDamage(float amount)
        {
            if (_health <= 0) return;

            amount = Mathf.Round(Random.Range(amount * 0.8f, amount * 1.2f));

            _health = Mathf.Clamp(_health - amount, 0, _maxHealth);
            _healthbarManager.UpdateHealthbar(_health, _maxHealth);
            
            if (enemySo.isBoss)
            {
                _healthbarManager.UpdateBossUIHealth(_health, _maxHealth, enemySo);
            }
            
            _damageNumberManager.CreateDamageNumber(amount);
            
            if (_health <= 0)
            {
                Death();
                return;
            }

            _sr.sharedMaterial.shader = flashShader;
            GameUtilities.instance.DelayExecute(() =>
            {
                if (_sr == null) return;
                _sr.sharedMaterial.shader = _defaultShader;
            }, 0.1f);
        }

        public void Knockback(Vector3 damageSourcePosition, float amount)
        {
            if (enemySo.isImmuneToKnockback) return;
            if (amount == 0) return;
            
            Rigidbody.velocity = Vector2.zero;
            
            // check if the damage source is on the left or the right in relation to the enemy
            var tr = transform;
            var dot = Vector3.Dot((damageSourcePosition - tr.position).normalized, tr.right);
            var knockbackDirection = dot > 0 ? -transform.right : transform.right;
            knockbackDirection = (knockbackDirection + transform.up * 0.6f).normalized;
            
            Rigidbody.AddForce(knockbackDirection * amount, ForceMode2D.Force);
            _canMove = false;
            GameUtilities.instance.DelayExecute(() => _canMove = true, 0.3f);
        }

        public void Death()
        {
            _deathPs.transform.SetParent(null);
            _deathPs.Play();
            TriggerOnDeath();
            GameUtilities.instance.DelayExecute(() => Destroy(_deathPs.gameObject), 1f);

            if (enemySo.isBoss)
            {
                _healthbarManager.ToggleBossUIHealth(false);
            }
            
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

        private void OnTriggerEnter2D(Collider2D col)
        {
            // base.OnTriggerEnter2D(col);

            // Damage player on contact
            if (col.TryGetComponent<PlayerController>(out var player))
            {
                player.TakeDamage(enemySo.contactDamage);
                player.Knockback(transform.position + (Vector3)enemySo.knockbackSourcePointOffset, currentKnockback);
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, enemySo.aggroRange);
        }
        
        [MenuItem("CONTEXT/EnemyEntity/InitializeForEditor")]
        static void InitializeForEditor(MenuCommand command)
        {
            var enemyEntity = (EnemyEntity)command.context;
            enemyEntity.gameObject.name = "(Enemy) " + enemyEntity.enemySo.enemyName;
            enemyEntity.GetComponent<Animator>().runtimeAnimatorController = enemyEntity.enemySo.overrideAnimator;
        }
    }
}
