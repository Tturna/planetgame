using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Entities
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 defaultCamPosition;
    
        public static CameraController instance;
        private Transform _planetTransform;

        private void Start()
        {
            instance = this;
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

        private static IEnumerator _CameraShake(float time, float strength)
        {
            while (time > 0f)
            {
                var rnd = Random.insideUnitCircle * strength;
                instance.transform.localPosition =  instance.defaultCamPosition + (Vector3)rnd;
                time -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
