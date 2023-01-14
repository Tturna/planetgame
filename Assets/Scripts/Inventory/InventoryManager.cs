using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inventory
{
    internal struct Slot
    {
        internal Item item;
        internal int stack;
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
            _mouseSlot = new Slot();
            
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
                if (Input.GetKeyDown(i.ToString()))
                {
                    DeselectSlot(_selectedIndex);
                    SelectSlot(i - 1);
                }
            }
            
            // Check for inventory switch
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _stashObject.SetActive(!_stashObject.activeInHierarchy);
            }
            
            // Make mouse slot follow the cursor
            _mouseSlotObject.transform.position = Input.mousePosition;
            
            // Check if clicking on slots
            if (Input.GetMouseButtonDown(0))
            {
                // TODO: Better thing for checking pointer event data. currently kinda WET
                
                var pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
            
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                var clickedObject = results.Find(x => x.gameObject.name.ToLower().Contains("slot")).gameObject;

                if (clickedObject)
                {
                    var isStash = clickedObject.transform.parent.gameObject == _stashObject;

                    var index = isStash
                        ? Array.FindIndex(_stashSlotObjects, x => x == clickedObject.transform)
                        : Array.FindIndex(_hotSlotObjects, x => x == clickedObject.transform);
                
                    if (index > -1)
                    {
                        SwapMouseSlot(index, isStash);
                    }
                }
            }
            
            // Check if splitting stacks or dropping single items
            if (Input.GetMouseButtonDown(1))
            {
                var pointerData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);
                var clickedObject = results.Find(x => x.gameObject.name.ToLower().Contains("slot")).gameObject;
                
                // If mouse slot has an item...
                if (_mouseSlot.stack > 0)
                {
                    if (clickedObject)
                    {
                        var isStash = clickedObject.transform.parent.gameObject == _stashObject;

                        var index = isStash
                            ? Array.FindIndex(_stashSlotObjects, x => x == clickedObject.transform)
                            : Array.FindIndex(_hotSlotObjects, x => x == clickedObject.transform);

                        if (index <= -1) return;
                        
                        var segment = isStash ? _stashSlots : _hotSlots;
                            
                        // check if clicked slot has items
                        if (segment[index].stack > 0)
                        {
                            // if so, check if it's the same as what is in the mouse slot
                            if (segment[index].item.itemSo.id == _mouseSlot.item.itemSo.id)
                            {
                                // if so, add 1 to the clicked slot
                                if (segment[index].stack < segment[index].item.itemSo.maxStack)
                                {
                                    segment[index].stack++;
                                            
                                    // Reduce 1 from the mouse slot
                                    _mouseSlot.stack--;

                                    if (_mouseSlot.stack == 0)
                                    {
                                        _mouseSlot = new Slot();
                                                
                                        // Update graphics
                                    }
                                }
                            }
                            // if the item is not the same, do nothing
                        }
                        else
                        {
                            // if there is no item, add 1 to the empty slot
                            segment[index] = _mouseSlot;
                            segment[index].stack = 1;
                                    
                            // Reduce 1 from the mouse slot
                            _mouseSlot.stack--;

                            if (_mouseSlot.stack == 0)
                            {
                                _mouseSlot = new Slot();
                                                
                                // Update graphics
                            }
                        }

                        if (isStash) _stashSlots = segment;
                        else _hotSlots = segment;
                    }
                    else
                    {
                        // TODO: Drop 1 item
                    }
                }
                else
                {
                    // if clicked slot has items
                    if (clickedObject)
                    {
                        var isStash = clickedObject.transform.parent.gameObject == _stashObject;

                        var index = isStash
                            ? Array.FindIndex(_stashSlotObjects, x => x == clickedObject.transform)
                            : Array.FindIndex(_hotSlotObjects, x => x == clickedObject.transform);

                        if (index <= -1) return;
                        
                        var segment = isStash ? _stashSlots : _hotSlots;
                        
                        // grab half
                        var half = Mathf.CeilToInt(segment[index].stack / 2f);

                        if (_mouseSlot.stack + half > _mouseSlot.item.itemSo.maxStack)
                        {
                            half = _mouseSlot.item.itemSo.maxStack - _mouseSlot.stack;
                        }

                        segment[index].stack -= half;
                        _mouseSlot = segment[index];
                        _mouseSlot.stack = half;

                        if (segment[index].stack == 0)
                        {
                            segment[index] = new Slot();
                        }
                        
                        // Update graphics

                        if (isStash) _stashSlots = segment;
                        else _hotSlots = segment;
                    }
                    
                    // if not, do nothing
                }
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

            var slotItem = segmentObjects[index].GetChild(0).gameObject;
            var img = slotItem.GetComponent<Image>();
            img.color = Color.white;
            img.sprite = copy.itemSo.sprite;
            img.SetNativeSize();

            var stackObject = segmentObjects[index].GetChild(1).gameObject;
            var text = stackObject.GetComponent<TextMeshProUGUI>();
            var stack = segmentSlots[index].stack;
            text.text = stack > 1 ? stack.ToString() : "";

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

        private static void SwapMouseSlot(int clickedIndex, bool isStash)
        {
            var segmentObjects = isStash ? _stashSlotObjects : _hotSlotObjects;
            var segmentSlots = isStash ? _stashSlots : _hotSlots;
            
            if (isStash)
            {
                (_mouseSlot, _stashSlots[clickedIndex]) = (_stashSlots[clickedIndex], _mouseSlot);
            }
            else
            {
                (_mouseSlot, _hotSlots[clickedIndex]) = (_hotSlots[clickedIndex], _mouseSlot);
            }
            
            // Update item graphics

            // Mouse slot item
            var itemImg = _mouseSlotObject.transform.GetChild(0).GetComponent<Image>();
            itemImg.sprite = _mouseSlot.item?.itemSo.sprite;
            itemImg.color = itemImg.sprite ? Color.white : Color.clear;
            itemImg.SetNativeSize();

            var itemStack = _mouseSlotObject.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            var stack = _mouseSlot.stack;
            itemStack.text = stack > 1 ? stack.ToString() : "";
            
            // Clicked slot item
            itemImg = segmentObjects[clickedIndex].GetChild(0).GetComponent<Image>();
            itemImg.sprite = segmentSlots[clickedIndex].item?.itemSo.sprite;
            itemImg.color = itemImg.sprite ? Color.white : Color.clear;
            itemImg.SetNativeSize();

            itemStack = segmentObjects[clickedIndex].GetChild(1).GetComponent<TextMeshProUGUI>();
            stack = segmentSlots[clickedIndex].stack;
            itemStack.text = stack > 1 ? stack.ToString() : "";
            
            // Update slot graphics
            var slotImg = segmentObjects[clickedIndex].GetComponent<Image>();
            if (stack > 0)
            {
                if (_selectedIndex == clickedIndex && !isStash)
                {
                    slotImg.sprite = instance.filledSlotSelectedSprite;
                }
                else if (isStash)
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
                if (_selectedIndex == clickedIndex && !isStash)
                {
                    slotImg.sprite = instance.emptySlotSelectedSprite;
                }
                else if (isStash)
                {
                    slotImg.sprite = instance.emptyStashSprite;
                }
                else
                {
                    slotImg.sprite = instance.emptySlotSprite;
                }
            }
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
        
        private static void OnSlotSelected(Item slotItem)
        {
            SlotSelected?.Invoke(slotItem);
        }
    }
}