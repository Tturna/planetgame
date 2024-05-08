using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Entities
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 defaultCamPosition;
    
        public static CameraController instance;
        private TerrainCameraController[] _terrainCameraControllers;
        private Transform _planetTransform;
        private Vector3 _usedDefaultCamPosition;

        private void Start()
        {
            instance = this;
            _usedDefaultCamPosition = defaultCamPosition;
            _terrainCameraControllers = FindObjectsOfType<TerrainCameraController>();
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
        
        public static void SetCameraParent(Transform parent)
        {
            instance.transform.SetParent(parent);
            foreach (var tcc in instance._terrainCameraControllers)
            {
                tcc.transform.SetParent(parent);
            }
        }
        
        public static void SetDefaultCameraPosition(Vector2 position)
        {
            var newDefaultPos = (Vector3)position;
            newDefaultPos.z = instance.defaultCamPosition.z;
            instance._usedDefaultCamPosition = newDefaultPos;
            instance.transform.localPosition = newDefaultPos;
        }
        
        public static void ResetDefaultCameraPosition()
        {
            instance._usedDefaultCamPosition = instance.defaultCamPosition;
            instance.transform.localPosition = instance.defaultCamPosition;
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
