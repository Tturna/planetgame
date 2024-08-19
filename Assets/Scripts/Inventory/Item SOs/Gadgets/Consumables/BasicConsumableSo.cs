using System.Collections.Generic;
using UnityEngine;

namespace Inventory.Item_SOs.Gadgets.Consumables
{
    [CreateAssetMenu(fileName = "Basic Consumable", menuName = "SO/Gadgets/Consumables/Basic Consumable")]
    public class BasicConsumableSo : BasicGadgetSo
    {
        public List<StatModifier> statModifiers;
        public float healthIncreaseOnUse;
        public float energyIncreaseOnUse;
        public float jetpackFuelIncreaseOnUse;
        public float effectDuration;
    }
}