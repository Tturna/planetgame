using System;
using System.Collections.Generic;
using Entities;
using Planets;
using UnityEngine;

namespace VFX
{
    public class ParallaxManager : MonoBehaviour
    {
        private struct UpdatingDecorObject
        {
            public GameObject decor;
            public PlanetDecorator.DecorOptions options;
            public SpriteRenderer sr;
        }
        
        [SerializeField] private float[] layerParallaxSpeeds = { 0f, 0f, 0f, 0f };
        
        private Transform[] _layerParents;
        private List<UpdatingDecorObject> _updatingDecorObjects;
        private MeshRenderer bgTerrainFgRenderer, bgTerrainMgRenderer;
        private Transform _currentPlanetTransform;
        private PlayerController _player;
        private float _oldZ;
        
        public static ParallaxManager instance;
        private static readonly int MatPropBrightness = Shader.PropertyToID("_Brightness");

        private void Start()
        {
            instance = this;
            _player = PlayerController.instance;
            _player.OnEnteredPlanet += OnPlanetEntered;
        }

        private void OnDestroy()
        {
            _player.OnEnteredPlanet -= OnPlanetEntered;
        }

        private void Update()
        {
            if (_layerParents == null || _layerParents.Length == 0)
            {
                Debug.LogWarning("Layer parent list empty. Current planet is probably not set.");
                return;
            }
            
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
            
            foreach (var updatingDecor in _updatingDecorObjects)
            {
                var options = updatingDecor.options;
                var decor = updatingDecor.decor;
                var sr = updatingDecor.sr;

                if (options.move)
                {
                    var decPos = decor.transform.position;
                    var dirToPlanet = (_currentPlanetTransform.position - decPos).normalized;
                    decor.transform.LookAt(decPos + Vector3.forward, -dirToPlanet);
                    
                    decor.transform.Translate(Vector3.right * (Time.deltaTime * 0.7f));
                }

                if (options.animate)
                {
                    var num = (int)(Time.time * 2f % 2);
                    sr.sprite = options.spritePool[num];
                    sr.flipX = true;
                }
            }
        }

        private void OnPlanetEntered(GameObject planet)
        {
            _currentPlanetTransform = planet.transform;
            var decorData = planet.GetComponent<PlanetDecorator>().GetDecorData();
            _layerParents = decorData.layerParents;
            var udoDataPairs = decorData.updatingDecorObjects;
            _updatingDecorObjects = new List<UpdatingDecorObject>();
            
            foreach (var (decor, options) in udoDataPairs)
            {
                _updatingDecorObjects.Add(new UpdatingDecorObject
                {
                    decor = decor,
                    options = options,
                    sr = decor.GetComponent<SpriteRenderer>()
                });
            }
            
            // Careful, bg terrains might not exist yet?
            bgTerrainFgRenderer = decorData.bgTerrainFg.GetComponent<MeshRenderer>();
            bgTerrainMgRenderer = decorData.bgTerrainMg.GetComponent<MeshRenderer>();
        }
        
        public static void SetParallaxTerrainBrightness(float brightness)
        {
            if (instance == null ||
                !instance.bgTerrainFgRenderer ||
                !instance.bgTerrainMgRenderer) return;
            
            instance.bgTerrainFgRenderer.material.SetFloat(MatPropBrightness, brightness);
            instance.bgTerrainMgRenderer.material.SetFloat(MatPropBrightness, brightness);
        }
    }
}
