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
    }
}
