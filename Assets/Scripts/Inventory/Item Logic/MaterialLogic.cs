using System;
using System.Linq;
using Inventory.Item_Types;
using ProcGen;
using UnityEngine;

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

            var useArea = 0.5f;
            
            var mousePoint = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            mousePoint.z = 0f;

            var sizeResRatio = usePlanet.diameter / usePlanet.resolution;
            var planetRadius = usePlanet.diameter / 2;
            
            var relativeMousePoint = mousePoint - usePlanet.transform.position;
            
            // Wacky explanation:
            // There is a circle around the cursor with radius toolRadius.
            // If that circle was inside a square, this variable would be the
            // bottom left corner.
            var bottomLeftPos = relativeMousePoint - Vector3.one * useArea;
            
            var roundX = Mathf.Round(bottomLeftPos.x * 2) / 2;
            var roundY = Mathf.Round(bottomLeftPos.y * 2) / 2;
            var nearestPointPos = new Vector2(roundX, roundY);
            
            // Debug.Log($"Mouse global: {mousePoint}, local: {hoveringPointPos}");
            
            var pointXIter = (int)((nearestPointPos.x + planetRadius) / sizeResRatio);
            var pointYIter = (int)((nearestPointPos.y + planetRadius) / sizeResRatio);
            
            // Debug.Log($"Mouse xy: {pointXIter},{pointYIter}");
            
            // var idx = (usePlanet.resolution - 1) * pointYIter + pointXIter;
            
            // Debug.Log(idx);

            // How many cells fit inside the diameter of the addition circle around the mouse.
            var loopRange = Mathf.FloorToInt(useArea * 2 / sizeResRatio);

            for (var y = 0; y < loopRange; y ++)
            {
                for (var x = 0; x < loopRange; x ++)
                {
                    var xIter = pointXIter + x;
                    var yIter = pointYIter + y;

                    var index = (usePlanet.resolution - 1) * yIter + xIter;
                    
                    if (index >= 65535) continue;

                    var cornerPoints = usePlanet.GetCellCornerPoints(index);

                    for (var i = 0; i < cornerPoints.Length; i++)
                    {
                        var point = cornerPoints[i];

                        if (Vector3.Distance(point.position, mousePoint) < useArea) point.value = Mathf.Clamp01(point.value -= Time.deltaTime);
                        
                        point.isSet = true;
                        cornerPoints[i] = point;
                    }
                    
                    // Update cell
                    var cellObject = GameObject.Find($"{usePlanet.name}/Cells/Cell {index}");
                    var cellData = usePlanet.CalculateCell(yIter, xIter, index, cornerPoints);
                    cellObject ??= usePlanet.GenerateCell(index, cellData.vertices, cellData.triangles);
                    
                    var mesh = cellObject.GetComponent<MeshFilter>().mesh;
                    mesh.vertices = cellData.vertices;
                    mesh.triangles = cellData.triangles;
                    mesh.RecalculateBounds();

                    // Convert vertices to vector2[] for the collider
                    var vertices2 = Array.ConvertAll(cellData.vertices, v3 => new Vector2(v3.x, v3.y));
                    cellObject.GetComponent<PolygonCollider2D>().points = cellData.triangles.Select(trindex => vertices2[trindex]).ToArray();
                }
            }
            
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
