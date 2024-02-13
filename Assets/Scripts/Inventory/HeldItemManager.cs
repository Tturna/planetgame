// This script is supposed to be on the root player object with PlayerController

using System;
using System.Collections;
using Entities;
using Inventory.Item_Logic;
using Inventory.Item_SOs;
using JetBrains.Annotations;
using UnityEngine;
using Utilities;

namespace Inventory
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerStatsManager))]
    public class HeldItemManager : MonoBehaviour
    {
        [SerializeField] private Transform equippedItemTransform;
        [SerializeField] private Transform recoilAnchor;
        [SerializeField] private Animator handsAnimator; // This component is also used by PlayerController
        [SerializeField] private Transform effectParent;
        [SerializeField] private Transform flippingEffectParent;
        
        private Transform _itemAnchor;
        private Transform _handsParent, _handLeft, _handRight;
        private SpriteRenderer _equippedSr;
        private Animator _recoilAnimator;
        private ItemAnimationManager _itemAnimationManager;
        [CanBeNull] private Item _equippedItem;
        private Rigidbody2D _rigidbody; // This component is also used by PlayerController
        
        public delegate void ItemUsedHandler(Item item);
        public static event ItemUsedHandler ItemUsed;
            
        private void Start()
        {
            _itemAnchor = recoilAnchor.parent;
            _recoilAnimator = recoilAnchor.GetComponent<Animator>();
            _itemAnimationManager = recoilAnchor.GetComponent<ItemAnimationManager>();
            _rigidbody = GetComponent<Rigidbody2D>();

            _handsParent = handsAnimator.transform;
            _handLeft = _handsParent.GetChild(0).GetChild(0);
            _handRight = _handsParent.GetChild(1).GetChild(0);

            _equippedSr = equippedItemTransform.GetComponent<SpriteRenderer>();

            InventoryManager.ItemEquipped += ItemEquipped;
        }

        private void Update()
        {
            var mouseDirection = GameUtilities.GetVectorToWorldCursor(transform.position).normalized;
            var cursorAngle = GameUtilities.GetCursorAngle(mouseDirection, transform.right);
            HandleItemAiming(mouseDirection, cursorAngle);
            
            if (UIUtilities.IsMouseOverUI()) return;
            
            // Use Item
            // UseItem functions return a bool based on if the attack was called with "once" on or off.
            // This way logic scripts can choose which case to act on, GetKey or GetKeyDown.
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                if (UseItem(true, false)) return;
            }
            
            if (Input.GetKey(KeyCode.Mouse0))
            {
                if (UseItem(false, false)) return;
            }
            
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                if (UseItem(true, true)) return;
            }
            
            if (Input.GetKey(KeyCode.Mouse1))
            {
                if (UseItem(false, true)) return;
            }
        }

        private void HandleItemAiming(Vector3 directionToMouse, float cursorAngle)
        {
            var trForward = transform.forward;

            var cross = Vector3.Cross(-trForward, directionToMouse);
            _itemAnchor.LookAt( transform.position - trForward, cross);

            // Flip sprite when aiming right
            // var angle = Vector3.Angle(transform.right, directionToMouse);
            // _equippedSr.flipY = angle < 90;
        
            // Flip the object when aiming right
            // We do this because otherwise the recoil animation is flipped
            var scale = recoilAnchor.localScale;
            scale.y = cursorAngle < 90 ? -1f : 1f;
            _itemAnchor.localScale = scale;
            flippingEffectParent.localScale = scale; // flip flipping effects
        
            // Manually set left hand position when holding an item
            if (_equippedItem?.itemSo != null)
            {
                handsAnimator.SetLayerWeight(1, 0f);
                handsAnimator.SetLayerWeight(2, 0f);

                var relativeOffset = _equippedItem.itemSo.handPositionOffset;
                var itemRight = equippedItemTransform.right;
                var itemUp = equippedItemTransform.up;
            
                var x = itemRight * relativeOffset.x;
                var y = itemUp * (relativeOffset.y * (cursorAngle < 90 ? -1f : 1f));
            
                var offset = x + y;

                _handLeft.position = equippedItemTransform.position + offset;
                
                if (_equippedItem.itemSo.useBothHands)
                {
                    _handRight.position = equippedItemTransform.position + offset;
                }
                else
                {
                    _handRight.localPosition = Vector3.zero;
                }
            }
            else
            {
                handsAnimator.SetLayerWeight(1, 1f);
                handsAnimator.SetLayerWeight(2, 1f);
                _handLeft.localPosition = Vector3.zero;
                _handRight.localPosition = Vector3.zero;
            }
        }
        
        private bool UseItem(bool once, bool secondary)
        {
            if (_equippedItem?.itemSo is not UsableItemSo usableItemSo) return false;
            if (_equippedItem.logicScript == null) return false;
            if (usableItemSo.isOnCooldown) return false;

            if (PlayerStatsManager.Energy < usableItemSo.energyCost)
            {
                NoEnergy();
                return false;
            }

            // Use Item
            Func<ItemLogicBase.UseParameters, bool> useitemFunction;

            if (once)
            {
                useitemFunction = secondary
                    ? _equippedItem.logicScript.UseOnceSecondary
                    : _equippedItem.logicScript.UseOnce;
            }
            else
            {
                useitemFunction = secondary
                    ? _equippedItem.logicScript.UseContinuousSecondary
                    : _equippedItem.logicScript.UseContinuous;
            }

            var useParameters = new ItemLogicBase.UseParameters
            {
                equippedItemObject = equippedItemTransform.gameObject,
                attackItem = _equippedItem,
                flipY = _equippedSr.flipY,
                playerObject = gameObject,
                itemAnimationManager = _itemAnimationManager
            };

            var res = useitemFunction(useParameters);
            
            if (!res) return false;
            
            OnItemUsed(_equippedItem);

            // TODO: Use attack speed from PlayerStatsManager
            if (usableItemSo.energyCost > 0)
            {
                StartCoroutine(HandleWeaponCooldown(usableItemSo));
                PlayerStatsManager.ChangeEnergy(-usableItemSo.energyCost);
            }
            
                
            // Recoil
            _recoilAnimator.SetLayerWeight(1, usableItemSo.recoilHorizontal);
            _recoilAnimator.SetLayerWeight(2, usableItemSo.recoilAngular);
            _recoilAnimator.SetFloat("recoil_shpeed_horizontal", usableItemSo.recoilSpeedHorizontal);
            _recoilAnimator.SetFloat("recoil_shpeed_angular", usableItemSo.recoilSpeedAngular);
            _recoilAnimator.SetTrigger("recoil");
            
            // Player recoil
            var recoilDirection = -_itemAnchor.right;
            _rigidbody.AddForce(recoilDirection * usableItemSo.playerRecoilStrength, ForceMode2D.Impulse);
            
            // Camera shake
            CameraController.CameraShake(usableItemSo.cameraShakeTime, usableItemSo.cameraShakeStrength);

            return true;
        }
        
        private void NoEnergy()
        {
            throw new NotImplementedException();
        }
        
        private IEnumerator HandleWeaponCooldown(UsableItemSo usableItem)
        {
            usableItem.isOnCooldown = true;
            yield return new WaitForSeconds(usableItem.attackCooldown);
            usableItem.isOnCooldown = false;
        }

        private void OnItemUsed(Item item)
        {
            ItemUsed?.Invoke(item);
        }

        private void ItemEquipped(Item item)
        {
            _equippedItem = item;
            var state = item != null && item.itemSo.altIdleAnimation;
            _recoilAnimator.SetBool("altIdle", state);
        }
    }
}
