// the point of this script is to register collisions between a melee weapon and an enemy,
// and then deal damage to the enemy. This should get weapon statistics from the player controller
// when the player equips one.

using System;
using Entities;
using Entities.Enemies;
using Inventory.Item_Logic;
using Inventory.Item_SOs;
using UnityEngine;

namespace Inventory
{
    public class MeleeHitManager : MonoBehaviour
    {
        [SerializeField] private TrailRenderer trailRenderer;
        private GameObject _equippedItemObject => gameObject;
        
        private MeleeSo _meleeSo;
        private EdgeCollider2D _edgeCollider;

        private void Start()
        {
            _edgeCollider = GetComponent<EdgeCollider2D>();
            
            var itemAnimationManager = transform.parent.GetComponent<ItemAnimationManager>();
            itemAnimationManager.SwingStarted += (trailState) => ToggleSwing(true, trailState);
            itemAnimationManager.SwingCompleted += () => ToggleSwing(false, false);

            InventoryManager.ItemEquipped += OnItemEquipped;
        }

        private void OnDestroy()
        {
            InventoryManager.ItemEquipped -= OnItemEquipped;
        }

        private void OnItemEquipped(Item item)
        {
            if (item?.itemSo is MeleeSo meleeSo)
            {
                _meleeSo = meleeSo;
                SetWeaponStats(_meleeSo);
                ToggleSwing(false, false);
            }
            else
            {
                SetWeaponStats(null);
            }
        }

        private void SetWeaponStats(MeleeSo meleeSo)
        {
            if (meleeSo == null)
            {
                _edgeCollider.points = null;
                return;
            }
            
            _edgeCollider.points = meleeSo.colliderPoints;
            trailRenderer.transform.localPosition = meleeSo.swingTrailOffset;
            trailRenderer.startWidth = meleeSo.swingTrailWidth;
        }

        private void ToggleSwing(bool state, bool trailState)
        {
            _edgeCollider.enabled = state;
            trailRenderer.enabled = trailState;
        }
        
        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.CompareTag("Enemy"))
            {
                var enemy = col.gameObject.GetComponent<EnemyEntity>();
                
                // TODO: Implement entity defense and defense penetration
                var damage = PlayerStatsManager.CalculateMeleeDamage(_meleeSo.damage, _meleeSo.critChance);
                var knockback = _meleeSo.knockback * PlayerStatsManager.KnockbackMultiplier;
                
                enemy.TakeDamage(damage);
                enemy.Knockback(transform.position, knockback);
            }
        }
    }
}
