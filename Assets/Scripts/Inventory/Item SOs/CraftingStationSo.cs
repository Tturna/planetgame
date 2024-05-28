using Inventory.Crafting;
using UnityEngine;

namespace Inventory.Item_SOs
{
    [CreateAssetMenu(fileName = "CraftingStation", menuName = "SO/CraftingStation")]
    public class CraftingStationSo : PlaceableSo
    {
        public RecipeSo[] recipes;
    }
}