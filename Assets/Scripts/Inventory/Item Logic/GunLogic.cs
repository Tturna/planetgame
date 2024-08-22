using Entities;
using Inventory.Item_SOs;
using UnityEngine;
using Utilities;

namespace Inventory.Item_Logic
{
    public class GunLogic : ItemLogicBase
    {
        private GameObject _muzzleFlashObject;
        private SpriteRenderer _muzzleFlashSr;
        private ParticleSystem _casingParticleSystem;

        private void Shoot(UseParameters useParameters, WeaponSo weaponSo)
        {
            var pos = (Vector2)useParameters.equippedItemObject.transform.position;
            var rot = useParameters.equippedItemObject.transform.eulerAngles;
            
            // object pooler will check if the pool exists so we don't have to.
            ObjectPooler.CreatePoolIfDoesntExist("Projectile Pool", GameUtilities.instance.GetProjectilePrefab(), 10, true);
            var projectile = ObjectPooler.GetObject("Projectile Pool");

            if (projectile == null)
            {
                throw new System.NullReferenceException("Projectile object is null from object pool!");
            }
            
            projectile.transform.SetParent(useParameters.equippedItemObject.transform);
            projectile.transform.position = pos;
            projectile.transform.eulerAngles = rot;
            
            var localPos = weaponSo.muzzlePosition;
            localPos.y = useParameters.flipY ? -localPos.y : localPos.y;
            projectile.transform.localPosition = localPos;
            projectile.transform.SetParent(null);
            
            var entity = projectile.GetComponent<ProjectileEntity>();
            entity.Init(weaponSo.projectile);
            
            if (weaponSo.muzzleFlashes.Length > 0)
            {
                if (!_muzzleFlashObject)
                {
                    _muzzleFlashObject = useParameters.equippedItemObject.transform.GetChild(0).GetChild(0).gameObject;
                    _muzzleFlashSr = _muzzleFlashObject.GetComponent<SpriteRenderer>();
                }
                
                _muzzleFlashObject.SetActive(true);
                _muzzleFlashObject.transform.localPosition = weaponSo.muzzlePosition;
                
                _muzzleFlashSr.sprite = weaponSo.muzzleFlashes[Random.Range(0, weaponSo.muzzleFlashes.Length)];
                _muzzleFlashSr.color = weaponSo.muzzleFlashColor;
                
                GameUtilities.instance.DelayExecute(() => { _muzzleFlashObject.SetActive(false); }, 0.07f);
            }

            if (!_casingParticleSystem)
            {
                _casingParticleSystem = useParameters.equippedItemObject.transform.GetChild(1).GetChild(0).GetComponent<ParticleSystem>();
            }
            
            _casingParticleSystem.Play();
        }
        
        public override bool UseOnce(UseParameters useParameters)
        {
            var weaponSo = (WeaponSo)useParameters.attackItem.itemSo;

            if (weaponSo.fullAuto) return false;
            
            Shoot(useParameters, weaponSo);
            
            return true;
        }

        public override bool UseContinuous(UseParameters useParameters)
        {
            var weaponSo = (WeaponSo)useParameters.attackItem.itemSo;
            
            if (!weaponSo.fullAuto) return false;
            
            Shoot(useParameters, weaponSo);
            
            return true;
        }

        public override bool UseOnceSecondary(UseParameters useParameters) => false;
        public override bool UseContinuousSecondary(UseParameters useParameters) => false;
    }
}