using JetBrains.Annotations;
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

        public GameObject prefab;
        [CanBeNull, Tooltip("Sprite to replace the one in the given prefab. Leave null to use the one in the prefab.")]
        public Sprite placeableSprite;
        public bool usePlaceableSpriteAsHologram;
        public int toughness;
        public LightData[] lights;
    }
}
