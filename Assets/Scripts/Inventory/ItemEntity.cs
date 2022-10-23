using Entities;
using UnityEngine;

namespace Inventory
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
        }
    }
}