using System;
using UnityEngine;

namespace Inventory.Inventory
{
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
        
        public void DropLoot()
        {
            // TODO: Check what copilot fucked up
            foreach (var lootDrop in lootTable)
            {
                if (UnityEngine.Random.Range(0f, 100f) <= lootDrop.dropChance)
                {
                    if (dropMultiple)
                    {
                        var amount = UnityEngine.Random.Range(1, 4);
                        for (var i = 0; i < amount; i++)
                        {
                            // InventoryManager.instance.AddItem(lootDrop.item);
                        }
                    }
                    else
                    {
                        // InventoryManager.instance.AddItem(lootDrop.item);
                    }
                }
            }
        }
    }
}
