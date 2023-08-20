using UnityEngine;

namespace Inventory.Inventory.Item_Types
{
    [CreateAssetMenu(fileName = "Melee Weapon", menuName = "SO/Melee")]
    public class MeleeSo : WeaponSo
    {
        public float damage;
        public float knockback;
        public Vector2[] colliderPoints;
        public Vector2 swingTrailOffset;
        public float swingTrailWidth;
    }
}
