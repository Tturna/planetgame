using System.Linq;
using Entities;
using Inventory.Item_SOs;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory.Crafting
{
    public class CraftingStation : MonoBehaviour
    {
        [SerializeField] protected ItemSo[] craftables;
        [SerializeField] protected GameObject craftableSlotsParent;
        [SerializeField] protected GameObject craftingMenu;
        
        private Interactable interactable;
        private Image[] craftableSlots;

        private void Awake()
        {
            interactable = GetComponent<Interactable>();
            interactable.InteractedImmediate += ToggleCraftingStation;
        }

        private void Start()
        {
            craftableSlots = (from Transform slot in craftableSlotsParent.transform select slot.GetChild(0).GetComponent<Image>()).ToArray();

            for (var i = 0; i < craftables.Length; i++)
            {
                var craftable = craftables[i];
                craftableSlots[i].sprite = craftable.sprite;
                craftableSlots[i].color = Color.white;
                craftableSlots[i].SetNativeSize();
            }
        }

        private void ToggleCraftingStation(GameObject interactSourceObject)
        {
            ValidateCraftables();
            ToggleCraftingMenu();
        }
        
        private void ValidateCraftables()
        {
            Debug.Log($"Validating craftables for {name}...");
            for (var i = 0; i < craftables.Length; i++)
            {
                var craftable = craftables[i];
                var canCraft = InventoryManager.CanCraft(craftable);
                Debug.Log($"Can craft {craftable.name}: {canCraft}");
                craftableSlots[i].color = canCraft ? Color.white : new Color(1f, 1f, 1f, .5f);
            }
        }    
        
        private void ToggleCraftingMenu()
        {
            craftingMenu.SetActive(!craftingMenu.activeSelf);
        }    
    }
}