using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Utilities
{
    public class UIUtilities : MonoBehaviour
    {
        public delegate void OnMouseRaycastHandler(List<RaycastResult> results);
        public static event OnMouseRaycastHandler OnMouseRaycast;

        private static void TriggerOnMouseRaycast()
        {
            OnMouseRaycast?.Invoke(Results);
        }
        
        private static readonly List<RaycastResult> Results = new();
        
        private void Update()
        {
            MouseRaycast();
            
            if (Results.Count > 0)
            {
                Debug.Log(Results[0].gameObject.name);
            }
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
            
            EventSystem.current.RaycastAll(pointerData, Results);
            TriggerOnMouseRaycast();
        }

        public static List<RaycastResult> GetMouseRaycast()
        {
            return Results;
        }
        
        public static bool IsMouseOverUI()
        {
            return Results.Count > 0;
        }
    }
}
