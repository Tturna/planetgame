using System.Linq;
using Entities;
using TMPro;
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
        [SerializeField] protected Transform ingredientRow1Parent;
        [SerializeField] protected Transform ingredientRow2Parent;
        
        private Interactable _interactable;
        private Image[] _recipeSlotImages;
        private GameObject _selectedRecipeSlotObject;
        private RecipeSo _selectedRecipe;
        private CraftableRecipe[] _currentStationRecipes;
        private CraftableRecipe _selectedCraftableRecipe;
        private Image[] ingredientRow1Images;
        private Image[] ingredientRow2Images;
        private TextMeshProUGUI[] ingredientRow1AmountTexts;
        private TextMeshProUGUI[] ingredientRow2AmountTexts;
        
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

            ingredientRow1Images = (from Transform ingredientSlot in ingredientRow1Parent
                select ingredientSlot.GetChild(0).GetComponent<Image>()).ToArray();
            
            ingredientRow2Images = (from Transform ingredientSlot in ingredientRow2Parent
                select ingredientSlot.GetChild(0).GetComponent<Image>()).ToArray();
            
            ingredientRow1AmountTexts = (from Transform ingredientSlot in ingredientRow1Parent
                select ingredientSlot.GetChild(1).GetComponent<TextMeshProUGUI>()).ToArray();
            
            ingredientRow2AmountTexts = (from Transform ingredientSlot in ingredientRow2Parent
                select ingredientSlot.GetChild(1).GetComponent<TextMeshProUGUI>()).ToArray();
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
                    if (!_selectedCraftableRecipe.canCraft)
                    {
                        Debug.Log($"Cannot craft {_selectedCraftableRecipe.recipe.name}");
                        return;
                    }

                    Debug.Log($"Crafting recipe: {_selectedCraftableRecipe.recipe.name}");
                    InventoryManager.Craft(_selectedCraftableRecipe.recipe);
                    ValidateCraftables();
                }
                else
                {
                    _selectedRecipeSlotObject = hoveringCraftableSlotObject.gameObject;
                    selectedRecipeOverlayImage.gameObject.SetActive(true);
                    selectedRecipeOverlayImage.transform.position = _selectedRecipeSlotObject.transform.position;
                    selectedRecipeOverlayImage.rectTransform.anchoredPosition += Vector2.one;
                    
                    var index = _selectedRecipeSlotObject.transform.GetSiblingIndex();
                    
                    if (index + 1 > _currentStationRecipes.Length) return;
                    
                    _selectedCraftableRecipe = _currentStationRecipes[index];

                    for (var i = 0; i < 10; i++)
                    {
                        if (_selectedCraftableRecipe.recipe.ingredients.Length > i)
                        {
                            if (i < 5)
                            {
                                ingredientRow1Images[i].transform.parent.gameObject.SetActive(true);
                                ingredientRow1Images[i].sprite = _selectedCraftableRecipe.recipe.ingredients[i].item.sprite;
                                ingredientRow1Images[i].SetNativeSize();
                                ingredientRow1AmountTexts[i].text = _selectedCraftableRecipe.recipe.ingredients[i].amount.ToString();
                            }
                            else
                            {
                                ingredientRow2Images[i - 5].transform.parent.gameObject.SetActive(true);
                                ingredientRow2Images[i - 5].sprite = _selectedCraftableRecipe.recipe.ingredients[i].item.sprite;
                                ingredientRow2Images[i - 5].SetNativeSize();
                                ingredientRow2AmountTexts[i - 5].text = _selectedCraftableRecipe.recipe.ingredients[i].amount.ToString();
                            }
                        }
                        else
                        {
                            if (i < 5)
                            {
                                ingredientRow1Images[i].transform.parent.gameObject.SetActive(false);
                            }
                            else
                            {
                                ingredientRow2Images[i - 5].transform.parent.gameObject.SetActive(false);
                            }
                        }
                    }
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