using Entities;
using Inventory.Item_Types;
using ProcGen;
using UnityEngine;

namespace Inventory.Item_Logic
{
    public class GunLogic : ItemLogicBase
    {
        private Utilities _utilities;
        private GameObject _projectilePrefab;
        
        public override bool UseOnce(GameObject equippedItemObject, Item attackItem, bool flipY, PlayerController player, PlanetGenerator usePlanet = null)
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
            
            // Choose random muzzle flash
            if (weaponSo.muzzleFlashes.Length > 0)
            {
                var muzzleFlashObject = equippedItemObject.transform.GetChild(0).gameObject;
                muzzleFlashObject.transform.localPosition = weaponSo.muzzlePosition;
                
                var flashSr = muzzleFlashObject.GetComponent<SpriteRenderer>();
                flashSr.sprite = weaponSo.muzzleFlashes[Random.Range(0, weaponSo.muzzleFlashes.Length)];
                flashSr.color = weaponSo.muzzleFlashColor;
                
                _utilities.DelayExecute(() => { flashSr.sprite = null; }, 0.1f);
            }
            
            //Debug.Log($"Shoot {weaponSo.name} with {weaponSo.projectile?.sprite?.name ?? "null"} @ {Time.time}");

            return true;
        }

        public override bool UseContinuous(GameObject equippedItemObject, Item attackItem, bool flipY, PlayerController player, PlanetGenerator usePlanet = null) => false;
        public override bool UseOnceSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, PlayerController player, PlanetGenerator usePlanet = null) => false;
        public override bool UseContinuousSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, PlayerController player, PlanetGenerator usePlanet = null) => false;
    }
}