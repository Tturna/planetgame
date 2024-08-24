using System;
using UnityEngine;

namespace Inventory
{
    [Serializable]
    public struct LootDrop
    {
        public Item item;
        [Range(0f, 100f)] public float dropChance;
    }
}