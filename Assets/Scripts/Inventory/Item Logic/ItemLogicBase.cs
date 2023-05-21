using UnityEngine;

namespace Inventory.Inventory.Item_Logic
{
    public abstract class ItemLogicBase
    {
        // One of these functions is called based on whether Use is called from GetKey or GetKeyDown.
        // GetKeyDown calls UseOnce, GetKey calls UseContinuous
        
        // If an item is not supposed to be used continuously, make UseContinuous return false. Same with UseOnce.
        // Otherwise return true.
        public abstract bool UseOnce(GameObject equippedItemObject, Item attackItem, bool flipY, GameObject playerObject, ItemAnimationManager itemAnimationManager);
        public abstract bool UseContinuous(GameObject equippedItemObject, Item attackItem, bool flipY, GameObject playerObject, ItemAnimationManager itemAnimationManager);
        
        // Same thing for secondary uses
        public abstract bool UseOnceSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, GameObject playerObject, ItemAnimationManager itemAnimationManager);
        public abstract bool UseContinuousSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, GameObject playerObject, ItemAnimationManager itemAnimationManager);
    }
}