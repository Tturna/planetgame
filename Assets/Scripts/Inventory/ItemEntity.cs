using Entities.Entities;
using UnityEditor;
using UnityEngine;

namespace Inventory.Inventory
{
    namespace Entities
    {
        public class ItemEntity : EntityController
        {
            public Item item;

            protected override void Start()
            {
                base.Start();

                if (!item.itemSo)
                {
                    Debug.LogWarning("Item has no scriptable object set.");
                    return;
                }

                gameObject.name = item.itemSo.name;
                GetComponent<SpriteRenderer>().sprite = item.itemSo.sprite;
            }

            [MenuItem("CONTEXT/ItemEntity/InitializeForEditor")]
            static void InitializeForEditor(MenuCommand command)
            {
                var itemEntity = (ItemEntity)command.context;
                itemEntity.gameObject.name = "(item) " + itemEntity.item.itemSo.name;
                itemEntity.GetComponent<SpriteRenderer>().sprite = itemEntity.item.itemSo.sprite;
            }
        }
    }
}