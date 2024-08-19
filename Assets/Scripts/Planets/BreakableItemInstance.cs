using Inventory.Item_SOs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Planets
{
    public class BreakableItemInstance : MonoBehaviour
    {
        [FormerlySerializedAs("oreSo")] public ItemSo itemSo;
        [FormerlySerializedAs("oreToughness")] public int toughness;

#if UNITY_EDITOR
        [MenuItem("CONTEXT/BreakableItemInstance/InitializeForEditor")]
        static void InitializeForEditor(MenuCommand command)
        {
            var breakableItemInstance = (BreakableItemInstance)command.context;
            breakableItemInstance.gameObject.name = "(breakable) " + breakableItemInstance.itemSo.name;
            breakableItemInstance.GetComponent<SpriteRenderer>().sprite = breakableItemInstance.itemSo.sprite;
            breakableItemInstance.GetComponent<BoxCollider2D>().size = breakableItemInstance.itemSo.sprite.bounds.size;
        }
#endif
    }
}
