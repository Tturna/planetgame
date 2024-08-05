using UnityEngine;

namespace Inventory.Item_SOs
{
    [CreateAssetMenu(fileName = "Placeable", menuName = "SO/Placeable")]
    public class PlaceableSo : UsableItemSo
    {
        [System.Serializable]
        public struct LightData
        {
            public float range;
            [Range(0f, 1f)]
            public float falloffStrength;
            public float intensity;
            public Color color;
        }

        public int toughness;
        public LightData[] lights;
    }
}
