using System.Linq;
using Entities;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace Inventory.Crafting
{
    public class CraftingManager : MonoBehaviour
    {
        private struct CraftableRecipe
        {
            public RecipeSo recipe;
            public bool canCraft;
        }
        
        [SerializeField] protected GameObject craftingMenu;
        [SerializeField] protected GameObject recipeSlotParent;
        [SerializeField] protected Image selectedRecipeOverlayImage;
        
        private Interactable _interactable;
        private Image[] _recipeSlotImages;
        private GameObject _selectedRecipeSlotObject;
        private RecipeSo _selectedRecipe;
        private CraftableRecipe[] _currentStationRecipes;
        
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

        private void Start()
        {
            _recipeSlotImages = (from Transform slot in recipeSlotParent.transform select
                slot.GetChild(0).GetComponent<Image>()).ToArray();
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
                    var index = _selectedRecipeSlotObject.transform.GetSiblingIndex();
                    var craftableRecipe = _currentStationRecipes[index];
                    
                    if (!craftableRecipe.canCraft)
                    {
                        Debug.Log($"Cannot craft {craftableRecipe.recipe.name}");
                        return;
                    }

                    Debug.Log($"Crafting i: {index}, recipe: {craftableRecipe.recipe.name}");
                    InventoryManager.Craft(craftableRecipe.recipe);
                    ValidateCraftables();
                }
                else
                {
                    _selectedRecipeSlotObject = hoveringCraftableSlotObject.gameObject;
                    selectedRecipeOverlayImage.gameObject.SetActive(true);
                    selectedRecipeOverlayImage.transform.position = _selectedRecipeSlotObject.transform.position;
                }
            }
        }

        private void PopulateCraftingMenu(RecipeSo[] recipes)
        {
            _currentStationRecipes = recipes.Select(r => new CraftableRecipe() { recipe = r }).ToArray();

            for (var i = 0; i < _recipeSlotImages.Length; i++)
            {
                var sprite = i < recipes.Length ? recipes[i].results[0].item.sprite : null;
                _recipeSlotImages[i].sprite = sprite;
                _recipeSlotImages[i].SetNativeSize();
            }
        }

        private void ValidateCraftables()
        {
            for (var i = 0; i < _currentStationRecipes.Length; i++)
            {
                _currentStationRecipes[i].canCraft = InventoryManager.CanCraft(_currentStationRecipes[i].recipe);
                Debug.Log($"Can craft {_currentStationRecipes[i].recipe.name}: {_currentStationRecipes[i].canCraft}");
                _recipeSlotImages[i].color = _currentStationRecipes[i].canCraft ? Color.white : new Color(1f, 1f, 1f, .5f);
            }
        }    
        
        public static void ToggleCraftingMenu(RecipeSo[] recipes = null)
        {
            if (recipes?.Length > 0)
            {
                _instance.PopulateCraftingMenu(recipes);
            }
            
            var craftingMenu = _instance.craftingMenu;
            ToggleCraftingMenu(recipes != null && !craftingMenu.activeSelf);
        }
        
        public static void ToggleCraftingMenu(bool? state)
        {
            if (state == null)
            {
                _instance.craftingMenu.SetActive(!_instance.craftingMenu.activeSelf);
            }
            else
            {
                _instance.craftingMenu.SetActive((bool)state);
            }

            if (!_instance.craftingMenu.activeSelf || _instance._currentStationRecipes.Length == 0)
            {
                _instance._currentStationRecipes = null;
                _instance.selectedRecipeOverlayImage.gameObject.SetActive(false);
                _instance._selectedRecipeSlotObject = null;
                _instance._selectedRecipe = null;
                return;
            }

            _instance.ValidateCraftables();
        }
    }
}