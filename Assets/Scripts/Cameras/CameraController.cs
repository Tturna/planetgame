using System.Collections;
using Entities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Cameras
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform bgImagesParent;
    
        public static CameraController instance;
        public static float zoomMultiplier = 1f;
        public Camera mainCam;
        private TerrainCameraController[] _terrainCameraControllers;
        private Transform _planetTransform;
        private Vector3 _defaultCamPosition;
        private Vector3 _usedDefaultCamPosition;
        private float _defaultMainCamZoom;
        private float _defaultTerrainCamZoom;
        private Coroutine _currentZoomCoroutine;

        private void Awake()
        {
            instance = this;
            mainCam = Camera.main!;
            _defaultCamPosition = transform.localPosition;
            _usedDefaultCamPosition = _defaultCamPosition;
            _defaultMainCamZoom = mainCam.orthographicSize;
        }

        private void Start()
        {
            _terrainCameraControllers = FindObjectsOfType<TerrainCameraController>();
            _defaultTerrainCamZoom = _terrainCameraControllers[0].Camera.orthographicSize;
            
            PlayerController.instance.OnEnteredPlanet += SetTargetPlanet;
        }

        private void LateUpdate()
        {
            if (!_planetTransform) return;

            var trPos = transform.position;
            var dirToPlanet = (_planetTransform.transform.position - trPos).normalized;
            transform.LookAt(trPos + Vector3.forward, -dirToPlanet);
        }

        private void SetTargetPlanet(GameObject planet)
        {
            _planetTransform = planet.transform;
        }

        public static void CameraShake(float time, float strength)
        {
            instance.StartCoroutine(_CameraShake(time, strength));
        }
        
        public static void SetParent(Transform parent)
        {
            instance.transform.SetParent(parent);
            foreach (var tcc in instance._terrainCameraControllers)
            {
                tcc.transform.SetParent(parent);
            }
        }
        
        public static void SetDefaultPosition(Vector2 position)
        {
            var newDefaultPos = (Vector3)position;
            newDefaultPos.z = instance._defaultCamPosition.z;
            instance._usedDefaultCamPosition = newDefaultPos;
            instance.transform.localPosition = newDefaultPos;
        }
        
        public static void ResetDefaultPosition()
        {
            instance._usedDefaultCamPosition = instance._defaultCamPosition;
            instance.transform.localPosition = instance._defaultCamPosition;
        }
        
        public static void SetZoomMultiplierSmooth(float targetZoomMultiplier, float smoothTime)
        {
            if (instance._currentZoomCoroutine != null)
            {
                instance.StopCoroutine(instance._currentZoomCoroutine);
            }
            
            instance._currentZoomCoroutine = instance.StartCoroutine(_SetZoomMultiplierSmooth(targetZoomMultiplier, smoothTime));
        }
        
        private static IEnumerator _SetZoomMultiplierSmooth(float targetZoomMultiplier, float lerpTime)
        {
            var timer = 0f;
            var startZoomMultiplier = zoomMultiplier;

            while (timer < lerpTime)
            {
                timer += Time.deltaTime;
                
                var normalTime = timer / lerpTime;
                var easeValue = normalTime < 0.5f
                    ? 4 * normalTime * normalTime * normalTime
                    : 1 - Mathf.Pow(-2 * normalTime + 2, 3) / 2;
                var newZoomMultiplier = Mathf.Lerp(startZoomMultiplier, targetZoomMultiplier, easeValue);
                SetZoomMultiplier(newZoomMultiplier);
                
                yield return new WaitForEndOfFrame();
            }
            
            SetZoomMultiplier(targetZoomMultiplier);
        }

        private static void SetZoomMultiplier(float targetZoomMultiplier)
        {
            zoomMultiplier = targetZoomMultiplier;
            instance.mainCam.orthographicSize = instance._defaultMainCamZoom * targetZoomMultiplier;
            
            foreach (var tcc in instance._terrainCameraControllers)
            {
                tcc.Camera.orthographicSize = instance._defaultTerrainCamZoom * targetZoomMultiplier;
            }
            
            instance.bgImagesParent.localScale = Vector3.one * targetZoomMultiplier;
        }

        private static IEnumerator _CameraShake(float time, float strength)
        {
            while (time > 0f)
            {
                var rnd = Random.insideUnitCircle * strength;
                instance.transform.localPosition =  instance._usedDefaultCamPosition + (Vector3)rnd;
                time -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            
            instance.transform.localPosition = instance._usedDefaultCamPosition;
        }
    }
}
