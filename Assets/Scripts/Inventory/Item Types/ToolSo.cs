using UnityEngine;

namespace Inventory.Item_Types
{
    [CreateAssetMenu(fileName = "Tool", menuName = "SO/Tool")]
    public class ToolSo : WeaponSo
    {
        public float toolPower;
        public float toolUseArea;
    }
}