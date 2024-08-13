using System;
using System.Collections.Generic;
using Inventory.Item_SOs;
using UnityEngine;
using Utilities;
using Random = UnityEngine.Random;

namespace Planets
{
    public class PlanetDecorator : MonoBehaviour
    {
        [Serializable]
        public struct DecorOptions
        {
            public Sprite[] spritePool;
            public bool breakable;
            public int breakableToughness;
            public ItemSo breakableDrop;
            public Vector2 breakableColliderSize;
            public bool animate;
            public bool move;
            public string objectName;
            public BackgroundLayer layer;
            public int count;
            [Range(0f, 1f), Tooltip("Minimum dot product between the surface normal and planet direction for the decor to spawn." +
                                    "1 = normal has to point directly to the sky. 0 = normal can be in any orientation.")]
            public float minNormalDot;
            public float minSpawnHeight;
            public float minAngleIncrement;
            public float maxAngleIncrement;
            public float minHeightOffset;
            public float maxHeightOffset;
            public Color spriteColor;
            public string sortingLayer;
            public int sortingOrder;
        }
        
        public struct DecorData
        {
            public Transform[] layerParents;
            public List<KeyValuePair<GameObject, DecorOptions>> updatingDecorObjects;
            public GameObject bgTerrainFg, bgTerrainMg;
        }
        
        public enum BackgroundLayer { This, Foreground, Midground, BackgroundOne, BackgroundTwo }
        
        // TODO: Make these options into one array
        [SerializeField] private DecorOptions realTreeOptions;
        [SerializeField] private DecorOptions fgTreeOptions;
        [SerializeField] private DecorOptions mgBushOptions, mgBirdOptions;
        [SerializeField] private DecorOptions bgMountainOptions, bgIslandOptions;
        [SerializeField] private DecorOptions flowerOptions, grassOptions, rockOptions, bushOptions;
        [SerializeField] private Material bgTerrainMaterialFg, bgTerrainMaterialMg;
        [SerializeField] private Material styledSpriteMaterial;
        [SerializeField] private GameObject breakablePrefab;

        private Transform[] _backgroundLayerParents;
        private readonly List<KeyValuePair<GameObject, DecorOptions>> _updatingDecorObjects = new();
        private GameObject bgTerrainFg, bgTerrainMg;

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
            SpawnDecor(planetGen, flowerOptions);
            SpawnDecor(planetGen, grassOptions);
            SpawnDecor(planetGen, rockOptions);
            SpawnDecor(planetGen, bushOptions);
        }

