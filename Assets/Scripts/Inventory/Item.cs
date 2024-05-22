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

        public Item(ItemSo itemSo)
        {
            this.itemSo = itemSo;
            
            logicScript = null;
            
            if (itemSo is UsableItemSo so)
            {
                logicScript = ItemLogic.GetScript(so.logicCode);
            }
        }

        // Clone constructor
        public Item(Item source) : this(source.itemSo) { }
    }
}