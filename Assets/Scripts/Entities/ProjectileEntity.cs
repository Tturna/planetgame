#nullable enable
using Entities.Enemies;
using Planets;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using Utilities;

namespace Entities
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ProjectileEntity : EntityController
    {
        [FormerlySerializedAs("_light")] [SerializeField] private Light2D light2D; 
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField] private GameObject breakEffectsParent;
        private ParticleSystem _defaultBreakPfx;
        private ProjectileData _data;
        private Vector3 _lastPos;
        private SpriteRenderer _sr;
        private TrailRenderer _trailRenderer;
        private float _lifetimeTimer;
        private Vector2 _initialVelocity;
        private Animator _animator;
        private static readonly int AnimTransitionToUpdateBool = Animator.StringToHash("transitionToUpdate");
        private bool animatorCreated;
        private AudioSource _breakAudioSource;

        protected override void Start()
        {
            _lastPos = transform.position;
            _defaultBreakPfx = GetComponentInChildren<ParticleSystem>();

            if (_data.lifetime == 0)
            {
                Debug.LogWarning($"Projectile with infinite lifetime detected ({gameObject.name}). This can cause performance issues.");
            }
        }

        private void Update()
        {
            if (_data.lifetime > 0)
            {
                _lifetimeTimer += Time.deltaTime;
                
                if (_lifetimeTimer >= _data.lifetime)
                {
                    DisableProjectile(transform.position);
                    return;
                }
            }
            
            // Raycast to last position to prevent projectiles from going through stuff
            var mask = LayerMask.GetMask("Default", "Enemy") & GameUtilities.BasicMovementCollisionMask;
            var position = transform.position;
            var distance = Vector3.Distance(_lastPos, position);
            var direction = (position - _lastPos).normalized;
            var hit = Physics2D.Raycast(_lastPos, direction, distance, mask);
            
            if (hit)
            {
                ProjectileHit(hit.collider, hit.point);
                return;
            }
            
            _lastPos = transform.position;
            
            if (_data.faceDirectionOfTravel)
            {
                transform.right = Rigidbody.velocity.normalized;
            }

            if (!_data.useGravity)
            {
                Rigidbody.velocity = _initialVelocity;
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        public void Init(ProjectileData projectileData, PlanetGenerator closestPlanetGen = null)
        {
            _lifetimeTimer = 0;
            _lastPos = transform.position;
            _data = projectileData;

            if (!Rigidbody)
            {
                Rigidbody = GetComponent<Rigidbody2D>();
            }

            if (!_sr)
            {
                _sr = GetComponent<SpriteRenderer>();
            }

            if (!_trailRenderer)
            {
                _trailRenderer = GetComponent<TrailRenderer>();
            }
            
            _sr.sprite = _data.sprite;

            if (string.IsNullOrEmpty(_data.sortingLayerName))
            {
                _sr.sortingLayerName = "Default";
                _sr.sortingOrder = 1;
                _trailRenderer.sortingLayerName = "Effects";
                _trailRenderer.sortingOrder = 0;
            }
            else
            {
                _sr.sortingLayerName = _data.sortingLayerName;
                _sr.sortingOrder = _data.sortingOrder;
                _trailRenderer.sortingLayerName = _data.sortingLayerName;
                _trailRenderer.sortingOrder = _data.sortingOrder - 1;
            }

            if (!MainCollider)
            {
                MainCollider = GetComponent<Collider2D>();
            }
            
            // make collider size match sprite size
            var col = (CapsuleCollider2D)MainCollider;
            col.size = _sr.sprite.bounds.size;
            
            ToggleControl(false);
            ToggleAutoRotation(false);

            if (_data.collisionActivationDelay > 0)
            {
                ToggleCollision(false);
                GameUtilities.instance.DelayExecute(() => ToggleCollision(true), _data.collisionActivationDelay);
            }
            
            TogglePhysics(_data.useGravity);
            gravityMultiplier = _data.gravityMultiplier;
            
            if (_data.useGravity)
            {
                if (closestPlanetGen)
                {
                    SetCurrentPlanet(closestPlanetGen);
                }
                else
                {
                    closestPlanetCheckTimer = ClosestPlanetCheckInterval;
                }
            }
            
            var speed = Random.Range(_data.minProjectileSpeed, _data.maxProjectileSpeed);
            Rigidbody.AddForce(transform.right * speed, ForceMode2D.Impulse);
            _initialVelocity = Rigidbody.velocity;
            _trailRenderer.Clear();
            _trailRenderer.colorGradient = _data.trailColor;
            _trailRenderer.time = _data.trailTime;
            _trailRenderer.startWidth = _data.trailStartEndWidth.x;
            _trailRenderer.endWidth = _data.trailStartEndWidth.y;

            light2D.gameObject.SetActive(_data.useLight);
            
            if (_data.useLight)
            {
                light2D.color = _data.lightColor;
            }

            if (_data.spawnAnimation || _data.updateAnimation)
            {
                // Apparently using this[string] for AnimatorOverrideController is not recommended
                // if you need to do it multiple times. We only do it twice and not every frame so it's fine.
                // Allegedly it's better to use ApplyOverrides():
                // https://docs.unity3d.com/ScriptReference/AnimatorOverrideController.ApplyOverrides.html

                if (!animatorCreated)
                {
                    _animator = gameObject.AddComponent<Animator>();
                    animatorCreated = true;
                }
                
                _animator.enabled = true;
                
                var orAnim = new AnimatorOverrideController(animatorController);
                _animator.runtimeAnimatorController = orAnim;
                
                if (_data.updateAnimation)
                {
                    orAnim["d_move"] = _data.updateAnimation;

                    if (_data.spawnAnimation)
                    {
                        _animator.SetBool(AnimTransitionToUpdateBool, true);
                    }
                    else
                    {
                        _animator.Play("Base Layer.Update");
                    }
                }
                
                if (_data.spawnAnimation)
                {
                    orAnim["d_wakeup"] = _data.spawnAnimation;
                    _animator.Play("Base Layer.Spawn");
                }
            }

            if (_data.breakSound)
            {
                _breakAudioSource = breakEffectsParent.GetComponent<AudioSource>();
                _breakAudioSource.clip = _data.breakSound;
            }
        }

        private void DisableProjectile(Vector3 hitObjectPos)
        {
            var hitObjectDirection = (hitObjectPos - transform.position).normalized; 
            
            var hit = Physics2D.Raycast(transform.position, hitObjectDirection, 1f, GameUtilities.BasicMovementCollisionMask);

            GameObject? clone = null;
            ParticleSystem breakPfx;
            
            if (_data.breakParticlePrefab)
            {
                // TODO: object pooling
                clone = Instantiate(_data.breakParticlePrefab, transform.position, Quaternion.identity, transform);
                clone.transform.SetParent(breakEffectsParent.transform);
                breakPfx = clone.GetComponent<ParticleSystem>();
            }
            else
            {
                breakPfx = _defaultBreakPfx;
            }
            
            breakEffectsParent.transform.SetParent(null);
            breakEffectsParent.transform.localScale = Vector3.one;
            var pfxTr = breakPfx.transform;

            if (hit)
            {
                pfxTr.transform.up = hit.normal;
                pfxTr.position = hit.point;
            }
            else
            {
                pfxTr.position = transform.position;
            }
            
            var main = breakPfx.main;
            main.startColor = _data.breakParticleColor;
            breakPfx.gameObject.SetActive(true);
            breakPfx.Play();
            
            if (_breakAudioSource && _breakAudioSource.clip)
            {
                var distToPlayer = Vector3.Distance(PlayerController.instance.transform.position, transform.position);
                _breakAudioSource.volume = Mathf.Clamp01(1f - distToPlayer / 15f);
                _breakAudioSource.Play();
            }
            
            GameUtilities.instance.DelayExecute(() =>
            {
                breakEffectsParent.transform.SetParent(transform);
                
                if (_data.breakParticlePrefab)
                {
                    if (clone != null)
                    {
                        Destroy(clone);
                    }
                }
                else
                {
                    breakPfx.Stop();
                    
                    // I guess Unity disables a gameobject added to a disabled gameobject
                    breakPfx.gameObject.SetActive(true);
                }
            }, 1f);
            
            if (animatorCreated)
            {
                _animator.StopPlayback();
                _animator.Rebind();
                _animator.Update(0f);
                _animator.enabled = false;
            }
            
            gameObject.SetActive(false);
        }

        private void ProjectileHit(Collider2D col, Vector3? hitPoint = null)
        {
            if (!gameObject.activeInHierarchy) return;

            if (((1 << col.gameObject.layer) & GameUtilities.BasicMovementCollisionMask) != 0)
            {
                if (_data.collideWithWorld)
                {
                    DisableProjectile(col.transform.position);
                }

                return;
            }

            if (hitPoint != null)
            {
                transform.position = (Vector3)hitPoint;
            }
            
            if (!_data.canHurtPlayer && !_data.canHurtEnemies) return;
            if (!col.transform.TryGetComponent<IDamageable>(out var damageable)) return;
            if (!_data.canHurtEnemies && damageable is EnemyEntity) return;
            if (!_data.canHurtPlayer && damageable is PlayerController) return;
            
            // TODO: Implement entity defense and defense penetration
            var damage = PlayerStatsManager.CalculateRangedDamage(_data.damage, _data.critChance);
            var trueKnockback = _data.knockback * PlayerStatsManager.KnockbackMultiplier;
            
            damageable.TakeDamage(damage, _lastPos);
            // Here we use last position because the projectile might have moved past the collider
            damageable.Knockback(_lastPos, trueKnockback);

            if (!_data.piercing)
            {
                DisableProjectile(col.transform.position);
            }
        }

        protected void OnTriggerEnter2D(Collider2D col)
        {
            ProjectileHit(col);
        }
    }
}