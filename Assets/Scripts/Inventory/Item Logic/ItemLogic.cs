/*  This script has an enum with all the different item logic script names/codes.
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
            Gun = 0
        }

        private static readonly ItemLogicBase[] Scripts = {
            new GunLogic()
        };

        public static ItemLogicBase GetScript(LogicCode key)
        {
            return Scripts[(int)key];
        }
    }
}