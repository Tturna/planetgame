using System.Collections.Generic;
using UnityEngine;

namespace Inventory.Item_SOs.Accessories
{
    
    [CreateAssetMenu(fileName = "Basic Accessory", menuName = "SO/Accessories/Basic Accessory")]
    public class BasicAccessorySo : ItemSo
    {
        public List<StatModifier> statModifiers = new();

        public virtual void ResetBehavior() { }
        
        public virtual void UpdateProcess() { }
    }
}
