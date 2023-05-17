using Inventory.Item_Logic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace Inventory.Item_Types
{
    // This intentionally doesn't have an asset menu entry as this is supposed to be used by other scriptable objects.
    public abstract class UsableItemSo : ItemSo
    {
        [UsedImplicitly] public ItemLogic.LogicCode logicCode;
        [FormerlySerializedAs("attackSpeed")] public float attackCooldown;
        public float energyCost;
        [Range(0f, 1f)] public float recoilHorizontal;
        [Range(0f, 1f)] public float recoilAngular;
        public float recoilSpeedHorizontal;
        public float recoilSpeedAngular;
        public float cameraShakeTime;
        public float cameraShakeStrength;
        public float playerRecoilStrength;

        [System.NonSerialized] public bool isOnCooldown;
    }
}
