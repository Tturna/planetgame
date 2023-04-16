using System.Collections;
using Entities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CameraScripts
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 defaultCamPosition;
    
        private GameObject _planet;
        private PlayerController _player;

        private void Start()
        {
            _player = PlayerController.instance;
            _player.OnEnteredPlanet += SetTargetPlanet;
        }

        private void LateUpdate()
        {
            if (!_planet) return;

            var trPos = transform.position;
            var dirToPlanet = (_planet.transform.position - trPos).normalized;
            transform.LookAt(trPos + Vector3.forward, -dirToPlanet);
        }

        private void SetTargetPlanet(Planet planet)
        {
            _planet = planet.gameObject;
        }

        public void CameraShake(float time, float strength)
        {
            StartCoroutine(_CameraShake(time, strength));
        }

        private IEnumerator _CameraShake(float time, float strength)
        {
            while (time > 0f)
            {
                var rnd = Random.insideUnitCircle * strength;
                transform.localPosition =  defaultCamPosition + (Vector3)rnd;
                time -= Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
