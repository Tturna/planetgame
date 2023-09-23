using UnityEngine;

namespace Inventory.Item_SOs
{
    [CreateAssetMenu(fileName = "Tool", menuName = "SO/Tool")]
    public class ToolSo : WeaponSo
    {
        public float toolPower;
        public float toolUseArea;
        public float toolRange;
    }
}
