using Entities;
using Inventory.Item_SOs.Gadgets.Consumables;
using Utilities;

namespace Inventory.Item_Logic
{
    public class ConsumableLogic : ItemLogicBase
    {
        public override bool UseOnce(UseParameters useParameters)
        {
            var consumableItem = useParameters.attackItem;
            var consumableSo = (BasicConsumableSo)consumableItem.itemSo;

            if (consumableSo.healthIncreaseOnUse != 0)
            {
                PlayerStatsManager.ChangeHealth(consumableSo.healthIncreaseOnUse);
            }
            
            if (consumableSo.energyIncreaseOnUse != 0)
            {
                PlayerStatsManager.ChangeHealth(consumableSo.energyIncreaseOnUse);
            }
            
            if (consumableSo.jetpackFuelIncreaseOnUse != 0)
            {
                PlayerStatsManager.ChangeHealth(consumableSo.jetpackFuelIncreaseOnUse);
            }

            if (consumableSo.statModifiers.Count == 0) return true;
            if (consumableSo.effectDuration == 0) return true;
            
            PlayerStatsManager.AddStatModifiers(consumableSo.statModifiers, consumableSo.id);

            GameUtilities.instance.DelayExecute(() =>
            {
                PlayerStatsManager.RemoveStatModifiers(consumableSo.id);
            }, consumableSo.effectDuration);
            
            return true;
        }

        public override bool UseContinuous(UseParameters useParameters)
        {
            return false;
        }

        public override bool UseOnceSecondary(UseParameters useParameters)
        {
            return false;
        }

        public override bool UseContinuousSecondary(UseParameters useParameters)
        {
            return false;
        }
    }
}