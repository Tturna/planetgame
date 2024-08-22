using Entities;
using UnityEngine;

namespace Inventory.Item_SOs
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "SO/Weapon")]
    public class WeaponSo : UsableItemSo
    {
        public ProjectileData projectile;
        public Vector2 muzzlePosition;
        public Sprite[] muzzleFlashes;
        public Color muzzleFlashColor;
        public bool fullAuto;
    }
}
