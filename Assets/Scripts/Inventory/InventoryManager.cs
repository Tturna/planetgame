using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cameras;
using Entities;
using Inventory.Crafting;
using Inventory.Entities;
using Inventory.Item_SOs;
using Inventory.Item_SOs.Accessories;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Utilities;

namespace Inventory
{
    [Serializable]
    public struct TooltipStatIcon
    {
        public string name;
        public Sprite sprite;
    }

    internal enum InventorySegmentType
    {
        Hotbar,
        Stash,
        Accessory,
        Mouse
    }

    public enum SuitableItemType
    {
        Any,
        Trinket,
        Helmet,
        Armor,
        Jetpack,
        Pet
    }
    
    public struct Slot
    {
        internal Item item;
        internal int stack;
        internal InventorySegmentType inventorySegmentType;
        internal SuitableItemType suitableItemType; 
        internal int index;

        internal Slot(int index, InventorySegmentType inventorySegmentType, SuitableItemType suitableItemType)
        {
            item = default;
            stack = default;
            this.inventorySegmentType = inventorySegmentType;
            this.suitableItemType = suitableItemType;
            this.index = index;
        }
    }

    public class InventoryManager : MonoBehaviour
    {
        [Header("Inventory Objects")]
        [FormerlySerializedAs("stashObject")] [SerializeField] private GameObject stashParent;
        [SerializeField] private GameObject hotslotsParent;
        [FormerlySerializedAs("accessoryObject")] [SerializeField] private GameObject accessoryParent;
        [SerializeField] private GameObject mouseSlotObject;
        [SerializeField] private RectTransform selectionOverlayRect;
        [SerializeField] private RectTransform selectionArrowRect;
        [SerializeField] private Image mouseSlotImage;
        [SerializeField] private TextMeshProUGUI mouseSlotStackText;
        [SerializeField] private GameObject pauseMenuButtonObject;
        
        [Header("Other Objects")]
        [SerializeField] private GameObject equippedItemObject;
        [SerializeField] private GameObject shipHudParent;

        [Header("Tooltip Objects")]
        [SerializeField] private RectTransform itemTooltipRect;
        [SerializeField] private TooltipStatIcon[] tooltipStatIcons;

        [SerializeField] private TextMeshProUGUI ttNameText;
        [SerializeField] private Image ttImage;
        [SerializeField] private TextMeshProUGUI ttTypeText;
        [SerializeField] private RectTransform ttStatsParentRect;
        [SerializeField] private Transform ttStatNamesParent;
        [SerializeField] private Transform ttStatValuesParent;
        [SerializeField] private Transform ttStatIconsParent;
        [SerializeField] private Transform ttStatNameTemplate;
        [SerializeField] private Transform ttStatValueTemplate;
        [SerializeField] private Transform ttStatIconTemplate;
        
        [Header("Inventory Prefabs")]
        [SerializeField] private GameObject itemPrefab;
        public GameObject breakablePrefab;
        public GameObject craftingStationPrefab;
        public GameObject[] roomModulePrefabs;
        
        [Header("Effects")]
        [SerializeField] private AudioSource effectAudioSource;
        [SerializeField] private AudioClip slotClickSound;
        [SerializeField] private AudioClip craftSound;
        
        [Header("Other")]
        [SerializeField] private ItemSo[] startingItems;
        [SerializeField] private RecipeSo[] startingRecipes;
        
        public static InventoryManager instance;
        public static bool isWearingJetpack;

        // Consider using a dictionary instead of arrays if the inventory is slow.
        private static Transform[] _hotSlotObjects;
        private static Transform[] _stashSlotObjects;
        private static Transform[] _accessorySlotObjects;
        private static Slot[] _hotSlots;
        private static Slot[] _stashSlots;
        private static Slot[] _accessorySlots;
        private static Slot _mouseSlot;
        private static int _selectedIndex;
        
        private const int MaxTooltipStats = 5;
        private KeyValuePair<GameObject, TextMeshProUGUI>[] _ttStatNames;
        private KeyValuePair<GameObject, TextMeshProUGUI>[] _ttStatValues;
        private KeyValuePair<GameObject, Image>[] _ttStatIcons;
        private GameObject _itemTooltipObject;
        private Dictionary<string, Sprite> _tooltipStatIcons;
        private bool shipHudTempClosed;

