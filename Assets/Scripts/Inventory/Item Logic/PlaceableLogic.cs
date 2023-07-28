using System.Reflection;
using System.Linq;
using Entities.Entities;
using Inventory.Inventory.Item_Types;
using Planets;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Inventory.Inventory.Item_Logic
{
    public class PlaceableLogic : ItemLogicBase
    {
        private PlayerController _pc;
        
        public override bool UseOnce(UseParameters useParameters)
        {
            var usableItem = (UsableItemSo)useParameters.attackItem.itemSo;
            var mousePoint = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
            mousePoint.z = 0f;

            if (Vector3.Distance(useParameters.playerObject.transform.position, mousePoint) > usableItem.useRange)
            {
                Debug.Log("Can't place that far away!");
                return false;
            }

            // _pc ??= useParameters.playerObject.GetComponent<PlayerController>();
            var relativeUp = useParameters.playerObject.transform.up;

            const float placementAssistRange = 1f;
            var layerMask = 1 << LayerMask.NameToLayer("Terrain");
            
            var assistRayStart = mousePoint + relativeUp * (placementAssistRange * .5f);
            var hit = Physics2D.Raycast(assistRayStart, -relativeUp, placementAssistRange, layerMask);

            if (hit.collider == null)
            {
                Debug.Log("Can't place in the air!");
                return false;
            }
            
            if (hit.point == (Vector2)mousePoint)
            {
                Debug.Log("Can't place inside terrain!");
                return false;
            }
            
            var placeable = (PlaceableSo)useParameters.attackItem.itemSo;
            var placeableObject = Object.Instantiate(InventoryManager.instance.breakablePrefab, hit.point, Quaternion.identity);
            
            var sr = placeableObject.GetComponent<SpriteRenderer>();
            sr.sprite = placeable.sprite;
            
            var col = placeableObject.GetComponent<BoxCollider2D>();
            col.size = placeable.sprite.bounds.size;
            
            var breakableItemInstance = placeableObject.GetComponent<BreakableItemInstance>();
            breakableItemInstance.itemSo = placeable;
            breakableItemInstance.toughness = placeable.toughness;
            
            placeableObject.transform.Translate(relativeUp * (col.size.y * .5f));
            placeableObject.transform.rotation = useParameters.playerObject.transform.rotation;

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
                var mask = maskLayers.Select(ml => ml.id).ToArray();
                targetSortingLayersField.SetValue(lightComponent, mask);
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