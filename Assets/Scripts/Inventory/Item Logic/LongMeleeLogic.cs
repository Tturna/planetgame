using Entities;

namespace Inventory.Item_Logic
{
    public class LongMeleeLogic : ItemLogicBase
    {
        public override bool UseOnce(UseParameters useParameters)
        {
            var parameters = new ItemAnimationManager.AttackMeleeParameters("attackLongMelee")
            {
                roundRobin = true,
                maxAttacks = 2
            };
            
            useParameters.itemAnimationManager.AttackMelee(parameters);

            return true;
        }

        public override bool UseContinuous(UseParameters useParameters) => false;

        public override bool UseOnceSecondary(UseParameters useParameters)
        {
            PlayerController.instance.ResetVelocity(true, false, true);
            var parameters = new ItemAnimationManager.AttackMeleeParameters("attackMeleeLunge")
            {
                animationEventCallback = () =>
                {
                    PlayerController.instance.AddForceTowardsCursor(10000f);
                }
            };
            
            useParameters.itemAnimationManager.AttackMelee(parameters);
            
            return true;
        }

        public override bool UseContinuousSecondary(UseParameters useParameters) => false;
    }
}
