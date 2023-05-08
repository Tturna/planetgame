using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProcGen
{
    [Serializable]
    internal struct DecorOptions
    {
        public Sprite[] spritePool;
        public string objectName;
        public Transform parent;
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
    
    public class PlanetDecorator : MonoBehaviour
    {
        // [SerializeField] private Sprite[] treePool;
        // [SerializeField] private Sprite[] fgTreePool;
        // [SerializeField] private Sprite[] mgBushPool, mgBirdPool;
        // [SerializeField] private Sprite[] bgMountainPool, bgIslandPool;
        // [SerializeField] private float minimumSpawnHeight;
        // [SerializeField] private int treeCount;
        // [SerializeField] private int fgTreeCount;
        // [SerializeField] private int mgBushCount, mgBirdCount;
        // [SerializeField] private int bgMountainCount, bgIslandCount;
        // [SerializeField] private GameObject fgParent, mgParent, bgParent;
        // [SerializeField] private Color fgColor, mgColor, bgColor;
        [SerializeField] private DecorOptions realTreeOptions;
        [SerializeField] private DecorOptions fgTreeOptions;
        [SerializeField] private DecorOptions mgBushOptions, mgBirdOptions;
        [SerializeField] private DecorOptions bgMountainOptions, bgIslandOptions;
        
        public void SpawnTrees(PlanetGenerator planetGen)
        {
            SpawnDecor(planetGen, realTreeOptions);
            // SpawnDecor(planetGen, treePool, "Tree", transform, treeCount, minimumSpawnHeight, 5f, 20f, Color.white);
        }

        public void CreateBackgroundDecorations(PlanetGenerator planetGen)
        {
            // Figure out some density for foreground, midground and background stuff
            // As of now (2023-5-8), foreground is trees, midground is bushes and birds and background is mountains and floating islands
            
            // Do the same thing as with trees, but put stuff into the ground by a random amount.
            // Also put birds and floating islands into the sky.
            SpawnDecor(planetGen, fgTreeOptions);
            SpawnDecor(planetGen, mgBushOptions);
            SpawnDecor(planetGen, mgBirdOptions);
            SpawnDecor(planetGen, bgMountainOptions);
            SpawnDecor(planetGen, bgIslandOptions);
            // SpawnDecor(planetGen, fgTreePool, "fgTree", fgParent.transform, fgTreeCount, 0f, 5f, 20f, fgColor, -.5f, 0f, "Background", 4);
            // SpawnDecor(planetGen, mgBushPool, "mgBush", mgParent.transform, mgBushCount, 0f, 5f, 15f, mgColor, -.5f, 0f, "Background", 3);
            // SpawnDecor(planetGen, mgBirdPool, "mgBird", mgParent.transform, mgBirdCount, 0f, 15f, 40f, mgColor, 4f, 8f, "Background", 3);
            // SpawnDecor(planetGen, bgMountainPool, "bgMountain", bgParent.transform, bgMountainCount, 0f, 10f, 25f, bgColor, -.5f, 0f, "Background", 2);
            // SpawnDecor(planetGen, bgIslandPool, "bgIsland", bgParent.transform, bgIslandCount, 0f, 15f, 30f, bgColor, 3f, 6f, "Background", 2);
        }

        private void SpawnDecor(PlanetGenerator planetGen, DecorOptions options)
        {
            // start at angle 0 and increment by random amounts
            var angle = 0f;
            for (var i = 0; i < options.count; i++)
            {
                // temp
                if (options.objectName == "bgIsland") print($"angle: {angle}");
                
                // Get surface height
                var point = (Vector3)planetGen.GetRelativeSurfacePoint(angle);
                point += transform.position;
                
                var dirToPlanet = (transform.position - point).normalized;
                
                // raycast below each decor object to find the surface point
                var hit = Physics2D.Raycast(point, dirToPlanet);

                if (Vector3.Distance(planetGen.transform.position, hit.point) < options.minSpawnHeight) return;

                // spawn decor objects
                var decor = new GameObject(options.objectName);
                decor.transform.SetParent(options.parent);
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
    }
}
