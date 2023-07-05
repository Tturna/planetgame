using System.Collections.Generic;
using Entities.Entities;
using Planets;
using UnityEngine;

namespace VFX
{
    public class ParallaxManager : MonoBehaviour
    {
        [SerializeField] private float[] layerParallaxSpeeds = { 0f, 0f, 0f, 0f };
        
        private Transform[] _layerParents;
        private List<KeyValuePair<GameObject, PlanetDecorator.DecorOptions>> _updatingDecorObjects;
        private Transform _currentPlanetTransform;
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
            if (_layerParents == null || _layerParents.Length == 0)
            {
                Debug.LogWarning("Layer parent list empty. Current planet is probably not set.");
                return;
            }
            
#region Rotate Layers
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
                pTr.Rotate(Vector3.forward, diff * layerParallaxSpeeds[i - 1]);
            }
#endregion

#region Move Updating Decor

            foreach (var updatingDecor in _updatingDecorObjects)
            {
                var options = updatingDecor.Value;
                var decor = updatingDecor.Key;

                if (options.move)
                {
                    var decPos = decor.transform.position;
                    var dirToPlanet = (_currentPlanetTransform.position - decPos).normalized;
                    decor.transform.LookAt(decPos + Vector3.forward, -dirToPlanet);
                    
                    // TODO: Random speed? Would be cool for birds but could fuck up other shit
                    decor.transform.Translate(Vector3.right * (Time.deltaTime * 0.7f));
                }

                if (options.animate)
                {
                    // TODO: optimize to not use GetComponent and to not get array item every frame
                    var sr = decor.GetComponent<SpriteRenderer>();
                    var num = (int)(Time.time * 2f % 2);
                    sr.sprite = options.spritePool[num];
                    sr.flipX = true;
                }
            }

#endregion    
            
        }

        private void OnPlanetEntered(GameObject planet)
        {
            _currentPlanetTransform = planet.transform;
            (_layerParents, _updatingDecorObjects) = planet.GetComponent<PlanetDecorator>().GetDecorData();
        }
    }
}
