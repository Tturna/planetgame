using System;
using System.Linq;
using ProcGen;
using UnityEngine;

namespace Inventory.Item_Logic
{
    public class MaterialLogic : ItemLogicBase
    {
        private Camera _camera;
        
        public override bool UseOnce(GameObject equippedItemObject, Item attackItem, bool flipY, PlanetGenerator usePlanet = null) => false;

        public override bool UseContinuous(GameObject equippedItemObject, Item attackItem, bool flipY, PlanetGenerator usePlanet)
        {
            if (!usePlanet) return false;

            if (!_camera) _camera = Camera.main!;
            
            // TODO: Set this up as a player "statistic parameter/attribute"
            const float useArea = 0.5f;
            
            var mousePoint = _camera.ScreenToWorldPoint(Input.mousePosition);
            mousePoint.z = 0f;

            var sizeResRatio = usePlanet.diameter / usePlanet.resolution;
            var planetRadius = usePlanet.diameter / 2;
            
            var relativeMousePoint = mousePoint - usePlanet.transform.position;
            
            // Wacky explanation:
            // There is a circle around the cursor with radius useArea.
            // If that circle was inside a square, this variable would be the
            // bottom left corner.
            var bottomLeftPos = relativeMousePoint - Vector3.one * useArea;
            
            // Round position to nearest 0.5
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
                    
                    // Check if cell is out of bounds (255*255 cell grid when resolution is 256)
                    if (index >= 65025) continue;

                    var cornerPoints = usePlanet.GetCellCornerPoints(index);

                    if (index == 64388)
                    {
                        Debug.Log("go'em");
                    }
                    
                    // TODO: Figure out a system so cells aren't updated for no reason...
                    // e.g. if the player is holding down the button without moving the mouse
                    
                    for (var i = 0; i < cornerPoints.Length; i++)
                    {
                        var point = cornerPoints[i];
                        
                        /* TODO: Fix addition snappiness
                         * Addition is snappy probably because the build area has to be over a point before it starts
                         * editing its value. The editing has to happen instantly because otherwise when the player
                         * edits in the air, it takes soil before anything appears because the value of the points
                         * is usually 1 and way lower than their isolevel. Keep in mind that with the current system
                         * (as of 17.3.2023), the outer most points in a planet are generated but set to air. This
                         * means that when adding terrain next to existing terrain, the new terrain "snaps" and
                         * instantly connects to the adjacent terrain, which looks rough.
                         *
                         * Theoretical fix:
                         * Instead of only editing the value of a corner point when the build area is over it,
                         * edit it when a neighboring point is being edited. Like if you have corners bl, br, tr and tl,
                         * adding terrain to bl would "bulge" the terrain towards the other points until the edge
                         * reaches the build area edge. This would make addition smooth. This might be a bit hard though,
                         * as you'd need to check adjacent cells maybe? idk try it out.
                        */

                        if (Vector3.Distance(point.position, mousePoint) < useArea)
                        {
                            point.value = Mathf.Clamp(point.value -= Time.deltaTime, 0f, point.isoLevel);
                        }

                        point.isSet = true;
                        cornerPoints[i] = point;
                    }

                    // Update cell
                    var cellObject = GameObject.Find($"{usePlanet.name}/Cells/Cell {index}");
                    var cellData = usePlanet.CalculateCell(yIter, xIter, index, cornerPoints);
                    
                    if (!cellObject)
                    {
                        usePlanet.GenerateCell(index, cellData.vertices, cellData.triangles);
                    }
                    else
                    {
                        var mesh = cellObject.GetComponent<MeshFilter>().mesh;
                        mesh.vertices = cellData.vertices;
                        mesh.triangles = cellData.triangles;
                        mesh.RecalculateBounds();
                        
                        // Convert vertices to vector2[] for the collider
                        var vertices2 = Array.ConvertAll(cellData.vertices, v3 => new Vector2(v3.x, v3.y));
                        cellObject.GetComponent<PolygonCollider2D>().points = cellData.triangles.Select(trindex => vertices2[trindex]).ToArray();
                    }
                }
            }

            return true;
        }
    }
}