        public delegate void ItemEquippedHandler(Item item);
        public static event ItemEquippedHandler ItemEquipped;
        
        private void Start()
        {
            #region Initialize Inventory
            
            instance = this;

            // Reverse the order of graphical slots because they're in reverse order in the hierarchy
            _hotSlotObjects = hotslotsParent.transform.Cast<Transform>().Reverse().ToArray();
            _stashSlotObjects = stashParent.transform.Cast<Transform>().Reverse().ToArray();
            _accessorySlotObjects = accessoryParent.transform.Cast<Transform>().Reverse().ToArray();
            _hotSlots = new Slot[_hotSlotObjects.Length];
            _stashSlots = new Slot[_stashSlotObjects.Length];
            _accessorySlots = new Slot[_accessorySlotObjects.Length];
            _mouseSlot = new Slot(-1, InventorySegmentType.Mouse, SuitableItemType.Any);

            // Initialize values for slots
            for (var i = 0; i < _stashSlots.Length; i++)
            {
                if (i < _hotSlots.Length)
                {
                    _hotSlots[i].index = i;
                    _hotSlots[i].inventorySegmentType = InventorySegmentType.Hotbar;
                    _hotSlots[i].suitableItemType = SuitableItemType.Any;
                }
                
                if (i < _accessorySlots.Length)
                {
                    _accessorySlots[i].index = i;
                    _accessorySlots[i].inventorySegmentType = InventorySegmentType.Accessory;
                    
                    _accessorySlots[i].suitableItemType = i switch
                    {
                        5 => SuitableItemType.Helmet,
                        6 => SuitableItemType.Armor,
                        7 => SuitableItemType.Jetpack,
                        8 => SuitableItemType.Pet,
                        _ => SuitableItemType.Trinket
                    };
                }

                _stashSlots[i].index = i;
                _stashSlots[i].inventorySegmentType = InventorySegmentType.Stash;
                _stashSlots[i].suitableItemType = SuitableItemType.Any;
            }

            SelectSlot(_selectedIndex);
            stashParent.SetActive(false);
            accessoryParent.SetActive(false);
            pauseMenuButtonObject.SetActive(false);
            
            #endregion
            
            // Initialize item tooltip
            _itemTooltipObject = itemTooltipRect.gameObject;
            _ttStatNames = new KeyValuePair<GameObject, TextMeshProUGUI>[MaxTooltipStats];
            _ttStatValues = new KeyValuePair<GameObject, TextMeshProUGUI>[MaxTooltipStats];
            _ttStatIcons = new KeyValuePair<GameObject, Image>[MaxTooltipStats];

            for (var i = 0; i < MaxTooltipStats; i++)
            {
                var statName = Instantiate(ttStatNameTemplate, ttStatNamesParent);
                var statValue = Instantiate(ttStatValueTemplate, ttStatValuesParent);
                var statIcon = Instantiate(ttStatIconTemplate, ttStatIconsParent);

                _ttStatNames[i] = new KeyValuePair<GameObject, TextMeshProUGUI>(statName.gameObject, statName.GetComponent<TextMeshProUGUI>());
                _ttStatValues[i] = new KeyValuePair<GameObject, TextMeshProUGUI>(statValue.gameObject, statValue.GetComponent<TextMeshProUGUI>());
                _ttStatIcons[i] = new KeyValuePair<GameObject, Image>(statIcon.gameObject, statIcon.GetComponent<Image>());
            }
            
            _tooltipStatIcons = new Dictionary<string, Sprite>();
            foreach (var statIcon in tooltipStatIcons)
            {
                _tooltipStatIcons.Add(statIcon.name, statIcon.sprite);
            }
            
            _itemTooltipObject.SetActive(false);
            

            PlayerController.instance.itemPickedUp += io => AddItem(io.GetComponent<ItemEntity>().item);
            UIUtilities.OnMouseRaycast += UpdateItemTooltip;
            HeldItemManager.ItemUsed += TryDecrementSelectedStack;
            
            foreach (var item in startingItems)
            {
                AddItem(new Item(item));
            }
        }

        private void OnDestroy()
        {
            PlayerController.instance.itemPickedUp -= io => AddItem(io.GetComponent<ItemEntity>().item);
            UIUtilities.OnMouseRaycast -= UpdateItemTooltip;
            HeldItemManager.ItemUsed -= TryDecrementSelectedStack;
        }

