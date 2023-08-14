// the point of this script is to register collisions between a melee weapon and an enemy,
// and then deal damage to the enemy. This should get weapon statistics from the player controller
// when the player equips one.

using Entities;
using Entities.Entities;
using Inventory.Inventory.Item_Logic;
using Inventory.Inventory.Item_Types;
using UnityEngine;

namespace Inventory.Inventory
{
    public class MeleeHitManager : MonoBehaviour
    {
        private GameObject _equippedItemObject => gameObject;
        
        private MeleeSo _meleeSo;
        private EdgeCollider2D _edgeCollider;
        private TrailRenderer _trailRenderer;

        private void Start()
        {
            _edgeCollider = GetComponent<EdgeCollider2D>();
            _trailRenderer = GetComponent<TrailRenderer>();
            
            var itemAnimationManager = transform.parent.GetComponent<ItemAnimationManager>();
            itemAnimationManager.SwingStarted += () => ToggleSwing(true);
            itemAnimationManager.SwingCompleted += () => ToggleSwing(false);

            InventoryManager.ItemEquipped += OnItemEquipped;
        }

        private void OnItemEquipped(Item item)
        {
            if (item?.itemSo is MeleeSo meleeSo)
            {
                _meleeSo = meleeSo;
                SetWeaponStats(meleeSo.colliderPoints);
                ToggleSwing(false);
            }
            else
            {
                SetWeaponStats(null);
            }
        }

        private void SetWeaponStats(Vector2[] colliderPoints)
        {
            _edgeCollider.points = colliderPoints;
        }

        private void ToggleSwing(bool state)
        {
            _edgeCollider.enabled = state;
            _trailRenderer.enabled = state;
        }
        
        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.CompareTag("Enemy"))
            {
                var enemy = col.gameObject.GetComponent<EnemyEntity>();
                enemy.TakeDamage(_meleeSo.damage);
                enemy.Knockback(transform.position, _meleeSo.knockback);
            }
        }
    }
}
