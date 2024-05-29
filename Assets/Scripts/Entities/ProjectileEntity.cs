using UnityEngine;
using Utilities;

namespace Entities
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ProjectileEntity : EntityController
    {
        private ParticleSystem _breakPs;
        private ProjectileData _data;
        private Vector3 _lastPos;
        
        protected override void Start()
        {
            // base.Start();

            _breakPs = GetComponentInChildren<ParticleSystem>();
            GetComponent<SpriteRenderer>().sprite = _data.sprite;
            
            _lastPos = transform.position;
        }

        private void Update()
        {
            // Raycast to last position to prevent projectiles from going through stuff
            var mask = GameUtilities.BasicMovementCollisionMask;
            var distance = Vector3.Distance(_lastPos, transform.position);
            var direction = (transform.position - _lastPos).normalized;
            var hit = Physics2D.Raycast(_lastPos, direction, distance, mask);
            
            if (hit)
            {
                transform.position = hit.point;
                ProjectileHit(hit.collider);
                return;
            }
            
            _lastPos = transform.position;
            
            if (_data.faceDirectionOfTravel)
            {
                transform.right = Rigidbody.velocity.normalized;
            }
        }

        public void Init(ProjectileData projectileData)
        {
            _data = projectileData;
            Rigidbody = GetComponent<Rigidbody2D>();
            
            ToggleControl(false);
            ToggleAutoRotation(false);
            
            if (!_data.useGravity)
            {
                TogglePhysics(false);
            }
            
            gravityMultiplier = _data.gravityMultiplier;
            
            Rigidbody.AddForce(transform.right * _data.projectileSpeed, ForceMode2D.Impulse);
            var trailRenderer = GetComponent<TrailRenderer>();
            trailRenderer.colorGradient = _data.trailColor;
            trailRenderer.time = _data.trailTime;
        }

        private void DestroyProjectile(Vector3 hitObjectPos)
        {
            var colDiff = (hitObjectPos - transform.position).normalized; 
            var angle = Mathf.Atan2(colDiff.y, colDiff.x) * Mathf.Rad2Deg;
            
            // TODO: object pooling
            var psTr = _breakPs.transform;
            psTr.SetParent(null);
            psTr.localScale = Vector3.one;
            psTr.eulerAngles = Vector3.forward * angle;
            
            var main = _breakPs.main;
            main.startColor = _data.breakParticleColor;
            _breakPs.Play();
            
            GameUtilities.instance.DelayExecute(() => Destroy(_breakPs.gameObject), 1f);
            Destroy(gameObject);
        }

        private void ProjectileHit(Collider2D col)
        {
            if ((col.gameObject.layer & GameUtilities.BasicMovementCollisionMask) != 0)
            {
                DestroyProjectile(col.transform.position);
            }

            if (!col.transform.root.TryGetComponent<IDamageable>(out var damageable)) return;
            if (!_data.canHurtPlayer && damageable is PlayerController) return;
            
            // TODO: Implement entity defense and defense penetration
            var damage = PlayerStatsManager.CalculateRangedDamage(_data.damage, _data.critChance);
            var trueKnockback = _data.knockback * PlayerStatsManager.KnockbackMultiplier;
            
            damageable.TakeDamage(damage);
            // Here we use last position because the projectile might have moved past the collider
            damageable.Knockback(_lastPos, trueKnockback);

            if (!_data.piercing)
            {
                DestroyProjectile(col.transform.position);
            }
        }

        protected void OnTriggerEnter2D(Collider2D col)
        {
            // base.OnTriggerEnter2D(col);
            ProjectileHit(col);
        }

        protected void OnTriggerExit2D(Collider2D other)
        {
            // base.OnTriggerExit2D(other);
        }
    }
}