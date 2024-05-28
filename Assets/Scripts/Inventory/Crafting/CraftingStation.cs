using Entities;
using UnityEngine;

namespace Inventory.Crafting
{
    [RequireComponent(typeof(Interactable))]
    public class CraftingStation : MonoBehaviour
    {
        private RecipeSo[] recipes;
        private Interactable interactable;
        
        private void Awake()
        {
            interactable = GetComponent<Interactable>();
            interactable.OnInteractImmediate += _ => CraftingManager.ToggleCraftingMenu(recipes);
            interactable.OnOutOfRange += _ => CraftingManager.ToggleCraftingMenu(false);
        }

        public void SetRecipes(RecipeSo[] initialRecipes)
        {
            recipes = initialRecipes;
        }
    }
}