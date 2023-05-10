using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProcGen
{
    
    public class PlanetDecorator : MonoBehaviour
    {
        [Serializable]
        public struct DecorOptions
        {
            public Sprite[] spritePool;
            public bool animate;
            public bool move;
            public string objectName;
            // public Transform parent;
            public BackgroundLayer layer;
            public int count;
            public float minSpawnHeight;
            public float minAngleIncrement;
            public float maxAngleIncrement;
            public float minHeightOffset;
            public float maxHeightOffset;
            public Color spriteColor;
            public string sortingLayer;
            public int sortingOrder;
        }
        
        public enum BackgroundLayer { This, Foreground, Midground, BackgroundOne, BackgroundTwo }
        
        [SerializeField] private DecorOptions realTreeOptions;
        [SerializeField] private DecorOptions fgTreeOptions;
        [SerializeField] private DecorOptions mgBushOptions, mgBirdOptions;
        [SerializeField] private DecorOptions bgMountainOptions, bgIslandOptions;

        private Transform[] _backgroundLayerParents;
        private List<KeyValuePair<GameObject, DecorOptions>> _updatingDecorObjects = new();

        public void SpawnTrees(PlanetGenerator planetGen)
        {
            if (_backgroundLayerParents == null)
            {
                InitParentObjects();
            }
            
            SpawnDecor(planetGen, realTreeOptions);
        }

        public void CreateBackgroundDecorations(PlanetGenerator planetGen)
        {
            if (_backgroundLayerParents == null)
            {
                InitParentObjects();
            }
            
            SpawnDecor(planetGen, fgTreeOptions);
            SpawnDecor(planetGen, mgBushOptions);
            SpawnDecor(planetGen, mgBirdOptions);
            SpawnDecor(planetGen, bgMountainOptions);
            SpawnDecor(planetGen, bgIslandOptions);
        }

        public (Transform[], List<KeyValuePair<GameObject, DecorOptions>>) GetDecorData()
        {
            return (_backgroundLayerParents, _updatingDecorObjects);
        }

        private void SpawnDecor(PlanetGenerator planetGen, DecorOptions options)
        {
            // start at angle 0 and increment by random amounts
            var angle = 0f;
            for (var i = 0; i < options.count; i++)
            {
                // Get surface height
                var point = (Vector3)planetGen.GetRelativeSurfacePoint(angle);
                point += transform.position;
                
                var dirToPlanet = (transform.position - point).normalized;
                
                // raycast below each decor object to find the surface point
                var hit = Physics2D.Raycast(point, dirToPlanet);

                if (Vector3.Distance(planetGen.transform.position, hit.point) < options.minSpawnHeight) return;

                // spawn decor objects
                var decor = new GameObject(options.objectName);
                decor.transform.SetParent(_backgroundLayerParents[(int)options.layer]);
                
                var sr = decor.AddComponent<SpriteRenderer>();
                sr.sprite = options.spritePool[Random.Range(0, options.spritePool.Length)];
                sr.color = options.spriteColor;

                if (options.sortingLayer != "")
                {
                    sr.sortingLayerName = options.sortingLayer;
                    sr.sortingOrder = options.sortingOrder;
                }
                
                // This assumes that the sprites have their pivot set to bottom center.
                // Normally this would set the center of the sprite to the surface level, burying ths sprite.
                decor.transform.position = (Vector3)hit.point - dirToPlanet * Random.Range(options.minHeightOffset, options.maxHeightOffset);
                decor.transform.LookAt(decor.transform.position + Vector3.forward, -dirToPlanet);

                if (options.animate || options.move)
                {
                    var entry = new KeyValuePair<GameObject, DecorOptions>(decor, options);
                    _updatingDecorObjects.Add(entry);
                }
                
                angle += Random.Range(options.minAngleIncrement, options.maxAngleIncrement);
            }
        }
        
        private void InitParentObjects()
        {
            var parallaxParent = new GameObject("Parallax Background").transform;
            parallaxParent.parent = transform;
            parallaxParent.localPosition = Vector3.zero;

            var arrLength = Enum.GetValues(typeof(BackgroundLayer)).Length;
            _backgroundLayerParents = new Transform[arrLength];
            
            for (var i = 0; i < arrLength; i++)
            {
                var parentTr = new GameObject(Enum.GetName(typeof(BackgroundLayer), i)).transform;
                parentTr.parent = parallaxParent;
                parentTr.localPosition = Vector3.zero;
                _backgroundLayerParents[i] = parentTr;
            }
        }
    }
}
