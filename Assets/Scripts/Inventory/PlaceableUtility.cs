using System;
using Entities;
using Inventory.Item_SOs;
using UnityEngine;
using Utilities;

namespace Inventory
{
    public static class PlaceableUtility
    {
        private static Collider2D[] _circleHits;
        private static Collider2D[] _neighbors;
        private static bool _initialized;

        private static void Init()
        {
            if (_initialized) return;
            _circleHits = new Collider2D[8];
            _neighbors = new Collider2D[4];
            _initialized = true;
        }

        public static bool TryGetPlaceablePosition(Vector3 mousePoint, ItemSo equippedItemSo, out Vector3? position, out Vector3? normal)
        {
            if (!_initialized) Init();
            
            const float placementAssistRange = 1f;
            var mask = GameUtilities.BasicMovementCollisionMask;

            if (equippedItemSo is RoomModuleSo roomModuleSo)
            {
                var buildingBoundsMask = 1 << LayerMask.NameToLayer("BuildingBounds");
                var neighborCheckRange = Mathf.Max(roomModuleSo.boundsSize.x, roomModuleSo.boundsSize.y);
                var neighborCount = Physics2D.OverlapCircleNonAlloc(mousePoint, neighborCheckRange, _neighbors, buildingBoundsMask);
                
                if (neighborCount > 0)
                {
                    var closestNeighbor = _neighbors[0];
                    var closestRoomTransform = closestNeighbor.transform.root;
                    var closestPoint = closestNeighbor.ClosestPoint(mousePoint);

                    for (var i = 1; i < neighborCount; i++)
                    {
                        var neighbor = _neighbors[i];
                        if (neighbor == closestNeighbor) continue;
                        if (neighbor.transform.root == closestRoomTransform) continue;
                        
                        var neighborClosestPoint = neighbor.ClosestPoint(mousePoint);
                        
                        if (Vector2.Distance(mousePoint, neighborClosestPoint) < Vector2.Distance(mousePoint, closestPoint))
                        {
                            closestNeighbor = neighbor;
                            closestRoomTransform = closestNeighbor.transform.root;
                            closestPoint = neighborClosestPoint;
                        }
                    }
                    
                    var closestNeighborPos = closestRoomTransform.position;
                    var closestNeighborUp = closestRoomTransform.up;
                    var closestNeighborRight = closestRoomTransform.right;
                    var neighborToMouse = mousePoint - closestNeighborPos;
                    var relativeMouse = closestRoomTransform.InverseTransformDirection(neighborToMouse);
                    var compareVertical = Mathf.Abs(relativeMouse.y) > Mathf.Abs(relativeMouse.x);
                    var neighborSize = ((BoxCollider2D)closestNeighbor).size;

                    var offsetDirection = relativeMouse switch
                    {
                        _ when !compareVertical && relativeMouse.x > 0f => closestNeighborRight,
                        _ when !compareVertical && relativeMouse.x < 0f => -closestNeighborRight,
                        _ when relativeMouse.y > 0f => closestNeighborUp,
                        _ when relativeMouse.y < 0f => -closestNeighborUp,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    
                    var offset = compareVertical
                        ? offsetDirection * roomModuleSo.boundsSize.y / 2f
                        : offsetDirection * roomModuleSo.boundsSize.x / 2f;
                    
                    var neighborBoundExtent = (compareVertical ? neighborSize.y : neighborSize.x) / 2f;
                    normal = closestNeighborUp;
                    // move down by half the room size because HeldItemManager and PlaceableLogic where this data
                    // is used will move the object up by half the room size. They do this because they are made for
                    // generic placeable objects that need this behavior, as do rooms when they're not
                    // connected to neighbors.
                    
                    position = closestNeighborPos + offset + offsetDirection * neighborBoundExtent - normal * roomModuleSo.boundsSize.y / 2;

                    if (compareVertical)
                    {
                        return true;
                    }
                    
                    const int maxHorizontalStack = 2;
                    const float offsetAddition = 0.2f;
                    var checkpoint = (Vector3)position;
                    var pointOffset = -offset.normalized * (roomModuleSo.boundsSize.x / 2f + offsetAddition);

                    for (var i = 0; i <= maxHorizontalStack; i++)
                    {
                        var hit = Physics2D.OverlapPoint(checkpoint + pointOffset, buildingBoundsMask);

                        if (hit)
                        {
                            checkpoint = hit.transform.position;
                            pointOffset = -offset.normalized * (hit.bounds.size.x / 2f + offsetAddition);
                        }
                        else return true;
                    }
                    
                    return false;
                }
            }
            
            var circleHitCount = Physics2D.OverlapCircleNonAlloc(mousePoint, placementAssistRange, _circleHits, mask);

            if (circleHitCount == 0)
            {
                position = null;
                normal = null;
                return false;
            }

            var rayHit = Physics2D.Raycast(mousePoint, PlayerController.instance.DirectionToClosestPlanet, placementAssistRange, mask);

            if (rayHit)
            {
                position = rayHit.point;
                normal = rayHit.normal;
            }
            else
            {
                var closestCollider = _circleHits[0];
                var currentBoundDistanceToMouse = 0f;

                for (var i = 1; i < circleHitCount; i++)
                {
                    var circleHit = _circleHits[i];
                    var hitBoundDistanceToMouse = Vector2.Distance(mousePoint, circleHit.ClosestPoint(mousePoint));
                    
                    if (hitBoundDistanceToMouse < currentBoundDistanceToMouse)
                    {
                        currentBoundDistanceToMouse = hitBoundDistanceToMouse;
                        closestCollider = circleHit;
                    }
                }
                
                position = closestCollider.ClosestPoint(mousePoint);
                normal = (mousePoint - (Vector3)position).normalized;
            }

            // Prevent placing item inside the terrain
            return Vector3.Distance((Vector3)position, mousePoint) > 0.1f;
        }
    }
}
