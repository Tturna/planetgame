using Inventory;
using UnityEngine;

namespace Entities
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ProjectileEntity : EntityController
    {
        private ProjectileData _data;
        
        protected override void Start()
        {
            // base.Start();

            GetComponent<SpriteRenderer>().sprite = _data.sprite;
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
            
            Rigidbody.AddForce(transform.right * _data.projectileSpeed, ForceMode2D.Impulse);
        }

        protected override void OnTriggerEnter2D(Collider2D col)
        {
            // base.OnTriggerEnter2D(col);

            if (!col.transform.root.TryGetComponent<IDamageable>(out var damageable)) return;
            
            if (_data.canHurtPlayer || damageable is not PlayerController)
            {
                damageable.TakeDamage(_data.damage);

                if (!_data.piercing)
                {
                    Destroy(gameObject);
                }
            }
        }

        protected override void OnTriggerExit2D(Collider2D other)
        {
            // base.OnTriggerExit2D(other);
        }
    }
}