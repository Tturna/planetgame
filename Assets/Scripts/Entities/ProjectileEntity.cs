using System;
using UnityEngine;

namespace Entities.Entities
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class ProjectileEntity : EntityController
    {
        private ProjectileData _data;
        private int _terrainLayer;
        private int _terrainBitsLayer;
        
        protected override void Start()
        {
            // base.Start();

            GetComponent<SpriteRenderer>().sprite = _data.sprite;
            _terrainLayer = LayerMask.NameToLayer("Terrain");
            _terrainBitsLayer = LayerMask.NameToLayer("TerrainBits");
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
            GetComponent<TrailRenderer>().colorGradient = _data.trailColor;
        }

        private void Update()
        {
            if (_data.faceDirectionOfTravel)
            {
                transform.right = Rigidbody.velocity.normalized;
            }
        }

        protected override void OnTriggerEnter2D(Collider2D col)
        {
            base.OnTriggerEnter2D(col);
            if (col.gameObject.layer == _terrainLayer || col.gameObject.layer == _terrainBitsLayer)
            {
                Destroy(gameObject);
            }

            if (!col.transform.root.TryGetComponent<IDamageable>(out var damageable)) return;
            
            if (_data.canHurtPlayer || damageable is not PlayerController)
            {
                damageable.TakeDamage(_data.damage);
                damageable.Knockback(transform.position, _data.knockback);

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