using System;
using System.Collections;
using Cameras;
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
        [SerializeField] private GameObject deathEffectsParent;
        [SerializeField] private ParticleSystem deathPfx;
        [SerializeField] private AudioSource deathAudioSource;
        [SerializeField] private AudioSource hitAudioSource;
        [SerializeField] private ParticleSystem hitPfx;
        
        public EnemySo enemySo;
        [HideInInspector] public Vector3 relativeMoveDirection;
        [HideInInspector] public float currentKnockback; // This exists so that attack patterns can change the contact knockback temporarily.
        
        private PlayerController _player;
        private Animator _animator;
        private SpriteRenderer _sr;
        private HealthbarManager _healthbarManager;
        private DamageNumberManager _damageNumberManager;
        
        private Shader _defaultShader;
        private MovementPattern _movementPattern;
        private Action<MovementPattern.MovementFunctionData> _movementFunction;
        private MovementPattern.MovementFunctionData _movementFunctionData;
        private Action _deathAttackCancelAction;
        
        private float _calculationTimer, _evasionTimer, _attackTimer, _idleTimer, _idleActionTimer, _wakeupTimer, _despawnTimer;
        private Vector2 _directionToPlayer;
        private float _distanceToPlayer;
        private float _health, _maxHealth;
        private bool _aggravated, _canMove = true, _initialized;
        private float _attackRecoveryTime;

        private static readonly int AnimWakeup = Animator.StringToHash("wakeup");
        private static readonly int AnimMoving = Animator.StringToHash("moving");
        private static readonly int AnimJumping = Animator.StringToHash("jumping");
        private static readonly int AnimAttackIndex = Animator.StringToHash("attackIndex");
        private static readonly int AnimAttack = Animator.StringToHash("attack");
        private static readonly int AnimDeath = Animator.StringToHash("death");
        
        // hacky shit to implement enrage for swamp titan. to be reworked.
        private bool _swampTitanEnraged;
        private bool _swampTitanDoubleAttack;
        private bool _swampTitanDoubleAttackDone;

        public delegate void DeathHandler(EnemySo enemySo);
        public event DeathHandler OnDeath;
        
        private void TriggerOnDeath()
        {
            OnDeath?.Invoke(enemySo);
        }

        protected override void Start()
        {
            base.Start();

            Init(enemySo);
        }

        private void Update()
        {
            if (_wakeupTimer < enemySo.wakeupDelay)
            {
                _wakeupTimer += Time.deltaTime;
                return;
            }
            
            if (_health <= 0) return;
            if (!CalculatePlayerRelation()) return;
            
            // to be reworked.
            if (enemySo.isBoss && !_swampTitanEnraged && _health <= _maxHealth * 0.25f)
            {
                _swampTitanEnraged = true;
                _swampTitanDoubleAttack = true;
                _attackTimer = enemySo.attackInterval;
                _canMove = false;
                _attackRecoveryTime = 0f;
                _animator.SetTrigger("enrage");
                CameraController.CameraShake(1f, 0.1f);
                _healthbarManager.SetBossEnraged(true);
                
                GameUtilities.instance.DelayExecute(() =>
                {
                    var delay = _animator.GetCurrentAnimatorStateInfo(0).length + _attackRecoveryTime;
                    StartCoroutine(DelayEnableMovement(delay));
                }, 1f);
            }

            CheckAggro();
            CheckDespawn();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            if (_wakeupTimer < enemySo.wakeupDelay) return;
            if (_health <= 0) return;
            
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
            // _calculationTimer -= Time.deltaTime;
            // if (!(_calculationTimer <= 0)) return false;
            // _calculationTimer = calculationInterval;
            
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

        private void CheckDespawn()
        {
            if (_health <= 0) return;
            if (enemySo.despawnTime <= 0) return;
            
            // Enemy is considered visible if it's observed in the scene view in Unity.
            if (!_sr.isVisible)
            {
                _despawnTimer += Time.deltaTime;

                if (_health > 0f && _despawnTimer >= enemySo.despawnTime)
                {
                    _health = 0;
                    Death(true);
                }
            }
            else
            {
                _despawnTimer = 0f;
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
                
                // to be reworked.
                if (_swampTitanEnraged && !_swampTitanDoubleAttackDone && _attackTimer <= 0)
                {
                    var rng = Random.Range(0, 2);

                    if (rng == 0)
                    {
                        _swampTitanDoubleAttack = true;
                    }
                }
            }
            else if (!_animator.GetBool(AnimJumping) && Attack())
            {
                // to be reworked.
                if (_swampTitanEnraged && _swampTitanDoubleAttack)
                {
                    _swampTitanDoubleAttack = false;
                    _swampTitanDoubleAttackDone = true;
                    _attackTimer = enemySo.attackInterval * 0.5f;
                    return;
                }

                if (_swampTitanEnraged)
                {
                    _swampTitanDoubleAttackDone = false;
                }

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
            
            _deathAttackCancelAction = pattern.GetAttack().Invoke(this, _directionToPlayer);
            _animator.SetInteger(AnimAttackIndex, pattern.GetIndex());
            _animator.SetTrigger(AnimAttack);

            if (!pattern.preventsMovement) return true;
            _animator.SetBool(AnimMoving, false);
            _canMove = false;

            var delay = _animator.GetCurrentAnimatorStateInfo(0).length + _attackRecoveryTime;
            StartCoroutine(DelayEnableMovement(delay));
            return true;
        }

        public void Init(EnemySo sourceSo)
        {
            if (_initialized) return;
            if (!sourceSo) return;

            enemySo = sourceSo;
            _initialized = true;
            
            _player = PlayerController.instance;
            _animator = GetComponent<Animator>();
            _sr = GetComponent<SpriteRenderer>();
            _healthbarManager = GetComponent<HealthbarManager>();
            _damageNumberManager = GetComponent<DamageNumberManager>();
            
            _animator.runtimeAnimatorController = enemySo.overrideAnimator;

            if (enemySo.wakeupDelay > 0)
            {
                _animator.SetTrigger(AnimWakeup);
            }

            _evasionTimer = enemySo.evasionTime;
            _attackTimer = enemySo.attackInterval;
            _health = enemySo.health;
            _maxHealth = enemySo.maxHealth;
            
            _movementPattern = enemySo.movementPattern;
            _movementFunction = _movementPattern.GetMovement();
            _movementPattern.Init();
            _movementFunctionData = new MovementPattern.MovementFunctionData();
            ToggleAutoRotation(!enemySo.faceMovementDirection);

            _healthbarManager.Initialize(_health, _maxHealth, enemySo);

            if (!MainCollider)
            {
                MainCollider = GetComponent<Collider2D>();
            }
            
            var mainCol = (BoxCollider2D)MainCollider;
            mainCol.offset = enemySo.hitboxOffset;
            mainCol.size = enemySo.hitboxSize;
            mainCol.edgeRadius = enemySo.hitboxEdgeRadius;

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
            _attackRecoveryTime = enemySo.attackRecoveryTime;
        }

        public void TakeDamage(float amount, Vector3 damageSourcePosition)
        {
            if (_health <= 0) return;
            if (_wakeupTimer < enemySo.wakeupDelay) return;
            
            CameraController.CameraShake(0.075f, 0.05f);
            hitAudioSource.PlayOneShot(enemySo.hitSound);
            
            var directionToSource = (damageSourcePosition - transform.position).normalized;
            var localHitPfxDirection = transform.InverseTransformDirection(-directionToSource);
            var angle = Mathf.Atan2(localHitPfxDirection.y, localHitPfxDirection.x) * Mathf.Rad2Deg;
            var shapeModule = hitPfx.shape;

            // If the hit is coming from above the enemy, rotate the hit effect upwards so less
            // particles hit the ground. The higher the hit, the less rotation offset.
            var relativeUpDot = Vector3.Dot(directionToSource, transform.up);
            
            if (relativeUpDot > 0f)
            {
                var offset = shapeModule.arc * (1f - relativeUpDot);
                
                if (localHitPfxDirection.x < 0)
                {
                    offset *= -1;
                }
                
                angle += offset;
            }
            
            shapeModule.rotation = new Vector3(0, 0, angle - shapeModule.arc / 2f);
            
            var mainModule = hitPfx.main;
            mainModule.startColor = enemySo.hitPfxColor;
            
            hitPfx.Play();
            
            amount = Mathf.Round(Random.Range(amount * 0.8f, amount * 1.2f));
            _health = Mathf.Clamp(_health - amount, 0, _maxHealth);
            _damageNumberManager.CreateDamageNumber(amount);
            
            if (enemySo.isBoss)
            {
                _healthbarManager.UpdateBossUIHealth(_health, _maxHealth, enemySo);
            }
            
            if (_health <= 0)
            {
                Death();
                return;
            }
            
            _healthbarManager.UpdateHealthbar(_health, _maxHealth);
            
            StartCoroutine(DamageStretch());

            _sr.sharedMaterial.shader = flashShader;
            GameUtilities.instance.DelayExecute(() =>
            {
                if (_sr == null) return;
                _sr.sharedMaterial.shader = _defaultShader;
            }, 0.1f);
        }
        
        private IEnumerator DamageStretch()
        {
            if (enemySo.hitSquishStretchMultiplier == Vector2.zero) yield break;
            
            const float time = 0.2f;
            var timer = time;

            while (timer > 0)
            {
                var n = timer / time;
                
                var squishMult = enemySo.hitSquishStretchMultiplier.x;
                var stretchMult = enemySo.hitSquishStretchMultiplier.y;

                float squish, stretch;

                if (squishMult < 1f)
                {
                    squish = Mathf.Lerp(0.65f, 1f, 1f - squishMult);
                }
                else
                {
                    squish = 0.65f * squishMult;
                }
                
                if (stretchMult < 1f)
                {
                    stretch = Mathf.Lerp(1.35f, 1f, 1f - stretchMult);
                }
                else
                {
                    stretch = 1.35f * stretchMult;
                }
                
                var bodyScale = transform.localScale;
                var squishLerp = Mathf.Lerp(1f, squish, n);
                var stretchLerp = Mathf.Lerp(1f, stretch, n);
                
                bodyScale.x = stretchLerp;
                bodyScale.y = squishLerp;

                transform.localScale = bodyScale;
                
                timer -= Time.deltaTime;
                yield return null;
            }
        }

        public void Knockback(Vector3 damageSourcePosition, float amount)
        {
            if (_wakeupTimer < enemySo.wakeupDelay) return;
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

        public void Death(bool despawn = false)
        {
            _deathAttackCancelAction?.Invoke();

            if (!despawn)
            {
                deathEffectsParent.transform.SetParent(null);
                deathPfx.Play();
            }

            if (!despawn)
            {
                if (enemySo.deathSound)
                {
                    var rngPitch = Random.Range(-3, 1);
                    deathAudioSource.pitch = Mathf.Pow(2f, rngPitch / 12f);
                    deathAudioSource.PlayOneShot(enemySo.deathSound);
                }
            }
            
            TriggerOnDeath();

            if (!despawn)
            {
                GameUtilities.instance.DelayExecute(() => Destroy(deathEffectsParent), 1f);
            }

            if (enemySo.isBoss)
            {
                _healthbarManager.SetBossEnraged(false);
                _healthbarManager.ToggleBossUIHealth(false);

                if (!despawn)
                {
                    CameraController.CameraShake(1f, 0.1f);
                }
            }

            transform.localScale = Vector3.one;

            if (!despawn)
            {
                _sr.flipX = enemySo.flipSprite;
                _animator.SetTrigger(AnimDeath);
            }

            if (!despawn && enemySo.deathDelay > 0)
            {
                GameUtilities.instance.DelayExecute(() =>
                {
                    Destroy(gameObject);
                }, enemySo.deathDelay);
            }
            else
            {
                Destroy(gameObject);
            }

            if (despawn)
            {
                Debug.Log("Enemy despawned.");
            }
        }

        public Vector3 GetVectorToPlayer()
        {
            return _player.transform.position - transform.position;
        }

        private IEnumerator DelayEnableMovement(float delay)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(delay);
            _canMove = true;
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            // base.OnTriggerEnter2D(col);

            // Damage player on contact
            if (col.TryGetComponent<PlayerController>(out var player))
            {
                var damageSourcePoint = transform.position + (Vector3)enemySo.knockbackSourcePointOffset;
                player.TakeDamage(enemySo.contactDamage, damageSourcePoint);
                player.Knockback(damageSourcePoint, currentKnockback);
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, enemySo.aggroRange);
        }

#if UNITY_EDITOR
        [MenuItem("CONTEXT/EnemyEntity/InitializeForEditor")]
        static void InitializeForEditor(MenuCommand command)
        {
            var enemyEntity = (EnemyEntity)command.context;
            enemyEntity.gameObject.name = "(Enemy) " + enemyEntity.enemySo.enemyName;
            enemyEntity.GetComponent<Animator>().runtimeAnimatorController = enemyEntity.enemySo.overrideAnimator;
        }
#endif
    }
}
