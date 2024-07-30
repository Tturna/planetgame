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
        private ParticleSystem _breakPs;
        private ProjectileData _data;
        private Vector3 _lastPos;
        private SpriteRenderer _sr;
        private TrailRenderer _trailRenderer;
        private float _lifetimeTimer;
        private Vector2 _initialVelocity;
        private Animator _animator;
        private static readonly int AnimTransitionToUpdateBool = Animator.StringToHash("transitionToUpdate");
        private bool animatorCreated;

        protected override void Start()
        {
            _breakPs = GetComponentInChildren<ParticleSystem>();
            _lastPos = transform.position;

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

            if (!string.IsNullOrEmpty(_data.sortingLayerName))
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
        }

        private void DisableProjectile(Vector3 hitObjectPos)
        {
            var colDiff = (hitObjectPos - transform.position).normalized; 
            var angle = Mathf.Atan2(colDiff.y, colDiff.x) * Mathf.Rad2Deg;
            
            // TODO: Make it so the break particles are pulled from projectile data
            var psTr = _breakPs.transform;
            psTr.SetParent(null);
            psTr.localScale = Vector3.one;
            psTr.eulerAngles = Vector3.forward * angle;
            
            var main = _breakPs.main;
            main.startColor = _data.breakParticleColor;
            _breakPs.Play();
            
            GameUtilities.instance.DelayExecute(() =>
            {
                _breakPs.Stop();
                psTr.SetParent(transform);
            }, 1f);
            
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
            
            damageable.TakeDamage(damage);
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