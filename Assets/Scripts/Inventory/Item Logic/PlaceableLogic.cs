using System.Linq;
using System.Reflection;
using Cameras;
using Entities;
using Inventory.Crafting;
using Inventory.Item_SOs;
using Planets;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Utilities;

namespace Inventory.Item_Logic
{
    public class PlaceableLogic : ItemLogicBase
    {
        private PlayerController _pc;
        
        public override bool UseOnce(UseParameters useParameters)
        {
            var usableItem = (UsableItemSo)useParameters.attackItem.itemSo;
            var mousePoint = CameraController.instance.mainCam.ScreenToWorldPoint(Input.mousePosition);
            mousePoint.z = 0f;

            if (Vector3.Distance(useParameters.playerObject.transform.position, mousePoint) > usableItem.useRange)
            {
                return false;
            }

            var canPlace = PlaceableUtility.TryGetPlaceablePosition(mousePoint, usableItem,
                out var placeablePosition, out var placeableNormal);

            if (!canPlace)
            {
                return false;
            }

            var position = (Vector3)placeablePosition!;
            var normal = (Vector3)placeableNormal!;
            
            PlaceableSo placeable;
            GameObject prefab;
            var isCraftingStation = useParameters.attackItem.itemSo is CraftingStationSo;
            var isRoomModule = useParameters.attackItem.itemSo is RoomModuleSo;
            
            if (isCraftingStation)
            {
                var craftingStationSo = (CraftingStationSo)useParameters.attackItem.itemSo;
                placeable = craftingStationSo;
                prefab = InventoryManager.instance.craftingStationPrefab;
            }
            else if (isRoomModule)
            {
                var roomSo = (RoomModuleSo)useParameters.attackItem.itemSo;
                placeable = roomSo;
                prefab = InventoryManager.instance.roomModulePrefabs[roomSo.prefabIndex];
            }
            else
            {
                var placeableSo = (PlaceableSo)useParameters.attackItem.itemSo;
                placeable = placeableSo;
                prefab = InventoryManager.instance.breakablePrefab;
            }
            
            var placeableObject = Object.Instantiate(prefab);
            AudioUtilities.PlayClip(0, 1f);

            if (isCraftingStation)
            {
                placeableObject.GetComponent<CraftingStation>().SetRecipes(((CraftingStationSo)placeable).recipes);
            }

            if (isRoomModule)
            {
                placeableObject.transform.position = position + normal * (((RoomModuleSo)placeable).boundsSize.y * .5f);
                placeableObject.transform.up = normal;
                return true;
            }
            
            var sr = placeableObject.GetComponent<SpriteRenderer>();
            sr.sprite = placeable.sprite;
            
            var col = placeableObject.GetComponent<BoxCollider2D>();
            col.size = placeable.sprite.bounds.size;
            
            var breakableItemInstance = placeableObject.GetComponent<BreakableItemInstance>();
            breakableItemInstance.itemSo = placeable;
            breakableItemInstance.toughness = placeable.toughness;
            
            placeableObject.transform.position = position + normal * (sr.bounds.size.y * .5f);
            placeableObject.transform.up = normal;
            
            // placeableObject.transform.Translate(relativeUp * (col.size.y * .5f));
            // placeableObject.transform.rotation = useParameters.playerObject.transform.rotation;

            foreach (var light in placeable.lights)
            {
                var lightObject = new GameObject("Light");
                lightObject.transform.SetParent(placeableObject.transform);
                lightObject.transform.localPosition = Vector3.zero;
                
                var lightComponent = lightObject.AddComponent<Light2D>();
                lightComponent.color = light.color;
                lightComponent.intensity = light.intensity;
                lightComponent.falloffIntensity = light.falloffStrength;
                lightComponent.pointLightInnerRadius = .5f;
                lightComponent.pointLightOuterRadius = light.range;
                
                // Hacky shit to change the target sorting layers. This is a Unity L AFAIK
                FieldInfo targetSortingLayersField = typeof(Light2D).GetField("m_ApplyToSortingLayers",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var maskLayers = SortingLayer.layers.Where(sl => sl.name != "Background");
                var masks = maskLayers.Select(ml => ml.id).ToArray();
                targetSortingLayersField.SetValue(lightComponent, masks);
            }
            return true;
        }

        public override bool UseContinuous(UseParameters useParameters)
        {
            return false;
        }

        public override bool UseOnceSecondary(UseParameters useParameters)
        {
            return false;
        }

        public override bool UseContinuousSecondary(UseParameters useParameters)
        {
            return false;
        }
    }
}