﻿/*  This script has an enum with all the different item logic script names/codes.
    This script is used to get the right logic script with an enum key.

    That way each item can store a logic script code in their scriptable object and
    in the item constructor, get the actual logic that would be used when attacking from here.
*/

namespace Inventory.Item_Logic
{
    public static class ItemLogic
    {
        public enum LogicCode
        {
            None = 0,
            Gun = 1,
            LongMelee = 2,
            Pickaxe = 3
        }

        private static readonly ItemLogicBase[] Scripts = {
            null,
            new GunLogic(),
            new LongMeleeLogic(),
            new PickaxeLogic()
        };

        public static ItemLogicBase GetScript(LogicCode key)
        {
            return Scripts[(int)key];
        }
    }
}