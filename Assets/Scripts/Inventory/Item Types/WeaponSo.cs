using UnityEngine;

namespace Inventory.Inventory.Item_Types
{
    [CreateAssetMenu(fileName = "Weapon", menuName = "SO/Weapon")]
    public class WeaponSo : UsableItemSo
    {
        public ProjectileData projectile;
        public Vector2 muzzlePosition;
        public Sprite[] muzzleFlashes;
        public Color muzzleFlashColor;
    }
}
