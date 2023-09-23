using System;
using Inventory.Item_Logic;
using Inventory.Item_SOs;

namespace Inventory
{
    [Serializable]
    public class Item
    {
        public ItemSo itemSo;
        public ItemLogicBase logicScript;
        
        public Item() {}

        // Clone constructor
        public Item(Item source)
        {
            itemSo = source.itemSo;
            
            logicScript = null;
            
            if (itemSo is UsableItemSo so)
            {
                logicScript = ItemLogic.GetScript(so.logicCode);
            }
        }
    }
}