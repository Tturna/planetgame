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
                Debug.Log("Can't place that far away!");
                return false;
            }

            var relativeUp = useParameters.playerObject.transform.up;

            const float placementAssistRange = 1f;

            var mask = GameUtilities.BasicMovementCollisionMask;
            var circleHit = Physics2D.OverlapCircle(mousePoint, placementAssistRange, mask);

            if (!circleHit)
            {
                return false;
            }

            var rayHit = Physics2D.Raycast(mousePoint, -relativeUp, placementAssistRange, mask);

            if (!rayHit) return false;
            
            if (Vector3.Distance(rayHit.point, mousePoint) < 0.1f)
            {
                return false;
            }
            
            PlaceableSo placeable;
            GameObject prefab;
            var isCraftingStation = useParameters.attackItem.itemSo is CraftingStationSo;
            
            if (isCraftingStation)
            {
                var craftingStationSo = (CraftingStationSo)useParameters.attackItem.itemSo;
                placeable = craftingStationSo;
                prefab = InventoryManager.instance.craftingStationPrefab;
            }
            else
            {
                var placeableSo = (PlaceableSo)useParameters.attackItem.itemSo;
                placeable = placeableSo;
                prefab = InventoryManager.instance.breakablePrefab;
            }
            
            var placeableObject = Object.Instantiate(prefab);

            if (isCraftingStation)
            {
                placeableObject.GetComponent<CraftingStation>().SetRecipes(((CraftingStationSo)placeable).recipes);
            }
            
            var sr = placeableObject.GetComponent<SpriteRenderer>();
            sr.sprite = placeable.sprite;
            
            var col = placeableObject.GetComponent<BoxCollider2D>();
            col.size = placeable.sprite.bounds.size;
            
            var breakableItemInstance = placeableObject.GetComponent<BreakableItemInstance>();
            breakableItemInstance.itemSo = placeable;
            breakableItemInstance.toughness = placeable.toughness;
            
            placeableObject.transform.position = rayHit.point + rayHit.normal * (sr.bounds.size.y * .5f);
            placeableObject.transform.up = rayHit.normal;
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