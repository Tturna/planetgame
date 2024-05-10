using System;
using System.Collections;
using System.Linq;
using Planets;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities
{
    public class GameUtilities : MonoBehaviour
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private RawImage terrainRenderImage; // "Canvas - Camera" -> "TerrainRender"
        
        private static Camera _mainCam;
        private static PlanetGenerator[] _allPlanets;
        
        public static GameUtilities instance;

        private void Awake()
        {
            instance = this;
            _mainCam = Camera.main;
        }

        public void DelayExecute(Action action, float delay)
        {
            StartCoroutine(DelayExec(action, delay));
        }

        private IEnumerator DelayExec(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action.Invoke();
        }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Vector3 eulerAngles, Transform parent)
        {
            var thing = Instantiate(prefab, parent);
            thing.transform.position = position;
            thing.transform.eulerAngles = eulerAngles;

            return thing;
        }
        
        public static float Lerp(float a, float b, float v)
        {
            return a + (b - a) * v;
        }
        
        public static float InverseLerp(float a, float b, float v)
        {
            return b - a == 0 ? 0f : Mathf.Clamp01((v - a) / (b - a));
        }

        public static float Remap(float oa, float ob, float na, float nb, float v)
        {
            var t = InverseLerp(oa, ob, v);
            return Mathf.Lerp(na, nb, t);
        }

        /// <summary>
        /// Gets position difference between the mouse cursor and the given position in world coordinates.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Vector3 GetVectorToWorldCursor(Vector3 position)
        {
            return _mainCam.ScreenToWorldPoint(Input.mousePosition) - position;
        }
        
        // TODO: Maybe this function could be better?
        // Like maybe require an origin position and find the mouse direction automatically???
        public static float GetCursorAngle(Vector3 directionToMouse, Vector3 relativeRightDirection)
        {
            return Vector3.Angle(relativeRightDirection.normalized, directionToMouse);
        }

        // TODO: figure out if this function actually makes any sense to exist
        public GameObject GetProjectilePrefab()
        {
            return projectilePrefab;
        }
        
        public static Material GetTerrainMaterial()
        {
            return instance.terrainRenderImage.material;
        }

        public static PlanetGenerator[] GetAllPlanets()
        {
            if (_allPlanets is { Length: > 0 })
            {
                return _allPlanets;
            }

            _allPlanets = FindObjectsOfType<PlanetGenerator>();
            return _allPlanets;
        }
    }
}
