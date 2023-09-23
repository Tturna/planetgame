using Entities;

namespace Inventory.Item_Logic
{
    public class LongMeleeLogic : ItemLogicBase
    {
        private ItemAnimationManager _itemAnimationManager;
        private PlayerController _player;
        
        public override bool UseOnce(UseParameters useParameters)
        {
            _itemAnimationManager ??= useParameters.itemAnimationManager;
            
            _itemAnimationManager.AttackMelee("attackLongMelee", null, true, 2);

            return true;
        }

        public override bool UseContinuous(UseParameters useParameters) => false;

        public override bool UseOnceSecondary(UseParameters useParameters)
        {
            _itemAnimationManager ??= useParameters.itemAnimationManager;
            _player ??= PlayerController.instance;
           
            _player.ResetVelocity(true, false, true);
            
            _itemAnimationManager.AttackMelee("attackMeleeLunge", () =>
            {
                _player.AddForceTowardsCursor(1000f);
            });
            
            return true;
        }

        public override bool UseContinuousSecondary(UseParameters useParameters) => false;
    }
}
