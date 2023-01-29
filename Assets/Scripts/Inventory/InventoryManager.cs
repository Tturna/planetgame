using System;
using System.Linq;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory
{
    internal struct Slot
    {
        internal Item item;
        internal int stack;
        internal bool isInStash;
        internal int index;

        internal Slot(int index)
        {
            item = default;
            stack = default;
            isInStash = default;
            this.index = index;
        }
    }

    public class InventoryManager : MonoBehaviour
    {
        [SerializeField] private GameObject inventoryObject;
        public Sprite emptySlotSprite;
        public Sprite filledSlotSprite;
        public Sprite emptySlotSelectedSprite;
        public Sprite filledSlotSelectedSprite;
        public Sprite emptyStashSprite;
        public Sprite filledStashSprite;

        public static InventoryManager instance;

        private static GameObject _stashObject;
        private static GameObject _mouseSlotObject;
        private static Transform[] _hotSlotObjects;
        private static Transform[] _stashSlotObjects;
        private static Slot[] _hotSlots;
        private static Slot[] _stashSlots;
        private static Slot _mouseSlot;
        private static int _selectedIndex;

        public delegate void SlotSelectedHandler(Item slotItem);
        public static event SlotSelectedHandler SlotSelected;

        private void Start()
        {
            instance = this;
            
            if (!inventoryObject)
            {
                Debug.LogError("No inventory object set for inventory manager.");
            }

            _hotSlotObjects = inventoryObject.transform.GetChild(0).Cast<Transform>().ToArray();
            _stashObject = inventoryObject.transform.GetChild(1).gameObject;
            _stashSlotObjects = _stashObject.transform.Cast<Transform>().ToArray();
            _mouseSlotObject = inventoryObject.transform.GetChild(2).gameObject;
            _hotSlots = new Slot[_hotSlotObjects.Length];
            _stashSlots = new Slot[_stashSlotObjects.Length];
            _mouseSlot = new Slot(-1);

            // Initialize values for slots
            for (var i = 0; i < _stashSlots.Length; i++)
            {
                if (i < _hotSlots.Length)
                {
                    _hotSlots[i].index = i;
                }

                _stashSlots[i].index = i;
                _stashSlots[i].isInStash = true;
            }
            
            SelectSlot(_selectedIndex);
            _stashObject.SetActive(false);
        }

        private float scrollDelta;
        private void Update()
        {
            // Check controls
            scrollDelta = Input.mouseScrollDelta.y;

            if (scrollDelta != 0)
            {
                if (SelectSlot(_selectedIndex - (int)scrollDelta))
                {
                    DeselectSlot(_selectedIndex);
                    _selectedIndex -= (int)scrollDelta;
                }
            }

            // Check for number keys
            for (var i = 1; i <= 8; i++)
            {
                if (!Input.GetKeyDown(i.ToString())) continue;
                DeselectSlot(_selectedIndex);
                SelectSlot(i - 1);
            }
            
            // Check for inventory switch
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _stashObject.SetActive(!_stashObject.activeInHierarchy);
            }
            
            // Make mouse slot follow the cursor
            _mouseSlotObject.transform.position = Input.mousePosition;
            
            if (Input.GetMouseButtonDown(0))
            {
                // Check if grabbing item, swapping item, putting down item or dropping a stack.
                var clickedSlotObject = GetHoveringSlotObject();
                var success = GetSlotFromObject(clickedSlotObject, out var slot);
                if (!success) return;

                HandleMouseOne(ref slot, clickedSlotObject);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                // Check if splitting stacks or dropping single items.
                var clickedSlotObject = GetHoveringSlotObject();
                var success = GetSlotFromObject(clickedSlotObject, out var slot);
                if (!success) return;
                
                HandleMouseTwo(ref slot, clickedSlotObject);
            }
        }

        private static void HandleMouseOne(ref Slot slot, GameObject clickedSlotObject)
        {
            if (!clickedSlotObject) return;
            
            // Check if both mouse slot and clicked slot have an item
            if (_mouseSlot.stack > 0 && slot.stack > 0)
            {
                // if so, check if they're the same
                if (slot.item.itemSo.id == _mouseSlot.item.itemSo.id)
                {
                    // if so, put the whole stack from mouse slot to clicked slot, if it fits
                    if (slot.stack < slot.item.itemSo.maxStack)
                    {
                        var amount = _mouseSlot.stack;

                        if (slot.stack + amount > slot.item.itemSo.maxStack)
                        {
                            amount = slot.item.itemSo.maxStack - slot.stack;
                        }

                        slot.stack += amount;
                        _mouseSlot.stack -= amount;

                        if (_mouseSlot.stack == 0)
                        {
                            _mouseSlot = new Slot();
                        }
                        
                        UpdateLogicalSlot(slot);
                        UpdateSlotGraphics(ref slot, clickedSlotObject.transform);
                        UpdateMouseSlotGraphics();

                        return;
                    }
                }
            }

            SwapMouseSlot(ref slot, clickedSlotObject);
        }
        
        private static void HandleMouseTwo(ref Slot slot, GameObject clickedSlotObject)
        {
            // If mouse slot has an item...
            if (_mouseSlot.stack > 0)
            {
                if (!clickedSlotObject)
                {
                    // If mouse slot has items and right click hit nothing, drop 1 item
                    // TODO: Drop 1 item
                    return;
                }
                        
                // check if clicked slot has items
                if (slot.stack > 0)
                {
                    // if so, check if it's the same as what is in the mouse slot
                    if (slot.item.itemSo.id == _mouseSlot.item.itemSo.id)
                    {
                        // if so, add 1 to the clicked slot
                        if (slot.stack >= slot.item.itemSo.maxStack) return;
                        
                        slot.stack++;
                        _mouseSlot.stack--;

                        if (_mouseSlot.stack == 0)
                        {
                            _mouseSlot = new Slot();
                        }
                            
                        UpdateLogicalSlot(slot);
                        UpdateSlotGraphics(ref slot, clickedSlotObject.transform);
                        UpdateMouseSlotGraphics();
                    }
                    else
                    {
                        // if the item is not the same, swap stacks
                        SwapMouseSlot(ref slot, clickedSlotObject);
                    }
                }
                else
                {
                    // if clicked slot has no item, add 1 to the empty slot
                    var index = slot.index;
                    slot = _mouseSlot;
                    slot.index = index;
                    slot.stack = 1;
                                
                    // Reduce 1 from the mouse slot
                    _mouseSlot.stack--;

                    if (_mouseSlot.stack == 0)
                    {
                        _mouseSlot = new Slot(-1);
                    }
                    
                    UpdateLogicalSlot(slot);
                    UpdateSlotGraphics(ref slot, clickedSlotObject.transform);
                    UpdateMouseSlotGraphics();
                }
            }
            else
            {
                // if mouse slot has no items, check if clicking on a slot
                if (!clickedSlotObject) return;
                if (slot.stack <= 0) return;
                
                // If clicked slot has items, grab half
                var half = Mathf.CeilToInt(slot.stack / 2f);

                slot.stack -= half;
                _mouseSlot = slot;
                _mouseSlot.stack = half;
                _mouseSlot.index = -1;

                if (slot.stack == 0)
                {
                    slot = new Slot(slot.index);

                    if (_selectedIndex == slot.index)
                    {
                        // TODO: Unequip an item if you take it from the equipped slot
                    }
                }
                
                UpdateLogicalSlot(slot);
                UpdateSlotGraphics(ref slot, clickedSlotObject.transform);
                UpdateMouseSlotGraphics();
            }
        }
        
        public static bool AddItem(Item item)
        {
            // Copy item
            var copy = new Item(item);
            
            // Check if inventory has space
            if (!HasSpaceForItem(copy, out var index, out var isStash)) return false;

            // Create temporary variables to make inventory management easier with the separate hotbar and stash
            var segmentSlots = isStash ? _stashSlots : _hotSlots;
            var segmentObjects = isStash ? _stashSlotObjects : _hotSlotObjects;
            
            // If slot was empty, change the sprite
            if (segmentSlots[index].stack == 0)
            {
                segmentObjects[index].gameObject.GetComponent<Image>().sprite = instance.filledSlotSprite;
            }
            
            // Add item to slot with space
            segmentSlots = AddToStack(segmentSlots, index, copy);

            UpdateSlotGraphics(ref segmentSlots[index], segmentObjects[index]);
            
            // Update the inventory variables using the temporary variables. Required because structs are value types.
            if (isStash)
            {
                _stashSlots = segmentSlots;
            }
            else
            {
                _hotSlots = segmentSlots;
            }
            
            // Update selected slot
            if (index == _selectedIndex) SelectSlot(_selectedIndex);
            
            return true;
        }

        private static void SwapMouseSlot(ref Slot slot, GameObject clickedSlotObject)
        {
            (_mouseSlot, slot) = (slot, _mouseSlot);
            (_mouseSlot.index, slot.index) = (slot.index, _mouseSlot.index);

            if (_selectedIndex == slot.index && slot.item == null)
            {
                // TODO: Unequip an item if you take it from the equipped slot
            }

            UpdateLogicalSlot(slot);
            UpdateSlotGraphics(ref slot, clickedSlotObject.transform);
            UpdateMouseSlotGraphics();
        }

        public static bool HasSpaceForItem(Item item, out int availableIndex, out bool isStash)
        {
            int FindIndex(Slot[] slotSegment)
            {
                var availableIndex = -1;
                for (var i = 0; i < slotSegment.Length; i++)
                {
                    // This will find the first empty slot in the slot segment (hotbar or stash)
                    if (availableIndex == -1 && slotSegment[i].stack == 0)
                    {
                        availableIndex = i;
                    }
                    // This will find the first available stack
                    // This is prioritized over an empty slot
                    else if (slotSegment[i].item?.itemSo.id == item.itemSo.id)
                    {
                        if (slotSegment[i].stack >= item.itemSo.maxStack) continue;
                        availableIndex = i;
                        break;
                    }
                }

                return availableIndex;
            }

            isStash = false;
            availableIndex = FindIndex(_hotSlots);

            if (availableIndex >= -1) return true;
            
            availableIndex = FindIndex(_stashSlots);

            if (availableIndex == -1) return false;
            isStash = true;
            return true;
        }

        private static Slot[] AddToStack(Slot[] segment, int index, Item item)
        {
            if (segment[index].item?.itemSo.id == item.itemSo.id)
            {
                segment[index].stack++;
                return segment;
            }

            segment[index].item = item;
            segment[index].stack = 1;
            return segment;
        }

        private static bool SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > _hotSlots.Length - 1) return false;
            
            var img = _hotSlotObjects[slotIndex].gameObject.GetComponent<Image>();
            img.sprite = _hotSlots[slotIndex].stack > 0 ? instance.filledSlotSelectedSprite : instance.emptySlotSelectedSprite;
            
            OnSlotSelected(_hotSlots[slotIndex].item);
            return true;
        }

        private static void DeselectSlot(int slotIndex)
        {
            var img = _hotSlotObjects[slotIndex].GetComponent<Image>();
            img.sprite = _hotSlots[slotIndex].stack > 0 ? instance.filledSlotSprite : instance.emptySlotSprite;
        }

        private static void UpdateSlotGraphics(ref Slot slot, Transform slotObject)
        {
            // var segmentObjects = isStash ? _stashSlotObjects : _hotSlotObjects;
            // var segmentSlots = isStash ? _stashSlots : _hotSlots;
            
            // Slot item
            var itemImg = slotObject.GetChild(0).GetComponent<Image>();
            itemImg.sprite = slot.item?.itemSo.sprite;
            itemImg.color = itemImg.sprite ? Color.white : Color.clear;
            itemImg.SetNativeSize();

            var itemStack = slotObject.GetChild(1).GetComponent<TextMeshProUGUI>();
            var stack = slot.stack;
            itemStack.text = stack > 1 ? stack.ToString() : "";
            
            // Update slot graphics
            var slotImg = slotObject.GetComponent<Image>();
            if (stack > 0)
            {
                if (_selectedIndex == slot.index && !slot.isInStash)
                {
                    slotImg.sprite = instance.filledSlotSelectedSprite;
                }
                else if (slot.isInStash)
                {
                    slotImg.sprite = instance.filledStashSprite;
                }
                else
                {
                    slotImg.sprite = instance.filledSlotSprite;
                }
            }
            else
            {
                if (_selectedIndex == slot.index && !slot.isInStash)
                {
                    slotImg.sprite = instance.emptySlotSelectedSprite;
                }
                else if (slot.isInStash)
                {
                    slotImg.sprite = instance.emptyStashSprite;
                }
                else
                {
                    slotImg.sprite = instance.emptySlotSprite;
                }
            }
        }

        private static void UpdateMouseSlotGraphics()
        {
            var itemImg = _mouseSlotObject.transform.GetChild(0).GetComponent<Image>();
            itemImg.sprite = _mouseSlot.item?.itemSo.sprite;
            itemImg.color = itemImg.sprite ? Color.white : Color.clear;
            itemImg.SetNativeSize();

            var itemStack = _mouseSlotObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            var stack = _mouseSlot.stack;
            itemStack.text = stack > 1 ? stack.ToString() : "";
        }

        private static void UpdateLogicalSlot(Slot slot)
        {
            // Update inventory
            if (slot.isInStash)
            {
                _stashSlots[slot.index] = slot;
            }
            else
            {
                _hotSlots[slot.index] = slot;
            }
        }
        
        /// <summary>
        /// Get the GameObject of the UI element under the cursor where the GameObject name in lower case contains "slot". Returns null if not found.
        /// </summary>
        /// <returns>GameObject of UI slot or null</returns>
        private static GameObject GetHoveringSlotObject()
        {
            var results = UIUtilities.MouseRaycast();
            return results.Find(x => x.gameObject.name.ToLower().Contains("slot")).gameObject;
        }

        private static bool GetSlotFromObject(GameObject slotObject, out Slot slot)
        {
            if (!slotObject)
            {
                slot = new Slot();
                return false;
            }

            var isStash = slotObject.transform.parent.gameObject == _stashObject;
            var segment = isStash ? _stashSlots : _hotSlots;
                
            var slotIndex = isStash
                ? Array.FindIndex(_stashSlotObjects, x => x == slotObject.transform)
                : Array.FindIndex(_hotSlotObjects, x => x == slotObject.transform);

            slot = segment[slotIndex];

            return true;
        }
        
        private static void OnSlotSelected(Item slotItem)
        {
            SlotSelected?.Invoke(slotItem);
        }
    }
}