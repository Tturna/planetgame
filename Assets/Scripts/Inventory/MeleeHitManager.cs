// the point of this script is to register collisions between a melee weapon and an enemy,
// and then deal damage to the enemy. This should get weapon statistics from the player controller
// when the player equips one.

using Entities;
using Inventory.Item_Types;
using UnityEngine;

namespace Inventory
{
    public class MeleeHitManager : MonoBehaviour
    {
        private MeleeSo _meleeSo;
        private EdgeCollider2D _edgeCollider;

        public void SetWeaponStats(MeleeSo so, GameObject equippedItemObject)
        {
            _edgeCollider ??= equippedItemObject.GetComponent<EdgeCollider2D>();
            
            _meleeSo = so;

            if (so)
            {
                _edgeCollider.points = so.colliderPoints;
            }
        }

        public void SetCollision(bool state)
        {
            _edgeCollider.enabled = state;
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
