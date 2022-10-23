using UnityEngine;

namespace Inventory
{
    [CreateAssetMenu(fileName="Item", menuName="SO/Item")]
    public class ItemSo : ScriptableObject
    {
        public new string name;
        public int maxStack;
        public Sprite sprite;
    }
}