        public void CreateBackgroundTerrain(MeshFilter[] meshFilters)
        {
            // TODO: Make the background terrain generation smarter
            
            /* This doesn't work rn. It tries to make a single mesh out of the whole terrain,
             * which is stupid because we only really need a bit of the surface terrain. The background
             * will probably change to some cave wall thing very soon anyway.
             *
             * I wonder if you could just take the terrain camera view and duplicate it
             * but with a flat color or something...
             */
            
            if (_backgroundLayerParents == null)
            {
                InitParentObjects();
            }

            const int meshBundleSize = 64; // how many tiles are combined and optimized
            var combines = new CombineInstance[meshFilters.Length];
            
            // Create meshes so we can combine multiple meshes into one, optimize them and go on.
            // This way we can reduce the amount of vertices as we're combining all the tiles.
            // If we try to combine all the tiles directly into 1, it'll have like over 300k vertices,
            // and Unity starts screaming.
            // var midMeshes = new Mesh[Mathf.CeilToInt(meshFilters.Length / (float)meshBundleSize)];
            //
            // for (var i = 0; i < meshFilters.Length;)
            // {
            //     for (var j = 0; j < meshBundleSize && i < meshFilters.Length; j++, i++)
            //     {
            //         combines[j].mesh = meshFilters[i].mesh;
            //         combines[j].transform = meshFilters[i].transform.localToWorldMatrix;
            //     }
            //     
            //     var index = Mathf.FloorToInt(i / (float)meshBundleSize - 1);
            //     var tMesh = new Mesh();
            //     tMesh.CombineMeshes(combines, true);
            //     
            //     // optimize
            //     var simplifier = new UnityMeshSimplifier.MeshSimplifier();
            //     simplifier.Initialize(tMesh);
            //     simplifier.SimplifyMesh(0.25f);
            //     midMeshes[index] = simplifier.ToMesh();
            // }
            //
            // combines = new CombineInstance[midMeshes.Length];
            //
            for (var i = 0; i < combines.Length; i++)
            {
                combines[i].mesh = meshFilters[i].mesh;
                combines[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }
            
            var mesh = new Mesh();
            mesh.CombineMeshes(combines, true);
            
            // mesh.Optimize();
            var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
            meshSimplifier.Initialize(mesh);
            
            meshSimplifier.SimplifyMesh(.35f);

            bgTerrainFg = new GameObject("bgTerrain");
            bgTerrainMg = new GameObject("bgTerrain2");
            bgTerrainFg.transform.parent = _backgroundLayerParents![1];
            bgTerrainMg.transform.parent = _backgroundLayerParents![2];
            
            var pos = bgTerrainFg.transform.position;
            pos.z = 1f;
            bgTerrainFg.transform.position = pos;
            pos.z = 1.5f;
            bgTerrainMg.transform.position = pos;

            var meshRendererFg = bgTerrainFg.AddComponent<MeshRenderer>();
            var meshRendererMg = bgTerrainMg.AddComponent<MeshRenderer>();
            meshRendererFg.material = bgTerrainMaterialFg;
            meshRendererMg.material = bgTerrainMaterialMg;
            
            var meshFilterFg = bgTerrainFg.AddComponent<MeshFilter>();
            var meshFilterMg = bgTerrainMg.AddComponent<MeshFilter>();
            meshFilterFg.sharedMesh = meshSimplifier.ToMesh();
            meshFilterMg.sharedMesh = meshFilterFg.sharedMesh;
            print($"Mesh vertices: { meshFilterFg.mesh.vertices.Length }");
        }

        public DecorData GetDecorData()
        {
            var decorData = new DecorData
            {
                layerParents = _backgroundLayerParents,
                updatingDecorObjects = _updatingDecorObjects,
                bgTerrainFg = bgTerrainFg,
                bgTerrainMg = bgTerrainMg
            };

            return decorData;
        }
        
        private void SpawnDecor(PlanetGenerator planetGen, DecorOptions options)
        {
            var angle = Random.Range(0f, 360f);
            
            for (var i = 0; i < options.count; i++)
            {
                angle += Random.Range(options.minAngleIncrement, options.maxAngleIncrement);
                angle %= 360;
                
                // Get surface height
                var point = (Vector3)planetGen.GetRelativeSurfacePoint(angle);
                point += transform.position;
                
                var dirToPlanet = (transform.position - point).normalized;
                
                // raycast below each decor object to find the surface point
                var mask = GameUtilities.BasicMovementCollisionMask;
                var hit = Physics2D.Raycast(point, dirToPlanet, 100, mask);
                
                var normalDot = Vector3.Dot(hit.normal, -dirToPlanet);

                if (normalDot < options.minNormalDot) continue;

                if (Vector3.Distance(planetGen.transform.position, hit.point) < options.minSpawnHeight) continue;

                GameObject decor;
                SpriteRenderer sr;
                
                if (options.breakable)
                {
                    decor = Instantiate(breakablePrefab);
                    sr = decor.GetComponent<SpriteRenderer>();
                    
                    var boxCollider = decor.GetComponent<BoxCollider2D>();
                    boxCollider.size = options.breakableColliderSize;
                    boxCollider.offset = Vector2.up * boxCollider.size.y / 2f;
                    
                    var breakable = decor.GetComponent<BreakableItemInstance>();
                    breakable.toughness = options.breakableToughness;
                    decor.tag = "Breakable";

                    if (options.breakableDrop)
                    {
                        breakable.itemSo = options.breakableDrop;
                    }
                }
                else
                {
                    decor = new GameObject();
                    sr = decor.AddComponent<SpriteRenderer>();
                    decor.tag = tag;
                }
                
                decor.name = options.objectName;
                decor.transform.SetParent(_backgroundLayerParents[(int)options.layer]);
                
                sr.sprite = options.spritePool[Random.Range(0, options.spritePool.Length)];
                sr.color = options.spriteColor;
                sr.material = styledSpriteMaterial;

                if (options.sortingLayer != "")
                {
                    sr.sortingLayerName = options.sortingLayer;
                    sr.sortingOrder = options.sortingOrder;
                }
                
                var weightedUpDirection = ((Vector3)hit.normal - dirToPlanet * 2) * 0.5f;
                
                // This assumes that the sprites have their pivot set to bottom center.
                // Normally this would set the center of the sprite to the surface level, burying the sprite.
                decor.transform.position = (Vector3)hit.point - dirToPlanet * Random.Range(options.minHeightOffset, options.maxHeightOffset);
                decor.transform.LookAt(decor.transform.position + Vector3.forward, weightedUpDirection);

                var pos = decor.transform.localPosition;
                pos.z = 0;
                decor.transform.localPosition = pos;

                if (options.animate || options.move)
                {
                    var entry = new KeyValuePair<GameObject, DecorOptions>(decor, options);
                    _updatingDecorObjects.Add(entry);
                }
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
                parentTr.localPosition = i > 1 ? Vector3.forward * 2 : Vector3.zero;
                _backgroundLayerParents[i] = parentTr;
            }
        }
    }
}
