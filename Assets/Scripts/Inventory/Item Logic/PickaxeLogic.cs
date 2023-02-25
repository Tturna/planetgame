using System;
using System.Linq;
using ProcGen;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Inventory.Item_Logic
{
    public class PickaxeLogic : ItemLogicBase
    {
        public override bool AttackOnce(GameObject equippedItemObject, Item attackItem, bool flipY) => false;

        public override bool AttackContinuous(GameObject equippedItemObject, Item attackItem, bool flipY)
        {
            var tool = (ToolSo)attackItem.itemSo;
            var mousePoint = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            mousePoint.z = 0f;
            
            var hits = Physics2D.CircleCastAll(mousePoint, tool.toolUseArea, Vector2.zero);

            PlanetGenerator planetGen = null;
            for (var i = 0; i < hits.Length; i++)
            {
                var hitObject = hits[i].collider.gameObject;

                if (!hitObject.CompareTag("Planet")) continue;

                planetGen ??= hitObject.transform.root.GetComponent<PlanetGenerator>();

                var res = Dig(hitObject, tool, planetGen, mousePoint);
                if (!res) return false;
            }

            return true;
        }

        bool Dig(GameObject cellObject, ToolSo tool, PlanetGenerator planetGen, Vector3 mousePoint)
        {
            // Get cell data
            var idx = int.Parse(cellObject.name[5..]);
            var cellCornerPoints = planetGen.GetCellCornerPoints(idx);
                
            // Do terraforming
            for (var index = 0; index < cellCornerPoints.Length; index++)
            {
                var point = cellCornerPoints[index];

                if (Vector3.Distance(point.position, mousePoint) > tool.toolUseArea) continue;
                    
                point.value = Mathf.Clamp01(point.value + 0.01f * tool.toolPower);
                cellCornerPoints[index] = point;
            }

            // Update cell
            var (x, y) = planetGen.GetXYFromIndex(idx);
            var cellData = planetGen.CalculateCell(y, x, idx, cellCornerPoints);

            if (cellData.vertices == null || cellData.triangles == null)
            {
                Object.Destroy(cellObject);
                return true;
            }
                
            var mesh = cellObject.GetComponent<MeshFilter>().mesh;
            mesh.vertices = cellData.vertices;
            mesh.triangles = cellData.triangles;
            mesh.RecalculateBounds();

            // Convert vertices to vector2[] for the collider
            var vertices2 = Array.ConvertAll(cellData.vertices, v3 => new Vector2(v3.x, v3.y));
            cellObject.GetComponent<PolygonCollider2D>().points = cellData.triangles.Select(trindex => vertices2[trindex]).ToArray();

            return true;
        }
    }
}
