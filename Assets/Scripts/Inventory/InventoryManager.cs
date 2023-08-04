using System;
using System.Collections.Generic;
using System.Linq;
using Entities.Entities;
using Inventory.Inventory.Entities;
using Inventory.Inventory.Item_Types;
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
    
    [Serializable]
    public struct TooltipStatIcon
    {
        public string name;
        public Sprite sprite;
    }

    public class InventoryManager : MonoBehaviour
    {
        [Header("Inventory Objects")]
        [SerializeField] private GameObject stashObject;
        [SerializeField] private GameObject hotslotsParent;
        [SerializeField] private GameObject accessoryObject;
        [SerializeField] private GameObject mouseSlotObject;
        [SerializeField] private RectTransform selectionOverlayRect;
        [SerializeField] private RectTransform selectionArrowRect;
        [SerializeField] private Image mouseSlotImage;
        [SerializeField] private TextMeshProUGUI mouseSlotStackText;
        [SerializeField] private GameObject pauseMenuButtonObject;
        
        [Header("Other Objects")]
        [SerializeField] private GameObject equippedItemObject;

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
        
        // To be removed probably
        [Header("Inventory Sprites")]
        [SerializeField] private Sprite emptySlotSprite;
        [SerializeField] private Sprite filledSlotSprite;
        [SerializeField] private Sprite emptySlotSelectedSprite;
        [SerializeField] private Sprite filledSlotSelectedSprite;
        [SerializeField] private Sprite emptyStashSprite;
        [SerializeField] private Sprite filledStashSprite;

        public static InventoryManager instance;

        private static Transform[] _hotSlotObjects;
        private static Transform[] _stashSlotObjects;
        private static Slot[] _hotSlots;
        private static Slot[] _stashSlots;
        private static Slot _mouseSlot;
        private static int _selectedIndex;
        
        private const int MaxTooltipStats = 5;
        private KeyValuePair<GameObject, TextMeshProUGUI>[] _ttStatNames;
        private KeyValuePair<GameObject, TextMeshProUGUI>[] _ttStatValues;
        private KeyValuePair<GameObject, Image>[] _ttStatIcons;
        private GameObject _itemTooltipObject;
        private Dictionary<string, Sprite> _tooltipStatIcons;

        public delegate void ItemEquippedHandler(Item item);
        public static event ItemEquippedHandler ItemEquipped;
        
        private void Start()
        {
            #region Initialize Inventory
            
            instance = this;

            // Reverse the order of graphical slots because they're in reverse order in the hierarchy
            _hotSlotObjects = hotslotsParent.transform.Cast<Transform>().Reverse().ToArray();
            _stashSlotObjects = stashObject.transform.Cast<Transform>().Reverse().ToArray();
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
            stashObject.SetActive(false);
            accessoryObject.SetActive(false);
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
            

            PlayerController.instance.ItemPickedUp += io => AddItem(io.GetComponent<ItemEntity>().item);
            UIUtilities.OnMouseRaycast += UpdateItemTooltip;
            HeldItemManager.ItemUsed += TryDecrementSelectedStack;
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
                    DeselectSlot(_selectedIndex);
                    _selectedIndex -= (int)_scrollDelta;
                }
            }

            // Check for number keys
            for (var i = 1; i <= 10; i++)
            {
                var numkey = i == 10 ? "0" : i.ToString();
                if (!Input.GetKeyDown(numkey)) continue;
                DeselectSlot(_selectedIndex);
                SelectSlot(i - 1);
                _selectedIndex = i - 1;
            }
            
            // Check for inventory switch
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                stashObject.SetActive(!stashObject.activeInHierarchy);
                accessoryObject.SetActive(!accessoryObject.activeInHierarchy);
                pauseMenuButtonObject.SetActive(!pauseMenuButtonObject.activeInHierarchy);
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

        private void AddItem(Item item)
        {
            // Copy item
            var copy = new Item(item);
            
            // Check if inventory has space
            if (!HasSpaceForItem(copy, out var index, out var isStash)) return;

            // Create temporary variables to make inventory management easier with the separate hotbar and stash
            var segmentSlots = isStash ? _stashSlots : _hotSlots;
            var segmentObjects = isStash ? _stashSlotObjects : _hotSlotObjects;
            
            // If slot was empty, change the sprite
            if (segmentSlots[index].stack == 0)
            {
                // segmentObjects[index].gameObject.GetComponent<Image>().sprite = instance.filledSlotSprite;
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
        }

        private void SwapMouseSlot(ref Slot slot, GameObject clickedSlotObject)
        {
            // (_mouseSlot, slot) = (slot, _mouseSlot);
            // (_mouseSlot.index, slot.index) = (slot.index, _mouseSlot.index);
            (_mouseSlot.item, slot.item) = (slot.item, _mouseSlot.item);
            (_mouseSlot.stack, slot.stack) = (slot.stack, _mouseSlot.stack);
            
            if (_selectedIndex == slot.index)
            {
                EquipItem(slot.item);
            }

            UpdateLogicalSlot(slot);
            UpdateSlotGraphics(ref slot, clickedSlotObject.transform);
            UpdateMouseSlotGraphics();
            
            // TODO: If swapping an item into a selected slot, equip it
        }

        private static bool HasSpaceForItem(Item item, out int availableIndex, out bool isStash)
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
                        availableIndex = slotSegment[i].index;
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

            var nextrow = slotIndex > 4;
            var x = 16f * (slotIndex % 5);
            if (nextrow) x += 8f;
            var y = nextrow ? -14f : 0f;
            selectionOverlayRect.anchoredPosition = new Vector2(x, y);

            x += 8.5f;
            y = nextrow ? -26.5f : -5.5f;
            selectionArrowRect.anchoredPosition = new Vector2(x, y);
            selectionArrowRect.eulerAngles = Vector3.forward * (nextrow ? 180f : 0f);
            
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
                var pos = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
                pos.z = 0;
                SpawnItem(droppedItem, pos);
            }
        }
        
        private static void DecrementStack(int index, bool hotbar, int amount = 1)
        {
            var segmentSlots = hotbar ? _hotSlots : _stashSlots;
            var segmentObjects = hotbar ? _hotSlotObjects : _stashSlotObjects;
            var slot = segmentSlots[index];
            
            if (slot.stack <= 0) return;
            
            slot.stack -= amount;
            if (slot.stack == 0)
            {
                slot = new Slot(slot.index);
                // segmentObjects[index].gameObject.GetComponent<Image>().sprite = instance.emptySlotSprite;
            }
            
            UpdateSlotGraphics(ref slot, segmentObjects[index]);
            UpdateLogicalSlot(slot);
        }
        
        private static void DeselectSlot(int slotIndex)
        {
            // var img = _hotSlotObjects[slotIndex].GetComponent<Image>();
            // img.sprite = _hotSlots[slotIndex].stack > 0 ? instance.filledSlotSprite : instance.emptySlotSprite;
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
            
            // 2023-8-4:
            // If the image has an even width, offset it by 0.5 pixels to fit the odd pixel width of the slot
            if (itemImg.sprite && itemImg.sprite.texture.width % 2 == 0)
            {
                ((RectTransform)itemImg.transform).anchoredPosition = Vector2.right * 0.5f;
            }

            var itemStack = slotObject.GetChild(1).GetComponent<TextMeshProUGUI>();
            var stack = slot.stack;
            itemStack.text = stack > 1 ? stack.ToString() : "";
            
            // Update slot graphics
            // var slotImg = slotObject.GetComponent<Image>();
            // if (stack > 0)
            // {
            //     if (_selectedIndex == slot.index && !slot.isInStash)
            //     {
            //         slotImg.sprite = instance.filledSlotSelectedSprite;
            //     }
            //     else if (slot.isInStash)
            //     {
            //         slotImg.sprite = instance.filledStashSprite;
            //     }
            //     else
            //     {
            //         slotImg.sprite = instance.filledSlotSprite;
            //     }
            // }
            // else
            // {
            //     if (_selectedIndex == slot.index && !slot.isInStash)
            //     {
            //         slotImg.sprite = instance.emptySlotSelectedSprite;
            //     }
            //     else if (slot.isInStash)
            //     {
            //         slotImg.sprite = instance.emptyStashSprite;
            //     }
            //     else
            //     {
            //         slotImg.sprite = instance.emptySlotSprite;
            //     }
            // }
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
            if (slot.isInStash)
            {
                _stashSlots[slot.index] = slot;
            }
            else
            {
                _hotSlots[slot.index] = slot;

                if (slot.index == _selectedIndex)
                {
                    instance.EquipItem(slot.item);
                }
            }
        }
        
        private void UpdateItemTooltip(List<RaycastResult> elementsUnderMouse)
        {
            // TODO: Maybe make this function not run every frame that the mouse is over a slot
            
            var slotObjectUnderMouse = GetHoveringSlotObject(elementsUnderMouse);
            GetSlotFromObject(slotObjectUnderMouse, out var slot);
            
            if (!slotObjectUnderMouse || slot.stack == 0 || !stashObject.activeInHierarchy)
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
                        tool.projectile.damage.ToString().ToUpper(),
                        tool.attackCooldown.ToString().ToUpper(),
                        tool.toolPower.ToString().ToUpper()
                    };

                    UpdateTooltip(statNames, statValues, 3);
                    break;
                
                case MeleeSo melee:
                    ttTypeText.text = "MELEE";
                    
                    statNames = new[] { "Melee Damage", "Knockback", "Use Time" };
                    statValues = new[]
                    {
                        melee.damage.ToString().ToUpper(),
                        melee.knockback.ToString().ToUpper(),
                        melee.attackCooldown.ToString().ToUpper()
                    };
                    
                    UpdateTooltip(statNames, statValues, 3);
                    break;
                
                case WeaponSo weapon:
                    ttTypeText.text = "WEAPON";
            
                    statNames = new[] { "Damage", "Knockback", "Use Time" };
                    statValues = new[]
                    {
                        weapon.projectile.damage.ToString().ToUpper(),
                        weapon.projectile.knockback.ToString().ToUpper(),
                        weapon.attackCooldown.ToString().ToUpper()
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
            return results.Find(x => x.gameObject.name.ToLower().Contains("slot")).gameObject;
        }

        private static void GetSlotFromObject(GameObject slotObject, out Slot slot)
        {
            if (!slotObject)
            {
                slot = new Slot();
                return;
            }

            var isStash = slotObject.transform.parent.gameObject == instance.stashObject;
            var segment = isStash ? _stashSlots : _hotSlots;
                
            var slotIndex = isStash
                ? Array.FindIndex(_stashSlotObjects, x => x == slotObject.transform)
                : Array.FindIndex(_hotSlotObjects, x => x == slotObject.transform);

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
    }
}