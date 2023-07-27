using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Inventory.Inventory.Item_Logic
{
    public class FlashlightLogic : ItemLogicBase
    {
        private GameObject _flashlightObject;
        
        public override bool UseOnce(UseParameters useParameters)
        {
            if (!_flashlightObject)
            {
                _flashlightObject = new GameObject("Flashlight");
                _flashlightObject.transform.SetParent(useParameters.equippedItemObject.transform.GetChild(0));
                _flashlightObject.transform.localPosition = Vector3.right * .13f; // Maybe should be a variable in the so
                _flashlightObject.transform.localEulerAngles = Vector3.forward * -90f;
                
                var light = _flashlightObject.AddComponent<Light2D>();
                light.intensity = 1.5f;
                light.pointLightOuterRadius = 10f;
                light.pointLightInnerAngle = 35f;
                light.pointLightOuterAngle = 125f;
                light.falloffIntensity = .45f;
                return true;
            }
            
            _flashlightObject.SetActive(!_flashlightObject.activeSelf);
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