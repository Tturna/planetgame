using UnityEngine;

namespace Inventory.Item_Logic
{
    public abstract class ItemLogicBase
    {
        // One of these functions is called based on whether Attack is called from GetKey or GetKeyDown.
        // GetKeyDown calls AttackOnce, GetKey calls AttackContinuous
        
        // If a weapon is not supposed to attack continuously, make it return false. Same with AttackOnce.
        // Otherwise return true.
        public abstract bool AttackOnce(GameObject equippedItemObject, Item attackItem, bool flipY);
        public abstract bool AttackContinuous(GameObject equippedItemObject, Item attackItem, bool flipY);
    }
}