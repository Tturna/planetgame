using Inventory.Inventory.Item_Types;
using UnityEngine;
using Utilities;

namespace Inventory.Inventory.Item_Logic
{
    public class GunLogic : ItemLogicBase
    {
        private GameUtilities _utilities;
        private GameObject _projectilePrefab;
        
        public override bool UseOnce(UseParameters useParameters)
        {
            var weaponSo = (WeaponSo)useParameters.attackItem.itemSo;
            
            _utilities ??= GameUtilities.instance;
            _projectilePrefab ??= _utilities.GetProjectilePrefab();
            
            // Spawn projectile
            var pos = (Vector2)useParameters.equippedItemObject.transform.position;
            var rot = useParameters.equippedItemObject.transform.eulerAngles;
            
            var projectile = GameUtilities.Spawn(_projectilePrefab, pos, rot, useParameters.equippedItemObject.transform);

            // Set projectile position to muzzle position
            var localPos = weaponSo.muzzlePosition;
            localPos.y = useParameters.flipY ? -localPos.y : localPos.y;
            projectile.transform.localPosition = localPos;
            projectile.transform.SetParent(null);
            
            // Initialize projectile
            var entity = projectile.GetComponent<ProjectileEntity>();
            entity.Init(weaponSo.projectile);
            
            //TODO: Object pooling
            
            // Choose random muzzle flash
            if (weaponSo.muzzleFlashes.Length > 0)
            {
                var muzzleFlashObject = useParameters.equippedItemObject.transform.GetChild(0).gameObject;
                muzzleFlashObject.transform.localPosition = weaponSo.muzzlePosition;
                
                var flashSr = muzzleFlashObject.GetComponent<SpriteRenderer>();
                flashSr.sprite = weaponSo.muzzleFlashes[Random.Range(0, weaponSo.muzzleFlashes.Length)];
                flashSr.color = weaponSo.muzzleFlashColor;
                
                _utilities.DelayExecute(() => { flashSr.sprite = null; }, 0.1f);
            }
            
            //Debug.Log($"Shoot {weaponSo.name} with {weaponSo.projectile?.sprite?.name ?? "null"} @ {Time.time}");

            return true;
        }

        public override bool UseContinuous(UseParameters useParameters) => false;
        public override bool UseOnceSecondary(UseParameters useParameters) => false;
        public override bool UseContinuousSecondary(UseParameters useParameters) => false;
    }
}