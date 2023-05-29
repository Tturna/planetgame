using System;
using System.Linq;
using Inventory.Inventory.Item_Types;
using Planets;
using UnityEngine;
using Utilities;
using Object = UnityEngine.Object;

namespace Inventory.Inventory.Item_Logic
{
    public class PickaxeLogic : ItemLogicBase
    {
        private static float _soil;
        private PlanetGenerator _planetGen;
        private float _mineTimer;
        private ItemAnimationManager _itemAnimationManager;

        public override bool UseOnce(GameObject equippedItemObject, Item attackItem, bool flipY,
            GameObject playerObject, ItemAnimationManager itemAnimationManager)
        {
            if (_mineTimer > 0) return true;
            
            _itemAnimationManager ??= itemAnimationManager;
            
            var tool = (ToolSo)attackItem.itemSo;
            var power = tool.toolPower;
            var mousePoint = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            
            if (Vector3.Distance(playerObject.transform.position, mousePoint) > tool.toolRange) return true;
                
            var hits = Physics2D.OverlapPointAll(mousePoint);
            
            if (_mineTimer == 0)
            {
                _itemAnimationManager.AttackMelee("attackPickaxe");
                _mineTimer = tool.attackCooldown;
                GameUtilities.instance.DelayExecute(() => _mineTimer = 0, _mineTimer);
            }

            foreach (var hit in hits)
            {
                var hitObject = hit.gameObject;
                if (hitObject.CompareTag("Ore"))
                {
                    DigOre(hitObject, power);
                    return true;
                }
            }

            return false;
        }

        public override bool UseContinuous(GameObject equippedItemObject, Item attackItem, bool flipY, GameObject playerObject, ItemAnimationManager itemAnimationManager)
        {
            var tool = (ToolSo)attackItem.itemSo;
            var useArea = tool.toolUseArea;
            var power = tool.toolPower;
            var mousePoint = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            mousePoint.z = 0f;
            
            if (Vector3.Distance(playerObject.transform.position, mousePoint) > tool.toolRange) return true;
            
            var midHits = Physics2D.OverlapPointAll(mousePoint);
            var hits = Physics2D.CircleCastAll(mousePoint, useArea, Vector2.zero);

            if (_mineTimer == 0)
            {
                _itemAnimationManager.AttackMelee("attackPickaxe");
                _mineTimer = tool.attackCooldown;
                GameUtilities.instance.DelayExecute(() => _mineTimer = 0, _mineTimer);
                
                foreach (var midHit in midHits)
                {
                    var hitObject = midHit.gameObject;

                    if (!hitObject.CompareTag("Ore")) continue;
                    DigOre(hitObject, power);
                    return true;
                }
            }
            
            foreach (var hit in hits)
            {
                var hitObject = hit.collider.gameObject;

                if (!hitObject.CompareTag("Planet")) continue;
                DigTerrain(hitObject, mousePoint, power, useArea);
                return true;
            }
            return true;
        }

        private void DigTerrain(GameObject hitObject, Vector3 mousePoint, float power, float useArea)
        {
            _planetGen ??= hitObject.transform.root.GetComponent<PlanetGenerator>();

            // Get cell data
            var idx = int.Parse(hitObject.name[5..]);
            var cellCornerPoints = _planetGen.GetCellCornerPoints(idx);

            // Do terraforming
            for (var index = 0; index < cellCornerPoints.Length; index++)
            {
                var point = cellCornerPoints[index];
                
                if (Vector3.Distance(point.position, mousePoint) > useArea) continue;
                
                var digAmount = power * Time.deltaTime;
                if (point.value + digAmount > 1f) digAmount = 1f - point.value;
                
                point.value += digAmount;
                cellCornerPoints[index] = point;
                
                _soil += digAmount;
            }
            Debug.Log($"Soil amount: {_soil}");

            // Update cell
            var (x, y) = _planetGen.GetXYFromIndex(idx);

            var cellData = _planetGen.CalculateCell(y, x, idx, cellCornerPoints);

            if (cellData.vertices == null || cellData.triangles == null)
            {
                Object.Destroy(hitObject);
                return;
            }
            
            var mesh = hitObject.GetComponent<MeshFilter>().mesh;
            mesh.vertices = cellData.vertices;
            mesh.triangles = cellData.triangles;
            mesh.RecalculateBounds();

            // Convert vertices to vector2[] for the collider
            var vertices2 = Array.ConvertAll(cellData.vertices, v3 => new Vector2(v3.x, v3.y));
            hitObject.GetComponent<PolygonCollider2D>().points = cellData.triangles.Select(trindex => vertices2[trindex]).ToArray();
        }

        private void DigOre(GameObject hitObject, float power)
        {
            var oreInstance = hitObject.GetComponent<OreInstance>();
            var oreSo = (ItemSo)oreInstance.oreSo;

            oreInstance.oreToughness -= Mathf.FloorToInt(power);
            
            if (oreInstance.oreToughness <= 0)
            {
                var item = new Item();
                item.itemSo = oreSo;
                InventoryManager.SpawnItem(item, hitObject.transform.position);
                Object.Destroy(hitObject);
            }
        }

        public override bool UseOnceSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, GameObject playerObject, ItemAnimationManager itemAnimationManager) => false;

        public override bool UseContinuousSecondary(GameObject equippedItemObject, Item attackItem, bool flipY, GameObject playerObject, ItemAnimationManager itemAnimationManager) => false;
    }
}
