using System;
using System.Linq;
using Cameras;
using Entities;
using Planets;
using UnityEngine;

namespace Inventory.Item_Logic
{
    public class MaterialLogic : ItemLogicBase
    {
        private Camera _camera;
        private PlayerController _player;
        private GameObject _currentPlayerPlanet;
        private PlanetGenerator _usePlanet;
        
        public override bool UseOnce(UseParameters useParameters) => false;

        public override bool UseContinuous(UseParameters useParameters)
        {
            // use this instead of ??= because ??= bypasses the unity object lifetime check
            if (!_player)
            {
                _player = PlayerController.instance;
            }
            
            if (!_player.CurrentPlanetObject)
            {
                return true;
            }

            if (_player.CurrentPlanetObject != _currentPlayerPlanet)
            {
                _usePlanet = _player.CurrentPlanetObject.GetComponent<PlanetGenerator>();
            }
            
            _currentPlayerPlanet = _player.CurrentPlanetObject;
            
            if (!_usePlanet) return false;

            if (!_camera) _camera = CameraController.instance.mainCam;
            
            // TODO: Set this up as a player "statistic parameter/attribute"
            const float useArea = 0.75f;
            
            var mousePoint = _camera.ScreenToWorldPoint(Input.mousePosition);
            mousePoint.z = 0f;

            var sizeResRatio = _usePlanet.diameter / _usePlanet.resolution;
            
            var relativeMousePoint = mousePoint - _usePlanet.transform.position;
            
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

            var cellCoords = _usePlanet.WorldToCellPoint(nearestPointPos);
            
            // var pointXIter = (int)((nearestPointPos.x + planetRadius) / sizeResRatio);
            // var pointYIter = (int)((nearestPointPos.y + planetRadius) / sizeResRatio);
            
            // Debug.Log($"Mouse xy: {pointXIter},{pointYIter}");
            
            // var idx = (usePlanet.resolution - 1) * pointYIter + pointXIter;
            
            // Debug.Log(idx);

            // How many cells fit inside the diameter of the addition circle around the mouse.
            var loopRange = Mathf.FloorToInt(useArea * 2 / sizeResRatio);

            // increment by 2 to prevent terraforming the same points multiple times
            for (var y = 0; y < loopRange; y++)
            {
                for (var x = 0; x < loopRange; x++)
                {
                    var xIter = cellCoords.x + x;
                    var yIter = cellCoords.y + y;

                    var index = (_usePlanet.resolution - 1) * yIter + xIter;
                    
                    // Check if cell is out of bounds (255*255 cell grid when resolution is 256)
                    if (index >= 65025) continue;

                    var cornerPoints = _usePlanet.GetCellCornerPoints(index);

                    // TODO: Figure out a system so cells aren't updated for no reason...
                    // e.g. if the player is holding down the button without moving the mouse
                    
                    for (var i = 0; i < cornerPoints.Length; i++)
                    {
                        var point = cornerPoints[i];
                        
                        var pointDistance = Vector3.Distance(point.position, mousePoint);
                        var normalDistance = pointDistance / useArea;

                        if (pointDistance < useArea)
                        {
                            var strength = 1f - normalDistance;
                            point.value = Mathf.Clamp(point.value -= strength * Time.deltaTime, 0f, 1f);
                        }

                        point.isSet = true;
                        cornerPoints[i] = point;
                    }

                    // Update cell
                    // var cellObject = GameObject.Find($"{usePlanet.name}/Cells/Cell {index}");
                    var cellObject = _usePlanet.GetCellFromIndex(index);
                    var cellData = _usePlanet.CalculateCell(yIter, xIter, index, cornerPoints);
                    
                    if (cellData.idx == -1) continue;
                    
                    if (!cellObject)
                    {
                        _usePlanet.GenerateCell(index, cellData.vertices, cellData.triangles);
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

        public override bool UseOnceSecondary(UseParameters useParameters) => false;

        public override bool UseContinuousSecondary(UseParameters useParameters) => false;
    }
}