        private float _scrollDelta;
        private void Update()
        {
            // Check controls
            _scrollDelta = Input.mouseScrollDelta.y;

            if (_scrollDelta != 0)
            {
                if (SelectSlot(_selectedIndex - (int)_scrollDelta))
                {
                    _selectedIndex -= (int)_scrollDelta;
                }
            }

            // Check for number keys
            for (var i = 1; i <= 10; i++)
            {
                var numkey = i == 10 ? "0" : i.ToString();
                if (!Input.GetKeyDown(numkey)) continue;
                SelectSlot(i - 1);
                _selectedIndex = i - 1;
            }
            
            // Check for inventory switch
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                var state = !stashParent.activeInHierarchy;
                stashParent.SetActive(state);
                accessoryParent.SetActive(state);
                pauseMenuButtonObject.SetActive(state);

                if (shipHudParent.activeSelf)
                {
                    shipHudParent.SetActive(false);
                    shipHudTempClosed = true;
                }
                else if (shipHudTempClosed)
                {
                    shipHudParent.SetActive(true);
                    shipHudTempClosed = false;
                }

                if (state)
                {
                    if (!CraftingManager.IsCraftingMenuOpen())
                    {
                        CraftingManager.ToggleCraftingMenu(startingRecipes);
                    }
                }
                else
                {
                    CraftingManager.ToggleCraftingMenu(false);
                }
            }
            
            // Make mouse slot follow the cursor
            mouseSlotObject.transform.position = Input.mousePosition;
            
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
            
            // Update accessory behavior
            for (var i = 0; i < _accessorySlots.Length; i++)
            {
                if (_accessorySlots[i].stack <= 0) continue;
                var accessorySo = (BasicAccessorySo)_accessorySlots[i].item.itemSo;
                accessorySo.UpdateProcess();
            }
        }

        private void LateUpdate()
        {
            // Make the item tooltip object follor the cursor
            _itemTooltipObject.transform.position = Input.mousePosition + (Vector3)(Vector2.one * 10f);
        }

