using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class UIUtilities : MonoBehaviour
    {
        public delegate void OnMouseRaycastHandler(List<RaycastResult> results);
        public static event OnMouseRaycastHandler OnMouseRaycast;

        private static void TriggerOnMouseRaycast()
        {
            OnMouseRaycast?.Invoke(_results);
        }
        
        private static List<RaycastResult> _results = new();
        
        private void Update()
        {
            MouseRaycast();
        }

        /// <summary>
        /// Get a list of UI elements under the cursor
        /// </summary>
        /// <returns></returns>
        private static void MouseRaycast()
        {
            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
            
            EventSystem.current.RaycastAll(pointerData, _results);
            TriggerOnMouseRaycast();
        }

        public static List<RaycastResult> GetMouseRaycast()
        {
            return _results;
        }
    }
}
