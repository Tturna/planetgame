using UnityEngine;

namespace Inventory.Item_Logic
{
    public abstract class ItemLogicBase
    {
        public struct UseParameters
        {
            public GameObject equippedItemObject;
            public Item attackItem;
            public bool flipY;
            public GameObject playerObject;
            public ItemAnimationManager itemAnimationManager;
            public int particleIndex;
            public Vector2 particleOffset;
            public Color particleColor;
            public AudioClip[] effectSounds;
        }
        
        // One of these functions is called based on whether Use is called from GetKey or GetKeyDown.
        // GetKeyDown calls UseOnce, GetKey calls UseContinuous
        
        // If an item is not supposed to be used continuously, make UseContinuous return false. Same with UseOnce.
        // Otherwise return true.
        public abstract bool UseOnce(UseParameters useParameters);
        public abstract bool UseContinuous(UseParameters useParameters);
        
        // Same thing for secondary uses
        public abstract bool UseOnceSecondary(UseParameters useParameters);
        public abstract bool UseContinuousSecondary(UseParameters useParameters);
    }
}