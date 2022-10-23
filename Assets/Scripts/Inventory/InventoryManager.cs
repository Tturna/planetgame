using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory
{
    internal struct Slot
    {
        internal Item Item;
        internal int Stack;
    }

    public class InventoryManager : MonoBehaviour
    {
        [SerializeField] private GameObject inventoryObject;
        public Sprite emptySlotSprite;
        public Sprite filledSlotSprite;
        public Sprite emptySlotSelectedSprite;
        public Sprite filledSlotSelectedSprite;

        public static InventoryManager Instance;

        private static Transform[] _slotObjects;
        private static Slot[] _slots;
        private static int _selectedIndex;

        public delegate void SlotSelectedHandler(Item slotItem);
        public static event SlotSelectedHandler SlotSelected;

        private void Start()
        {
            Instance = this;
            
            if (!inventoryObject)
            {
                Debug.LogError("No inventory object set for inventory manager.");
            }

            _slotObjects = inventoryObject.transform.Cast<Transform>().ToArray();
            _slots = new Slot[_slotObjects.Length];
            
            SelectSlot(_selectedIndex);
        }

        private float scrollDelta;
        private void Update()
        {
            // Check controls

            scrollDelta = Input.mouseScrollDelta.y;

            if (scrollDelta != 0)
            {
                if (SelectSlot(_selectedIndex + (int)scrollDelta))
                {
                    DeselectSlot(_selectedIndex);
                    _selectedIndex += (int)scrollDelta;
                }
            }

            //TODO: Smart slot selection with numeric keys
            
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                DeselectSlot(_selectedIndex);
                SelectSlot(0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                DeselectSlot(_selectedIndex);
                SelectSlot(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                DeselectSlot(_selectedIndex);
                SelectSlot(2);
            }
        }

        public static bool AddItem(Item item)
        {
            // Copy item
            var copy = new Item(item);
            
            // Check if inventory has space
            if (!HasSpaceForItem(copy, out var index)) return false;

            // If slot was empty, change the sprite
            if (_slots[index].Stack == 0)
            {
                _slotObjects[index].gameObject.GetComponent<Image>().sprite = Instance.filledSlotSprite;
            }
            
            // Add item to slot with space
            AddToStack(index, copy);

            var slotItem = _slotObjects[index].GetChild(0).gameObject;
            var img = slotItem.GetComponent<Image>();
            img.color = Color.white;
            img.sprite = copy.itemSo.sprite;
            img.SetNativeSize();
            
            // Update selected slot
            if (index == _selectedIndex) SelectSlot(_selectedIndex);
            
            return true;
        }

        public static bool HasSpaceForItem(Item item, out int availableIndex)
        {
            availableIndex = -1;
            for (var i = 0; i < _slots.Length; i++)
            {
                // This will find the first empty slot
                if (availableIndex == -1 && _slots[i].Stack == 0)
                {
                    availableIndex = i;
                }
                // This will find the first available stack
                // This is prioritized over an empty slot
                else if (_slots[i].Item == item)
                {
                    if (_slots[i].Stack >= item.itemSo.maxStack) continue;
                    availableIndex = i;
                    break;
                }
            }

            return availableIndex >= 0;
        }

        private static void AddToStack(int index, Item item)
        {
            if (_slots[index].Item == item)
            {
                _slots[index].Stack++;
                return;
            }

            _slots[index].Item = item;
            _slots[index].Stack = 1;
        }

        private static bool SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > _slots.Length - 1) return false;
            
            var img = _slotObjects[slotIndex].gameObject.GetComponent<Image>();
            img.sprite = _slots[slotIndex].Stack > 0 ? Instance.filledSlotSelectedSprite : Instance.emptySlotSelectedSprite;
            
            OnSlotSelected(_slots[slotIndex].Item);
            return true;
        }

        private static void DeselectSlot(int slotIndex)
        {
            var img = _slotObjects[slotIndex].GetComponent<Image>();
            img.sprite = _slots[slotIndex].Stack > 0 ? Instance.filledSlotSprite : Instance.emptySlotSprite;
        }

        public static Item GetSelectedItem()
        {
            return _slots[_selectedIndex].Item;
        }

        private static void OnSlotSelected(Item slotItem)
        {
            SlotSelected?.Invoke(slotItem);
        }
    }
}