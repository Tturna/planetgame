using System;
using Inventory.Item_SOs;
using UnityEngine;

namespace Inventory.Crafting
{
    [Serializable]
    public struct CraftingResource
    {
        public ItemSo item;
        public int amount;
    }
    
    [CreateAssetMenu(fileName="Recipe", menuName="SO/Recipe")]
    public class RecipeSo : ScriptableObject
    {
        public CraftingResource[] ingredients;
        public CraftingResource[] results;
    }
}