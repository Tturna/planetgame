using Entities;
using UnityEngine;

namespace Inventory.Inventory.Item_Logic
{
    public class LongMeleeLogic : ItemLogicBase
    {
        private ItemAnimationManager _itemAnimationManager;
        private PlayerController _player;
        
        public override bool UseOnce(GameObject equippedItemObject, Item attackItem, bool flipY, GameObject playerObject, ItemAnimationManager itemAnimationManager)
        {
            _itemAnimationManager ??= itemAnimationManager;
            
            _itemAnimationManager.AttackMelee("attackLongMelee");

            return true;
        }

        public override bool UseContinuous(GameObject equippedItemObject, Item attackItem, bool flipY, GameObject playerObject, ItemAnimationManager itemAnimationManager) => false;

        public override bool UseOnceSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, GameObject playerObject, ItemAnimationManager itemAnimationManager)
        {
            _itemAnimationManager ??= itemAnimationManager;
            _player ??= PlayerController.instance;
           
            _player.ResetVelocity(true, false, true);
            
            _itemAnimationManager.AttackMelee("attackMeleeLunge", () =>
            {
                _player.AddForceTowardsCursor(1000f);
            });
            
            return true;
        }

        public override bool UseContinuousSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, GameObject playerObject, ItemAnimationManager itemAnimationManager) => false;
    }
}
