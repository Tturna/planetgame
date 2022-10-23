using Entities;
using UnityEngine;

namespace Inventory.Item_Logic
{
    public class GunLogic : ItemLogicBase
    {
        private Utilities _utilities;
        private GameObject _projectilePrefab;
        
        public override void Attack(GameObject equippedItemObject, Item attackItem, bool flipY)
        {
            _utilities ??= Utilities.Instance;
            _projectilePrefab ??= _utilities.GetProjectilePrefab();
            
            // Spawn projectile
            var weaponSo = (WeaponSo)attackItem.itemSo;
            var pos = (Vector2)equippedItemObject.transform.position;
            var rot = equippedItemObject.transform.eulerAngles;
            
            var projectile = _utilities.Spawn(_projectilePrefab, pos, rot, equippedItemObject.transform);

            // Set projectile position to muzzle position
            var localPos = weaponSo.muzzlePosition;
            localPos.y = flipY ? -localPos.y : localPos.y;
            projectile.transform.localPosition = localPos;
            projectile.transform.SetParent(null);
            
            // Initialize projectile
            var entity = projectile.GetComponent<ProjectileEntity>();
            entity.Init(weaponSo.projectile);
            
            //TODO: Object pooling
            
            //Debug.Log($"Shoot {weaponSo.name} with {weaponSo.projectile?.sprite?.name ?? "null"} @ {Time.time}");
        }
    }
}