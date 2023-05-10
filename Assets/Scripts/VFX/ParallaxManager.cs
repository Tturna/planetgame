using Entities;
using ProcGen;
using UnityEngine;

namespace VFX
{
    public class ParallaxManager : MonoBehaviour
    {
        private Transform[] _layerParents;
        private PlayerController _player;
        private float _oldZ;

        private void Start()
        {
            _player = PlayerController.instance;
            _player.OnEnteredPlanet += OnPlanetEntered;
        }

        // Update is called once per frame
        private void Update()
        {
            // Rotate all the layers depending on player rotation

            var z = _player.transform.eulerAngles.z;
            var diff = z - _oldZ;
            _oldZ = z;
            
            switch (diff)
            {
                case > 350:
                    diff -= 360;
                    break;
                case < -350:
                    diff += 360;
                    break;
            }

            // Ignore first parent since it's the planet itself
            for (var i = 1; i < _layerParents.Length; i++)
            {
                var pTr = _layerParents[i];
                pTr.Rotate(Vector3.forward, diff * (i * .15f));
            }
        }

        private void OnPlanetEntered(Planet planet)
        {
            _layerParents = planet.GetComponent<PlanetDecorator>().BackgroundLayerParents;
        }
    }
}
