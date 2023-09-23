using System;
using Entities.Enemies;
using Inventory;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Entities
{
    [RequireComponent(typeof(EnemyEntity))]
    public class EnemyLootManager : MonoBehaviour
    {
        // [Serializable]
        // public struct LootDrop
        // {
        //     public Item item;
        //     [Range(0f, 100f)] public float dropChance;
        // }
        //
        // [SerializeField] private bool dropMultiple;
        // [SerializeField] private LootDrop[] lootTable;

        private void Start()
        {
            GetComponent<EnemyEntity>().OnDeath += DropLoot;
        }

        private void DropLoot(EnemySo enemySo)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var lootDrop in enemySo.lootTable)
            {
                var rng = Random.Range(0f, 100f);

                if (rng > lootDrop.dropChance) continue;
                // drop item
                InventoryManager.SpawnItem(lootDrop.item, transform.position);

                if (!enemySo.dropMultiple) return;
            }
        }
    }
}
