using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Inventory.Inventory.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utilities;

namespace Inventory.Inventory
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
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private GameObject itemTooltipObject;
        [SerializeField] private GameObject equippedItemObject;
        [SerializeField] private Sprite emptySlotSprite;
        [SerializeField] private Sprite filledSlotSprite;
        [SerializeField] private Sprite emptySlotSelectedSprite;
        [SerializeField] private Sprite filledSlotSelectedSprite;
        [SerializeField] private Sprite emptyStashSprite;
        [SerializeField] private Sprite filledStashSprite;

        public static InventoryManager instance;

        private static GameObject _stashObject;
        private static GameObject _mouseSlotObject;
        private static Transform[] _hotSlotObjects;
        private static Transform[] _stashSlotObjects;
        private static Slot[] _hotSlots;
        private static Slot[] _stashSlots;
        private static Slot _mouseSlot;
        private static int _selectedIndex;
        private Item _equippedItem;

        public delegate void ItemEquippedHandler(Item item);
        public static event ItemEquippedHandler ItemEquipped;
        
        private void Start()
        {
            #region Initialize Inventory
            
            instance = this;
            
            if (!inventoryObject)
            {
                Debug.LogError("No inventory object set for inventory manager.");
            }

            if (!itemTooltipObject)
            {
                Debug.LogError("No item tooltip object set for inventory manager.");
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
            itemTooltipObject.SetActive(false);
            
            #endregion

            PlayerController.instance.ItemPickedUp += io => AddItem(io.GetComponent<ItemEntity>().item);
            UIUtilities.OnMouseRaycast += UpdateItemTooltip;
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
                _selectedIndex = i - 1;
            }
            
            // Check for inventory switch
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _stashObject.SetActive(!_stashObject.activeInHierarchy);
            }
            
            // Make mouse slot follow the cursor
            _mouseSlotObject.transform.position = Input.mousePosition;
            
            // Make the item tooltip object follor the cursor
            itemTooltipObject.transform.position = Input.mousePosition + (Vector3)(Vector2.one * 10f);
            
            if (Input.GetMouseButtonDown(0))
            {
                // Check if grabbing item, swapping item, putting down item or dropping a stack.
                var clickedSlotObject = GetHoveringSlotObject();
                GetSlotFromObject(clickedSlotObject, out var slot);

                HandleMouseOne(ref slot, clickedSlotObject);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                // Check if splitting stacks or dropping single items.
                var clickedSlotObject = GetHoveringSlotObject();
                GetSlotFromObject(clickedSlotObject, out var slot);
                
                HandleMouseTwo(ref slot, clickedSlotObject);
            }
        }

        private void HandleMouseOne(ref Slot clickedSlot, GameObject clickedSlotObject)
        {
            if (!clickedSlotObject)
            {
                if (_mouseSlot.stack <= 0) return;

                DropItems(_mouseSlot.stack);
                return;
            }

            // Check if both mouse slot and clicked slot have an item
            // check if they're the same
            // put the whole stack from mouse slot to clicked slot, if it fits
            if (_mouseSlot.stack > 0 && clickedSlot.stack > 0 &&
                clickedSlot.item.itemSo.id == _mouseSlot.item.itemSo.id &&
                clickedSlot.stack < clickedSlot.item.itemSo.maxStack)
            {
                var amount = _mouseSlot.stack;

                if (clickedSlot.stack + amount > clickedSlot.item.itemSo.maxStack)
                {
                    amount = clickedSlot.item.itemSo.maxStack - clickedSlot.stack;
                }

                clickedSlot.stack += amount;
                _mouseSlot.stack -= amount;

                if (_mouseSlot.stack == 0)
                {
                    _mouseSlot = new Slot();
                }
                        
                UpdateLogicalSlot(clickedSlot);
                UpdateSlotGraphics(ref clickedSlot, clickedSlotObject.transform);
                UpdateMouseSlotGraphics();

                return;
            }

            SwapMouseSlot(ref clickedSlot, clickedSlotObject);
        }
        
        private void HandleMouseTwo(ref Slot clickedSlot, GameObject clickedSlotObject)
        {
            // If mouse slot has an item...
            if (_mouseSlot.stack > 0)
            {
                if (!clickedSlotObject)
                {
                    // If mouse slot has items and right click hit nothing, drop 1 item
                    DropItems(1);
                    return;
                }
                        
                // check if clicked slot has items
                if (clickedSlot.stack > 0)
                {
                    // if so, check if it's the same as what is in the mouse slot
                    if (clickedSlot.item.itemSo.id == _mouseSlot.item.itemSo.id)
                    {
                        // if so, add 1 to the clicked slot
                        if (clickedSlot.stack >= clickedSlot.item.itemSo.maxStack) return;
                        
                        clickedSlot.stack++;
                        _mouseSlot.stack--;

                        if (_mouseSlot.stack == 0)
                        {
                            _mouseSlot = new Slot();
                        }
                            
                        UpdateLogicalSlot(clickedSlot);
                        UpdateSlotGraphics(ref clickedSlot, clickedSlotObject.transform);
                        UpdateMouseSlotGraphics();
                    }
                    else
                    {
                        // if the item is not the same, swap stacks
                        SwapMouseSlot(ref clickedSlot, clickedSlotObject);
                    }
                }
                else
                {
                    // if clicked slot has no item, add 1 to the empty slot
                    var index = clickedSlot.index;
                    clickedSlot = _mouseSlot;
                    clickedSlot.index = index;
                    clickedSlot.stack = 1;
                                
                    // Reduce 1 from the mouse slot
                    _mouseSlot.stack--;

                    if (_mouseSlot.stack == 0)
                    {
                        _mouseSlot = new Slot(-1);
                    }
                    
                    UpdateLogicalSlot(clickedSlot);
                    UpdateSlotGraphics(ref clickedSlot, clickedSlotObject.transform);
                    UpdateMouseSlotGraphics();
                }
            }
            else
            {
                // if mouse slot has no items, check if clicking on a slot
                if (!clickedSlotObject) return;
                if (clickedSlot.stack <= 0) return;
                
                // If clicked slot has items, grab half
                var half = Mathf.CeilToInt(clickedSlot.stack / 2f);

                clickedSlot.stack -= half;
                _mouseSlot = clickedSlot;
                _mouseSlot.stack = half;
                _mouseSlot.index = -1;

                if (clickedSlot.stack == 0)
                {
                    clickedSlot = new Slot(clickedSlot.index);

                    if (_selectedIndex == clickedSlot.index)
                    {
                        EquipItem(null);
                    }
                }
                
                UpdateLogicalSlot(clickedSlot);
                UpdateSlotGraphics(ref clickedSlot, clickedSlotObject.transform);
                UpdateMouseSlotGraphics();
            }
        }
        
        public bool AddItem(Item item)
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

        private void SwapMouseSlot(ref Slot slot, GameObject clickedSlotObject)
        {
            (_mouseSlot, slot) = (slot, _mouseSlot);
            (_mouseSlot.index, slot.index) = (slot.index, _mouseSlot.index);

            if (_selectedIndex == slot.index && slot.item == null)
            {
                EquipItem(null);
            }

            UpdateLogicalSlot(slot);
            UpdateSlotGraphics(ref slot, clickedSlotObject.transform);
            UpdateMouseSlotGraphics();
            
            // TODO: If swapping an item into a selected slot, equip it
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

        private bool SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > _hotSlots.Length - 1) return false;
            
            var img = _hotSlotObjects[slotIndex].gameObject.GetComponent<Image>();
            img.sprite = _hotSlots[slotIndex].stack > 0 ? instance.filledSlotSelectedSprite : instance.emptySlotSelectedSprite;
            
            EquipItem(_hotSlots[slotIndex].item);
            return true;
        }

        private static void DropItems(int count)
        {
            var droppedItem = _mouseSlot.item;
            _mouseSlot.stack -= _mouseSlot.stack - count < 0 ? _mouseSlot.stack : count;

            if (_mouseSlot.stack == 0)
            {
                _mouseSlot = new Slot(_mouseSlot.index);
            }
            
            // UpdateLogicalSlot(slot);
            UpdateMouseSlotGraphics();
            
            for (var i = 0; i < count; i++)
            {
                // TODO: Make it so you can drop stacks of the same item.
                // This would reduce the amount of items required to instantiate
                var item = Instantiate(instance.itemPrefab);
                item.GetComponent<ItemEntity>().item = droppedItem;
                var pos = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
                pos.z = 0;
                item.transform.position = pos;
            }
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

        private void UpdateItemTooltip(List<RaycastResult> elementsUnderMouse)
        {
            var slotObjectUnderMouse = GetHoveringSlotObject(elementsUnderMouse);
            GetSlotFromObject(slotObjectUnderMouse, out var slot);
            
            if (!slotObjectUnderMouse || slot.stack == 0 || !_stashObject.activeInHierarchy)
            {
                itemTooltipObject.SetActive(false);
                return;
            }
            
            itemTooltipObject.SetActive(true);

            var tr = itemTooltipObject.transform;
            
            // This is a bit shit but what can you do
            var nameText = tr.GetChild(0).GetComponent<TextMeshProUGUI>();
            nameText.text = slot.item.itemSo.name;

            var image = tr.GetChild(1).GetComponent<Image>();
            image.sprite = slot.item.itemSo.sprite;
            image.SetNativeSize();

            var typeText = tr.GetChild(2).GetComponent<TextMeshProUGUI>();
            var stats = tr.GetChild(3);
            var statNames = stats.GetChild(0);
            var statValues = stats.GetChild(1);

            var statName1 = statNames.GetChild(0).GetComponent<TextMeshProUGUI>();
            var statName2 = statNames.GetChild(1).GetComponent<TextMeshProUGUI>();
            var statName3 = statNames.GetChild(2).GetComponent<TextMeshProUGUI>();
            
            var statValue1 = statValues.GetChild(0).GetComponent<TextMeshProUGUI>();
            var statValue2 = statValues.GetChild(1).GetComponent<TextMeshProUGUI>();
            var statValue3 = statValues.GetChild(2).GetComponent<TextMeshProUGUI>();
            
            // TODO: Update tooltip UI elements in script. Unity's layout stuff is buggy as shit it seems.
            // TODO: Would be cool if you could choose which data in the scriptable object is shown in the tooltip.
            // switch (slot.item.itemSo)
            // {
            //     case ToolSo tool:
            //         typeText.text = "Tool";
            //         statName1.text = "Damage";
            //         statName2.text = "Use Speed";
            //         statName3.text = "Tool Power";
            //
            //         statValue1.text = tool.projectile.damage.ToString();
            //         statValue2.text = tool.attackCooldown.ToString();
            //         statValue3.text = tool.toolPower.ToString();
            //         break;
            //     
            //     case WeaponSo weapon:
            //         typeText.text = "Weapon";
            //         statName1.text = "Damage";
            //         statName2.text = "Attack Speed";
            //         statName3.text = "idk";
            //
            //         statValue1.text = weapon.projectile.damage.ToString();
            //         statValue2.text = weapon.attackCooldown.ToString();
            //         statValue3.text = "Something";
            //         break;
            //     
            //     default:
            //         typeText.text = "";
            //         statName1.text = "";
            //         statName2.text = "";
            //         statName3.text = "";
            //
            //         statValue1.text = "";
            //         statValue2.text = "";
            //         statValue3.text = "";
            //         break;
            // }
        }
        
        /// <summary>
        /// Get the GameObject of the UI element under the cursor where the GameObject name in lower case contains "slot". Returns null if not found.
        /// </summary>
        /// <returns>GameObject of UI slot or null</returns>
        private static GameObject GetHoveringSlotObject(List<RaycastResult> results = null)
        {
            results ??= UIUtilities.GetMouseRaycast();
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
        
        private void EquipItem(Item item)
        {
            _equippedItem = item;
            equippedItemObject.SetActive(item != null);

            if (item != null)
            {
                equippedItemObject.GetComponent<SpriteRenderer>().sprite = item.itemSo.sprite;
                OnItemEquipped(item);
            }
        }
        
        private static void OnItemEquipped(Item item)
        {
            ItemEquipped?.Invoke(item);
        }
    }
}