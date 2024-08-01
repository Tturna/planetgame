using Entities;

namespace Inventory.Item_Logic
{
    public class LongMeleeLogic : ItemLogicBase
    {
        public override bool UseOnce(UseParameters useParameters)
        {
            useParameters.itemAnimationManager.AttackMelee("attackLongMelee", null, true, 2);

            return true;
        }

        public override bool UseContinuous(UseParameters useParameters) => false;

        public override bool UseOnceSecondary(UseParameters useParameters)
        {
            PlayerController.instance.ResetVelocity(true, false, true);
            
            useParameters.itemAnimationManager.AttackMelee("attackMeleeLunge", () =>
            {
                PlayerController.instance.AddForceTowardsCursor(1000f);
            });
            
            return true;
        }

        public override bool UseContinuousSecondary(UseParameters useParameters) => false;
    }
}
