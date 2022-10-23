using System;
using Inventory.Item_Logic;

namespace Inventory
{
    [Serializable]
    public class Item
    {
        public ItemSo itemSo;
        public ItemLogicBase LogicScript;
        
        //public Item() {}

        // Clone constructor
        public Item(Item source)
        {
            itemSo = source.itemSo;
            LogicScript = null;
            
            if (itemSo is WeaponSo so)
            {
                LogicScript = ItemLogic.GetScript(so.logicCode);
            }
        }
    }
}