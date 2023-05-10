using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProcGen
{
    
    public class PlanetDecorator : MonoBehaviour
    {
        [Serializable]
        internal struct DecorOptions
        {
            public Sprite[] spritePool;
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

        public Transform[] BackgroundLayerParents { get; private set; }

        public void SpawnTrees(PlanetGenerator planetGen)
        {
            if (BackgroundLayerParents == null)
            {
                InitParentObjects();
            }
            
            SpawnDecor(planetGen, realTreeOptions);
        }

        public void CreateBackgroundDecorations(PlanetGenerator planetGen)
        {
            if (BackgroundLayerParents == null)
            {
                InitParentObjects();
            }
            
            SpawnDecor(planetGen, fgTreeOptions);
            SpawnDecor(planetGen, mgBushOptions);
            SpawnDecor(planetGen, mgBirdOptions);
            SpawnDecor(planetGen, bgMountainOptions);
            SpawnDecor(planetGen, bgIslandOptions);
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
                decor.transform.SetParent(BackgroundLayerParents[(int)options.layer]);
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

                angle += Random.Range(options.minAngleIncrement, options.maxAngleIncrement);
            }
        }
        
        private void InitParentObjects()
        {
            var parallaxParent = new GameObject("Parallax Background").transform;
            parallaxParent.parent = transform;
            parallaxParent.localPosition = Vector3.zero;

            var arrLength = Enum.GetValues(typeof(BackgroundLayer)).Length;
            BackgroundLayerParents = new Transform[arrLength];
            
            for (var i = 0; i < arrLength; i++)
            {
                var parentTr = new GameObject(Enum.GetName(typeof(BackgroundLayer), i)).transform;
                parentTr.parent = parallaxParent;
                parentTr.localPosition = Vector3.zero;
                BackgroundLayerParents[i] = parentTr;
            }
            // _fgParent = new GameObject("Foreground Parent").transform;
            // _mgParent = new GameObject("Midground Parent").transform;
            // _bgOneParent = new GameObject("Background One Parent").transform;
            // _bgTwoParent = new GameObject("Background Two Parent").transform;
            //
            // var tr = transform;
            // _fgParent.parent = tr;
            // _mgParent.parent = tr;
            // _bgOneParent.parent = tr;
            // _bgTwoParent.parent = tr;
            //
            // _fgParent.transform.localPosition = Vector3.zero;
            // _mgParent.transform.localPosition = Vector3.zero;
            // _bgOneParent.transform.localPosition = Vector3.zero;
            // _bgTwoParent.transform.localPosition = Vector3.zero;
        }
    }
}
