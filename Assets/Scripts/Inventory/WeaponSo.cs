using Inventory.Item_Logic;
using UnityEngine;

namespace Inventory
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "SO/Weapon")]
    public class WeaponSo : ItemSo
    {
        public ItemLogic.LogicCode logicCode;
        public ProjectileData projectile;
        public Vector2 muzzlePosition;
        public float energyCost;
        [Range(0f, 1f)] public float recoilHorizontal;
        [Range(0f, 1f)] public float recoilAngular;
        public float recoilSpeedHorizontal;
        public float recoilSpeedAngular;
        public float cameraShakeTime;
        public float cameraShakeStrength;
        public float playerRecoilStrength;
    }
}
