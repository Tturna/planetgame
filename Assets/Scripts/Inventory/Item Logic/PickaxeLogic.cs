using System;
using Inventory;
using Inventory.Item_Logic;
using UnityEngine;
using Object = UnityEngine.Object;

public class PickaxeLogic : ItemLogicBase
{
    public override void Attack(GameObject equippedItemObject, Item attackItem, bool flipY)
    {
        var hits = Physics2D.CircleCastAll(Camera.main!.ScreenToWorldPoint(Input.mousePosition), ((ToolSo)attackItem.itemSo).toolUseArea, Vector2.zero);

        for (var i = 0; i < hits.Length; i++)
        {
            var hitObject = hits[i].collider.gameObject;

            if (!hitObject.CompareTag("Planet")) continue;
            Debug.Log(hitObject.name);
            Object.Destroy(hitObject);
        }
    }
}
