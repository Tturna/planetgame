using System;
using Entities.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Inventory.Inventory
{
    [RequireComponent(typeof(EntityController))]
    public class EntityLootManager : MonoBehaviour
    {
        [Serializable]
        public struct LootDrop
        {
            public Item item;
            [Range(0f, 100f)] public float dropChance;
        }

        [SerializeField] private bool dropMultiple;
        [SerializeField] private LootDrop[] lootTable;

        private void Start()
        {
            GetComponent<EntityController>().OnDeath += DropLoot;
        }

        private void DropLoot()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var lootDrop in lootTable)
            {
                var rng = Random.Range(0f, 100f);

                if (rng > lootDrop.dropChance) continue;
                // drop item
                InventoryManager.SpawnItem(lootDrop.item, transform.position);

                if (!dropMultiple) return;
            }
        }
    }
}
