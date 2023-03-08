using System;
using System.Linq;
using ProcGen;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Inventory.Item_Logic
{
    public class MaterialLogic : ItemLogicBase
    {
        public override bool UseOnce(GameObject equippedItemObject, Item attackItem, bool flipY, PlanetGenerator usePlanet = null) => false;

        public override bool UseContinuous(GameObject equippedItemObject, Item attackItem, bool flipY, PlanetGenerator usePlanet)
        {
            if (!usePlanet) return false;
            
            // TODO: Figure out how to do terrain addition
            
            /*
             * First of all, you should probably try taking the mouse position relative to the planet you're editing.
             * Then take the coordinates of that position and use them to get the cell index.
             * Check if that cell exists, and if not, create it.
             * Add terrain to all the points in that cell that are within x distance from the mouse point.
             * ???
             * profit
             */
            
            var mousePoint = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            mousePoint.z = 0f;

            var sizeResRatio = usePlanet.diameter / usePlanet.resolution;
            var planetRadius = usePlanet.diameter / 2;

            var relativeMousePoint = mousePoint - usePlanet.transform.position;
            var roundX = (float)Math.Floor(relativeMousePoint.x * 2) / 2;
            var roundY = (float)Math.Floor(relativeMousePoint.y * 2) / 2;
            var hoveringPointPos = new Vector2(roundX, roundY);

            // Debug.Log($"Mouse global: {mousePoint}, local: {hoveringPointPos}");
            
            var pointXIter = (hoveringPointPos.x + planetRadius) / sizeResRatio;
            var pointYIter = (hoveringPointPos.y + planetRadius) / sizeResRatio;

            // Debug.Log($"Mouse xy: {pointXIter},{pointYIter}");
            
            var idx = (usePlanet.resolution - 1) * pointYIter + pointXIter;
            
            Debug.Log(idx);
            
            // var hits = Physics2D.CircleCastAll(mousePoint, useArea, Vector2.zero);

            // PlanetGenerator planetGen = null;
            // for (var i = 0; i < hits.Length; i++)
            // {
            //     var hitObject = hits[i].collider.gameObject;
            //
            //     if (!hitObject.CompareTag("Planet")) continue;
            //
            //     planetGen ??= hitObject.transform.root.GetComponent<PlanetGenerator>();
            //
            //     // Get cell data
            //     var idx = int.Parse(hitObject.name[5..]);
            //     var cellCornerPoints = planetGen.GetCellCornerPoints(idx);
            //
            //     // Do terraforming
            //     for (var index = 0; index < cellCornerPoints.Length; index++)
            //     {
            //         var point = cellCornerPoints[index];
            //
            //         if (Vector3.Distance(point.position, mousePoint) > useArea) continue;
            //
            //         var digAmount = power * Time.deltaTime;
            //         if (point.value + digAmount > 1f) digAmount = 1f - point.value;
            //
            //         point.value += digAmount;
            //         cellCornerPoints[index] = point;
            //     }
            //
            //     // Update cell
            //     var (x, y) = planetGen.GetXYFromIndex(idx);
            //     var cellData = planetGen.CalculateCell(y, x, idx, cellCornerPoints);
            //
            //     if (cellData.vertices == null || cellData.triangles == null)
            //     {
            //         Object.Destroy(hitObject);
            //         return true;
            //     }
            //
            //     var mesh = hitObject.GetComponent<MeshFilter>().mesh;
            //     mesh.vertices = cellData.vertices;
            //     mesh.triangles = cellData.triangles;
            //     mesh.RecalculateBounds();
            //
            //     // Convert vertices to vector2[] for the collider
            //     var vertices2 = Array.ConvertAll(cellData.vertices, v3 => new Vector2(v3.x, v3.y));
            //     hitObject.GetComponent<PolygonCollider2D>().points =
            //         cellData.triangles.Select(trindex => vertices2[trindex]).ToArray();
            // }

            return true;
        }
    }
}
