using UnityEngine;

namespace Inventory.Item_Logic
{
    public abstract class ItemLogicBase
    {
        public abstract void Attack(GameObject equippedItemObject, Item attackItem, bool flipY);
    }
}