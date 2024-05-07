using UnityEngine;

namespace Inventory.Item_SOs
{
    [CreateAssetMenu(fileName = "Melee Weapon", menuName = "SO/Melee")]
    public class MeleeSo : WeaponSo
    {
        public float damage;
        public float knockback;
        public float critChance;
        public Vector2[] colliderPoints;
        public Vector2 swingTrailOffset;
        public float swingTrailWidth;
    }
}