        private void HandleMouseOne(ref Slot clickedSlot, GameObject clickedSlotObject)
        {
            if (!clickedSlotObject)
            {
                if (_mouseSlot.stack <= 0) return;

                DropItems(_mouseSlot.stack);
                return;
            }
            
            if (!stashParent.activeInHierarchy) return;

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
                    // var index = clickedSlot.index;
                    // clickedSlot = _mouseSlot;
                    // clickedSlot.index = index;
                    clickedSlot.item = _mouseSlot.item;
                    clickedSlot.stack = 1;
                                
                    // Reduce 1 from the mouse slot
                    _mouseSlot.stack--;

                    if (_mouseSlot.stack == 0)
                    {
                        _mouseSlot = new Slot(-1, InventorySegmentType.Mouse, SuitableItemType.Any);
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
                    clickedSlot = new Slot(clickedSlot.index, clickedSlot.inventorySegmentType, clickedSlot.suitableItemType);

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
        
        private static Slot[] GetInventorySegment(InventorySegmentType inventorySegmentType)
        {
            return inventorySegmentType switch
            {
                InventorySegmentType.Hotbar => _hotSlots,
                InventorySegmentType.Stash => _stashSlots,
                InventorySegmentType.Accessory => _accessorySlots,
                _ => throw new ArgumentOutOfRangeException(nameof(inventorySegmentType), inventorySegmentType, null)
            };
        }
        
        private static Transform[] GetInventorySegmentObjects(InventorySegmentType inventorySegmentType)
        {
            return inventorySegmentType switch
            {
                InventorySegmentType.Hotbar => _hotSlotObjects,
                InventorySegmentType.Stash => _stashSlotObjects,
                InventorySegmentType.Accessory => _accessorySlotObjects,
                _ => throw new ArgumentOutOfRangeException(nameof(inventorySegmentType), inventorySegmentType, null)
            };
        }
        
        private static void UpdateInventorySegment(Slot[] segmentData, InventorySegmentType segmentType)
        {
            switch (segmentType)
            {
                case InventorySegmentType.Hotbar:
                    _hotSlots = segmentData;
                    break;
                case InventorySegmentType.Stash:
                    _stashSlots = segmentData;
                    break;
                case InventorySegmentType.Accessory:
                    _accessorySlots = segmentData;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(segmentType), segmentType, null);
            }
        }

        private static void EquipAccessory(BasicAccessorySo accessorySo)
        {
            PlayerStatsManager.AddStatModifiers(accessorySo.statModifiers, accessorySo.id);
            
            if (accessorySo.suitableSlotItemType == SuitableItemType.Jetpack)
            {
                PlayerController.instance.SetJetpackSprite(accessorySo.sprite);
                isWearingJetpack = true;
            }
        }
        
        private static void UnequipAccessory(BasicAccessorySo accessorySo)
        {
            PlayerStatsManager.RemoveStatModifiers(accessorySo.id);
            
            if (accessorySo.suitableSlotItemType == SuitableItemType.Jetpack)
            {
                PlayerController.instance.SetJetpackSprite(null);
                isWearingJetpack = false;
            }
        }

        private void AddItem(Item item)
        {
            // Copy item
            var copy = new Item(item);
            
            // Check if inventory has space
            if (!HasSpaceForItem(copy, out var index, out var availableSlotType)) return;

            // Create temporary variables to make inventory management easier with the separate hotbar and stash
            var segmentSlots = GetInventorySegment(availableSlotType);
            var segmentObjects = GetInventorySegmentObjects(availableSlotType);
            
            // If slot was empty, change the sprite
            if (segmentSlots[index].stack == 0)
            {
                // segmentObjects[index].gameObject.GetComponent<Image>().sprite = instance.filledSlotSprite;
            }
            
            // Add item to slot with space
            segmentSlots = AddToStack(segmentSlots, index, copy);
            UpdateSlotGraphics(ref segmentSlots[index], segmentObjects[index]);
            
            UpdateInventorySegment(segmentSlots, availableSlotType);
            
            // Update selected slot
            if (index == _selectedIndex) SelectSlot(_selectedIndex);
        }

        private void SwapMouseSlot(ref Slot slot, GameObject clickedSlotObject)
        {
            if (_mouseSlot.stack > 0)
            {
                if (slot.suitableItemType != SuitableItemType.Any)
                {
                    if (slot.suitableItemType != _mouseSlot.item.itemSo.suitableSlotItemType) return;
                }
            }
            else if (slot.stack == 0) return;

            // (_mouseSlot, slot) = (slot, _mouseSlot);
            // (_mouseSlot.index, slot.index) = (slot.index, _mouseSlot.index);
            (_mouseSlot.item, slot.item) = (slot.item, _mouseSlot.item);
            (_mouseSlot.stack, slot.stack) = (slot.stack, _mouseSlot.stack);
            
            if (slot.inventorySegmentType == InventorySegmentType.Hotbar && _selectedIndex == slot.index)
            {
                EquipItem(slot.item);
            }

            UpdateLogicalSlot(slot);
            UpdateSlotGraphics(ref slot, clickedSlotObject.transform);
            UpdateMouseSlotGraphics();
            
            // ReSharper disable once MergeIntoPattern
            if (slot.stack > 0 && slot.inventorySegmentType == InventorySegmentType.Accessory)
            {
                var accessorySo = (BasicAccessorySo)slot.item.itemSo;
                EquipAccessory(accessorySo);
                accessorySo.ResetBehavior();
            }
            
            // ReSharper disable once InvertIf
            if (_mouseSlot.stack > 0 && slot.inventorySegmentType == InventorySegmentType.Accessory)
            {
                var accessorySo = (BasicAccessorySo)_mouseSlot.item.itemSo;
                UnequipAccessory(accessorySo);
                accessorySo.ResetBehavior();
            }
            
            effectAudioSource.PlayOneShot(slotClickSound);
        }

        private static bool HasSpaceForItem(Item item, out int availableIndex, out InventorySegmentType availableInventorySegmentType)
        {
            bool IsEmptySlotSuitable(int availableIndex, Slot slot)
            {
                if (availableIndex != -1) return false;
                if (slot.stack != 0) return false;
                
                if (slot.suitableItemType == SuitableItemType.Any) return true;
                
                return slot.suitableItemType == item.itemSo.suitableSlotItemType;
            }
            
            int FindIndex(Slot[] slotSegment)
            {
                var availableIndex = -1;
                for (var i = 0; i < slotSegment.Length; i++)
                {
                    // This will find the first empty slot in the slot segment
                    if (IsEmptySlotSuitable(availableIndex, slotSegment[i]))
                    {
                        availableIndex = i;
                    }
                    // This will find the first available stack
                    // This is prioritized over an empty slot
                    else if (slotSegment[i].item?.itemSo.id == item.itemSo.id)
                    {
                        if (slotSegment[i].stack >= item.itemSo.maxStack) continue;
                        availableIndex = slotSegment[i].index;
                        break;
                    }
                }

                return availableIndex;
            }

            availableInventorySegmentType = InventorySegmentType.Hotbar;
            availableIndex = FindIndex(_hotSlots);

            if (availableIndex > -1) return true;
            
            availableInventorySegmentType = InventorySegmentType.Stash;
            availableIndex = FindIndex(_stashSlots);

            if (availableIndex > -1) return true;
            
            availableInventorySegmentType = InventorySegmentType.Accessory;
            availableIndex = FindIndex(_accessorySlots);
            
            return availableIndex != -1;
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

            var nextrow = slotIndex > 4;
            var x = 22f * (slotIndex % 5);
            if (nextrow) x += 11f;
            var y = nextrow ? -26f : -7f;
            selectionOverlayRect.anchoredPosition = new Vector2(x, y);

            x += nextrow ? 9f : 16f;
            y = nextrow ? -59f : -1f;
            selectionArrowRect.anchoredPosition = new Vector2(x, y);
            selectionArrowRect.eulerAngles = Vector3.forward * (nextrow ? 90f : -90f);
            
            EquipItem(_hotSlots[slotIndex].item);
            return true;
        }

        private static void DropItems(int count)
        {
            var droppedItem = _mouseSlot.item;
            _mouseSlot.stack -= _mouseSlot.stack - count < 0 ? _mouseSlot.stack : count;

            if (_mouseSlot.stack == 0)
            {
                _mouseSlot = new Slot(_mouseSlot.index, _mouseSlot.inventorySegmentType, _mouseSlot.suitableItemType);
            }
            
            // UpdateLogicalSlot(slot);
            UpdateMouseSlotGraphics();
            
            for (var i = 0; i < count; i++)
            {
                // TODO: Make it so you can drop stacks of the same item.
                // This would reduce the amount of items required to instantiate
                var pos = CameraController.instance.mainCam.ScreenToWorldPoint(Input.mousePosition);
                pos.z = 0;
                SpawnItem(droppedItem, pos);
            }
        }
        
        /// <summary>
        /// Returns how much the given amount was over the stack amount of the slot
        /// </summary>
        /// <param name="index"></param>
        /// <param name="hotbar"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        private static int DecrementStack(int index, bool hotbar, int amount = 1)
        {
            var segmentSlots = hotbar ? _hotSlots : _stashSlots;
            var segmentObjects = hotbar ? _hotSlotObjects : _stashSlotObjects;
            var slot = segmentSlots[index];
            var excess = Mathf.Clamp(amount - slot.stack, 0, amount);
            
            if (slot.stack <= 0) return excess;
            
            slot.stack -= amount - excess;
            
            if (slot.stack == 0)
            {
                slot = new Slot(slot.index, slot.inventorySegmentType, slot.suitableItemType);
                // segmentObjects[index].gameObject.GetComponent<Image>().sprite = instance.emptySlotSprite;
            }
            
            UpdateSlotGraphics(ref slot, segmentObjects[index]);
            UpdateLogicalSlot(slot);
            return excess;
        }
        
        private static void UpdateSlotGraphics(ref Slot slot, Transform slotObject)
        {
            // var segmentObjects = isStash ? _stashSlotObjects : _hotSlotObjects;
            // var segmentSlots = isStash ? _stashSlots : _hotSlots;
            
            var itemImg = slotObject.GetChild(0).GetComponent<Image>();
            itemImg.sprite = slot.item?.itemSo.sprite;
            itemImg.color = itemImg.sprite ? Color.white : Color.clear;
            itemImg.SetNativeSize();
            
            // 2023-8-4:
            // If the image has an even width, offset it by 0.5 pixels to fit the odd pixel width of the slot
            if (itemImg.sprite && itemImg.sprite.texture.width % 2 == 0)
            {
                ((RectTransform)itemImg.transform).anchoredPosition = Vector2.right * 0.5f;
            }

            var itemStack = slotObject.GetChild(1).GetComponent<TextMeshProUGUI>();
            var stack = slot.stack;
            itemStack.text = stack > 1 ? stack.ToString() : "";
            
            // Disable accessory slot icons if there's an item in the slot
            if (slot.inventorySegmentType != InventorySegmentType.Accessory) return;
            var accessoryTypeIcon = slotObject.GetChild(2).gameObject;
            accessoryTypeIcon.SetActive(slot.item == null);
        }

        private static void UpdateMouseSlotGraphics()
        {
            var itemImg = instance.mouseSlotImage;
            itemImg.sprite = _mouseSlot.item?.itemSo.sprite;
            itemImg.color = itemImg.sprite ? Color.white : Color.clear;
            itemImg.SetNativeSize();

            var itemStack = instance.mouseSlotStackText;
            var stack = _mouseSlot.stack;
            itemStack.text = stack > 1 ? stack.ToString() : "";
        }

        private static void UpdateLogicalSlot(Slot slot)
        {
            // Update inventory
            switch (slot.inventorySegmentType)
            {
                case InventorySegmentType.Stash:
                {
                    _stashSlots[slot.index] = slot;
                    break;
                }
                case InventorySegmentType.Hotbar:
                {
                    _hotSlots[slot.index] = slot;

                    if (slot.index == _selectedIndex)
                    {
                        instance.EquipItem(slot.item);
                    }
                    break;
                }
                case InventorySegmentType.Accessory:
                {
                    _accessorySlots[slot.index] = slot;
                    break;
                }
                case InventorySegmentType.Mouse:
                {
                    _mouseSlot = slot;
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        private void UpdateItemTooltip(List<RaycastResult> elementsUnderMouse)
        {
            // TODO: Maybe make this function not run every frame that the mouse is over a slot
            
            var slotObjectUnderMouse = GetHoveringSlotObject(elementsUnderMouse);
            GetSlotFromObject(slotObjectUnderMouse, out var slot);
            
            if (!slotObjectUnderMouse || slot.stack == 0 || !stashParent.activeInHierarchy)
            {
                _itemTooltipObject.SetActive(false);
                return;
            }
            
            _itemTooltipObject.SetActive(true);

            ttNameText.text = slot.item.itemSo.name.ToUpper();
            ttImage.sprite = slot.item.itemSo.sprite;
            ttImage.SetNativeSize();

            void UpdateTooltip(IReadOnlyList<string> statNames, IReadOnlyList<string> statValues, int statCount)
            {
                var showAdvanced = Input.GetKey(KeyCode.LeftAlt);
                float maxWidth = 0;
                for (var i = 0; i < MaxTooltipStats; i++)
                {
                    if (statCount == 0)
                    {
                        _ttStatNames[i].Key.SetActive(false);
                        _ttStatValues[i].Key.SetActive(false);
                        _ttStatIcons[i].Key.SetActive(false);
                        continue;
                    }
                    
                    _ttStatNames[i].Key.SetActive(showAdvanced && i < statCount);
                    _ttStatValues[i].Key.SetActive(i < statCount);
                    _ttStatIcons[i].Key.SetActive(i < statCount);
                    _ttStatNames[i].Value.text = i < statCount ? statNames[i] : "";
                    _ttStatValues[i].Value.text = i < statCount ? statValues[i] : "";
                    _ttStatIcons[i].Value.sprite = i < statCount ? _tooltipStatIcons[statNames[i]] : null;
                    
                    var nameRect = (RectTransform)_ttStatNames[i].Key.transform;
                    var valueRect = (RectTransform)_ttStatValues[i].Key.transform;
                    var iconRect = (RectTransform)_ttStatIcons[i].Key.transform;
                    nameRect.anchoredPosition = Vector2.down * (8f * i);
                    valueRect.anchoredPosition = Vector2.down * (8f * i);
                    iconRect.anchoredPosition = Vector2.down * (8f * i);

                    var descriptorWidth = showAdvanced ? _ttStatNames[i].Value.renderedWidth + 10f : 5f; // 5 is the width of the icon
                    var comparedWidth = descriptorWidth + _ttStatValues[i].Value.renderedWidth + 6f;
                    maxWidth = Mathf.Max(maxWidth, comparedWidth);
                }
                
                var tooltipWidth = Mathf.Max(Mathf.Max(ttNameText.preferredWidth + 9f, maxWidth), 16f);
                itemTooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tooltipWidth);
                itemTooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 14f + statCount * 8f);
                ttStatsParentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tooltipWidth - 2f);
            }
            
            // TODO: Would be cool if you could choose which data in the scriptable object is shown in the tooltip.
            // Custom editor script?
            switch (slot.item.itemSo)
            {
                case ToolSo tool:
                    ttTypeText.text = "TOOL";

                    // TODO: Maybe make stat names into en enum
                    string[] statNames = { "Damage", "Use Time", "Tool Power" };
                    string[] statValues =
                    {
                        tool.projectile.damage.ToString(CultureInfo.InvariantCulture).ToUpper(),
                        tool.attackCooldown.ToString(CultureInfo.InvariantCulture).ToUpper(),
                        tool.toolPower.ToString(CultureInfo.InvariantCulture).ToUpper()
                    };

                    UpdateTooltip(statNames, statValues, 3);
                    break;
                
                case MeleeSo melee:
                    ttTypeText.text = "MELEE";
                    
                    statNames = new[] { "Melee Damage", "Knockback", "Use Time" };
                    statValues = new[]
                    {
                        melee.damage.ToString(CultureInfo.InvariantCulture).ToUpper(),
                        melee.knockback.ToString(CultureInfo.InvariantCulture).ToUpper(),
                        melee.attackCooldown.ToString(CultureInfo.InvariantCulture).ToUpper()
                    };
                    
                    UpdateTooltip(statNames, statValues, 3);
                    break;
                
                case WeaponSo weapon:
                    ttTypeText.text = "WEAPON";
            
                    statNames = new[] { "Damage", "Knockback", "Use Time" };
                    statValues = new[]
                    {
                        weapon.projectile.damage.ToString(CultureInfo.InvariantCulture).ToUpper(),
                        weapon.projectile.knockback.ToString(CultureInfo.InvariantCulture).ToUpper(),
                        weapon.attackCooldown.ToString(CultureInfo.InvariantCulture).ToUpper()
                    };

                    UpdateTooltip(statNames, statValues, 3);
                    break;
                
                default:
                    ttTypeText.text = "";
                    UpdateTooltip(null, null, 0);
                    break;
            }
        }
        
        /// <summary>
        /// Get the GameObject of the UI element under the cursor where the GameObject name in lower case contains "slot". Returns null if not found.
        /// </summary>
        /// <returns>GameObject of UI slot or null</returns>
        private static GameObject GetHoveringSlotObject(List<RaycastResult> results = null)
        {
            results ??= UIUtilities.GetMouseRaycast();
            return results.Find(x => x.gameObject.CompareTag("InventorySlot")).gameObject;
        }

        private static void GetSlotFromObject(GameObject slotObject, out Slot slot)
        {
            if (!slotObject)
            {
                slot = new Slot();
                return;
            }

            var slotParent = slotObject.transform.parent.gameObject;
            var objectSlotType = slotParent switch
            {
                _ when slotParent == instance.stashParent => InventorySegmentType.Stash,
                _ when slotParent == instance.accessoryParent => InventorySegmentType.Accessory,
                _ => InventorySegmentType.Hotbar
            };

            var segment = GetInventorySegment(objectSlotType);
            
            var slotIndex = objectSlotType switch
            {
                InventorySegmentType.Hotbar => Array.FindIndex(_hotSlotObjects, x => x == slotObject.transform),
                InventorySegmentType.Stash => Array.FindIndex(_stashSlotObjects, x => x == slotObject.transform),
                InventorySegmentType.Accessory => Array.FindIndex(_accessorySlotObjects, x => x == slotObject.transform),
                _ => throw new ArgumentOutOfRangeException(nameof(objectSlotType), objectSlotType, null)
            };

            slot = segment[slotIndex];
        }
        
        private void EquipItem(Item item)
        {
            // _equippedItem = item;
            equippedItemObject.SetActive(item != null);

            if (item != null)
            {
                equippedItemObject.GetComponent<SpriteRenderer>().sprite = item.itemSo.sprite;
            }
            OnItemEquipped(item);
        }
        
        private static void OnItemEquipped(Item item)
        {
            ItemEquipped?.Invoke(item);
        }

        private static void TryDecrementSelectedStack(Item item)
        {
            var usableItem = (UsableItemSo)item.itemSo;

            if (usableItem.incrementStackOnUse)
            {
                DecrementStack(_selectedIndex, true);
            }
        }

        public static void SpawnItem(Item item, Vector3 position)
        {
            var itemEntity = Instantiate(instance.itemPrefab);
            itemEntity.GetComponent<ItemEntity>().item = item;
            itemEntity.transform.position = position;
        }

        private static bool TryGetSlotsWithIngredients(RecipeSo recipe,
            out Dictionary<CraftingResource, List<Slot>> hotIngredientsDict,
            out Dictionary<CraftingResource, List<Slot>> stashIngredientsDict)
        {
            hotIngredientsDict = new Dictionary<CraftingResource, List<Slot>>();
            stashIngredientsDict = new Dictionary<CraftingResource, List<Slot>>();
            
            foreach (var hotSlot in _hotSlots)
            {
                var ingredient = recipe.ingredients.ToList().Find(i => i.item.id == hotSlot.item?.itemSo.id);
                if (ingredient.amount == 0) continue;
                
                if (hotIngredientsDict.ContainsKey(ingredient))
                {
                    hotIngredientsDict[ingredient].Add(hotSlot);
                }
                else
                {
                    hotIngredientsDict.Add(ingredient, new List<Slot> { hotSlot });
                }
            }
            
            foreach (var stashSlot in _stashSlots)
            {
                var ingredient = recipe.ingredients.ToList().Find(i => i.item.id == stashSlot.item?.itemSo.id);
                if (ingredient.amount == 0) continue;
                
                if (stashIngredientsDict.ContainsKey(ingredient))
                {
                    stashIngredientsDict[ingredient].Add(stashSlot);
                }
                else
                {
                    stashIngredientsDict.Add(ingredient, new List<Slot> { stashSlot });
                }
            }
            
            var foundKeyCount = hotIngredientsDict.Count + stashIngredientsDict.Count;
            // Check if all ingredients are found
            if (foundKeyCount < recipe.ingredients.Length) return false;

            // The rest of this checks if enough ingredients are found
            var combinedIngredientSlots = new Dictionary<CraftingResource, List<Slot>>();

            // deep copy hotIngredients
            foreach (var (ingredient, slots) in hotIngredientsDict)
            {
                combinedIngredientSlots.Add(ingredient, new List<Slot>(slots));
            }
            
            foreach (var (ingredient, slots) in stashIngredientsDict)
            {
                if (!combinedIngredientSlots.TryAdd(ingredient, slots))
                {
                    combinedIngredientSlots[ingredient].AddRange(slots);
                }
            }
            
            foreach (var (ingredient, slots) in combinedIngredientSlots)
            {
                var requiredAmount = ingredient.amount;
                var totalAmount = slots.Sum(s => s.stack);
                
                if (totalAmount < requiredAmount) return false;
            }

            return true;
        }

        public static bool CanCraft(RecipeSo recipe)
        {
            return TryGetSlotsWithIngredients(recipe, out _, out _);
        }

        public void Craft(RecipeSo recipe)
        {
            if (!TryGetSlotsWithIngredients(
                recipe,
                out var hotIngredientsDict,
                out var stashIngredientsDict))
            {
                return;
            }

            foreach (var ingredient in recipe.ingredients)
            {
                var decrementAmount = ingredient.amount;
                List<Slot> ingredientSlots;
                
                if (hotIngredientsDict.TryGetValue(ingredient, out var slots))
                {
                    ingredientSlots = slots;

                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var slot in ingredientSlots)
                    {
                        var excess = DecrementStack(slot.index, true, decrementAmount);
                        decrementAmount = excess;
                        if (decrementAmount == 0) break;
                    }
                }
                
                if (decrementAmount == 0) continue;
                
                ingredientSlots = stashIngredientsDict[ingredient];
                
                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var slot in ingredientSlots)
                {
                    var excess = DecrementStack(slot.index, false, decrementAmount);
                    decrementAmount = excess;
                    if (decrementAmount == 0) break;
                }
                
                effectAudioSource.PlayOneShot(craftSound);
            }

            // TODO: Make sure the ingredients aren't decremented if there is no space for the result
            instance.AddItem(new Item(recipe.results[0].item));
        }
    }
}