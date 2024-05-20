using Entities;
using UnityEngine;

namespace Inventory.Crafting
{
    [RequireComponent(typeof(Interactable))]
    public class CraftingStation : MonoBehaviour
    {
        [SerializeField] private RecipeSo[] recipes;
        
        private Interactable interactable;
        
        private void Awake()
        {
            interactable = GetComponent<Interactable>();
            interactable.InteractedImmediate += ToggleCraftingMenu;
        }
        
        private void ToggleCraftingMenu(GameObject interactSourceObject)
        {
            CraftingManager.ToggleCraftingMenu(recipes);
        }
    }
}