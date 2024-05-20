using Entities;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace Inventory.Crafting
{
    public class CraftingManager : MonoBehaviour
    {
        [SerializeField] protected GameObject craftingMenu;
        
        private Interactable _interactable;
        private Image[] _recipeSlotImages;
        private GameObject _selectedRecipeSlotObject;
        private RecipeSo _selectedRecipe;
        
        private static CraftingManager _instance;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var results = UIUtilities.GetMouseRaycast();
                var hoveringCraftableSlotObject = results.Find(result => result.gameObject.CompareTag("CraftableSlot"));

                if (!hoveringCraftableSlotObject.isValid) return;

                if (hoveringCraftableSlotObject.gameObject == _selectedRecipeSlotObject)
                {
                    // craft
                }
                else
                {
                    _selectedRecipeSlotObject = hoveringCraftableSlotObject.gameObject;
                }
            }
        }

        private void ValidateCraftables()
        {
            // for (var i = 0; i < craftables.Length; i++)
            // {
            //     var craftable = craftables[i];
            //     var canCraft = InventoryManager.CanCraft(craftable);
            //     Debug.Log($"Can craft {craftable.name}: {canCraft}");
            //     craftableSlots[i].color = canCraft ? Color.white : new Color(1f, 1f, 1f, .5f);
            // }
        }    
        
        public static void ToggleCraftingMenu(RecipeSo[] recipes = null)
        {
            var craftingMenu = _instance.craftingMenu;
            
            if (recipes == null)
            {
                craftingMenu.SetActive(false);
            }
            else
            {
                craftingMenu.SetActive(!craftingMenu.activeSelf);
            }
        }
    }
}