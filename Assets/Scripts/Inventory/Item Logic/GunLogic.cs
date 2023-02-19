using Entities;
using UnityEngine;

namespace Inventory.Item_Logic
{
    public class GunLogic : ItemLogicBase
    {
        private Utilities _utilities;
        private GameObject _projectilePrefab;
        
        public override bool AttackOnce(GameObject equippedItemObject, Item attackItem, bool flipY)
        {
            var weaponSo = (WeaponSo)attackItem.itemSo;
            
            _utilities ??= Utilities.instance;
            _projectilePrefab ??= _utilities.GetProjectilePrefab();
            
            // Spawn projectile
            var pos = (Vector2)equippedItemObject.transform.position;
            var rot = equippedItemObject.transform.eulerAngles;
            
            var projectile = Utilities.Spawn(_projectilePrefab, pos, rot, equippedItemObject.transform);

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

            return true;
        }

        public override bool AttackContinuous(GameObject equippedItemObject, Item attackItem, bool flipY) => false;
    }
}