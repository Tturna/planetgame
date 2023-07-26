// This script populates a planet with ore veins. Ore veins are generated using
// the Poisson-Disc sampling algorithm to keep ore chunks from overlapping.
// Implemented by following this video:
// https://www.youtube.com/watch?v=flQgnCUxHlw

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Planets
{
    public class PlanetOrePopulator : MonoBehaviour
    {
        [Serializable]
        private struct VeinType
        {
            public ScriptableObject oreSo;
            [Range(0f, 1f)] public float oreSpawnHeight;
            public float veinDiameter;
            public int oreToughness;
            
            public float chunkSampleGapSmall;
            [FormerlySerializedAs("chunkSampleGap")] public float chunkSampleGapMedium;
            public float chunkSampleGapLarge;
            
            public Sprite[] oreSpritesSmall;
            [FormerlySerializedAs("oreSprites")] public Sprite[] oreSpritesMedium;
            public Sprite[] oreSpritesLarge;
        }
        
        [SerializeField] private GameObject orePrefab;
        [SerializeField] private VeinType[] veinTypes;
        
        [Header("Shit to be removed probably")]
        [SerializeField, Range(0.1f, 1f)] private float oreSpawnHeight;
        
        [SerializeField] private float veinSampleGap;
        [SerializeField] private int veinSampleGenAttempts;
        
        private Transform _oreParentTransform;
        private PlanetGenerator _planetGen;
        
        public void GenerateVeins(Transform terrainParent)
        {
            _planetGen = GetComponent<PlanetGenerator>();
            
            var oreParentObject = new GameObject("Ore Parent");
            _oreParentTransform = oreParentObject.transform;
            _oreParentTransform.parent = terrainParent;
            _oreParentTransform.localPosition = Vector3.zero;

            var oreSpawnDiameter = _planetGen.diameter * oreSpawnHeight;
            var veinGrid = PoissonDisc(oreSpawnDiameter, veinSampleGap, veinSampleGenAttempts);

            foreach (var vein in veinGrid)
            {
                if (vein == null) continue;
                var veinPos = (Vector2)vein;
                var corVeinPos = veinPos - Vector2.one * oreSpawnDiameter * .5f;
                
                if (corVeinPos.magnitude > oreSpawnDiameter * .5f) continue;
                
                var veinParent = new GameObject("Vein Parent");
                var veinParentTransform = veinParent.transform;
                veinParentTransform.parent = _oreParentTransform;
                veinParentTransform.localPosition = corVeinPos;

                // TODO: Each vein type should have a rarity
                var veinType = veinTypes[Random.Range(0, veinTypes.Length)];

                GenerateOreChunks(veinParentTransform.position, veinParentTransform, veinType);
            }
        }

        // TODO: re-learn enumerators because I guess you should use one here?
        private static Vector2?[] PoissonDisc(float areaWidth, float sampleGap, int sampleGenAttempts)
        {
            var cellEdgeLength = sampleGap / Mathf.Sqrt(2);
            var cols = Mathf.FloorToInt(areaWidth / cellEdgeLength);
            var grid = new Vector2?[cols * cols];
            List<Vector2> active = new();
            
            for (var i = 0; i < cols * cols; i++)
            {
                grid[i] = null;
            }

            var center = Vector2.one * areaWidth * .5f;
            
            var centerCellIndices = Vector2Int.FloorToInt(center / cellEdgeLength);
            grid[centerCellIndices.x + centerCellIndices.y * cols] = center;
            
            active.Add(center);

            while (active.Count > 0)
            {
                var rIndex = Random.Range(0, active.Count);
                
                // Attempt to create new points in valid locations k times.
                // This is done by randomly generating points around existing points and checking if they're too close
                // to other existing points.
                var validFound = false;
                for (var i = 0; i < sampleGenAttempts; i++)
                {
                    var offset = Random.insideUnitCircle.normalized * Random.Range(sampleGap, 2 * sampleGap);
                    var sample = active[rIndex] + offset;
                    var xy = Vector2Int.FloorToInt(sample / cellEdgeLength);
                    
                    if (xy.x < 0 || xy.x >= cols) continue;
                    if (xy.y < 0 || xy.y >= cols) continue;
                    
                    if (grid[xy.x + xy.y * cols] != null) continue;

                    // Check if new sample is too close to existing points by comparing against neighboring cells in the grid.
                    var valid = true;
                    for (var x = -1; x <= 1; x++)
                    {
                        for (var y = -1; y <= 1; y++)
                        {
                            if (xy.x + x < 0 || xy.x + x >= cols) continue;
                            if (xy.y + y < 0 || xy.y + y >= cols) continue;
                            
                            var n = grid[xy.x + x + (xy.y + y) * cols];
                            if (n == null) continue;
                            var neighbor = (Vector2)n;
                            
                            var dist = Vector2.Distance(sample, neighbor);

                            if (dist < sampleGap)
                            {
                                valid = false;
                            }
                        }
                    }
                    
                    if (valid)
                    {
                        validFound = true;
                        grid[xy.x + xy.y * cols] = sample;
                        active.Add(sample);
                    }
                }
                
                if (!validFound)
                {
                    active.RemoveAt(rIndex);
                }
            }

            return grid;
        }

        private static (int oreSize, float sampleGap) GetRandomOreValues(VeinType veinType)
        {
            var (oreSize, sampleGap) = (-1, 0f);
            var rng = Random.Range(0, 100);

            if (veinType.oreSpritesMedium?.Length > 0)
            {
                (oreSize, sampleGap) = (1, veinType.chunkSampleGapMedium);
                if (rng < 50) return (oreSize, sampleGap);
            }
            
            if (veinType.oreSpritesSmall?.Length > 0)
            {
                (oreSize, sampleGap) = (0, veinType.chunkSampleGapSmall);
                if (rng < 85) return (oreSize, sampleGap);
            }
            
            if (veinType.oreSpritesLarge?.Length > 0)
            {
                (oreSize, sampleGap) = (2, veinType.chunkSampleGapLarge);
                if (rng >= 85) return (oreSize, sampleGap);
            }

            if (oreSize == -1) throw new Exception("No ore sprites found.");
            
            return (oreSize, sampleGap);

        }
        
        private static KeyValuePair<Vector2?, int>[] PoissonDiscOres(VeinType veinType, int sampleGenAttempts)
        {
            var areaWidth = veinType.veinDiameter;
            var cellEdgeLength = veinType.chunkSampleGapMedium / Mathf.Sqrt(2);
            var cols = Mathf.FloorToInt(areaWidth / cellEdgeLength);
            var grid = new KeyValuePair<Vector2?, int>[cols * cols];
            List<KeyValuePair<Vector2, int>> active = new();
            
            for (var i = 0; i < cols * cols; i++)
            {
                grid[i] = new KeyValuePair<Vector2?, int>(null, -1);
            }

            var center = Vector2.one * areaWidth * .5f;
            
            var centerCellIndices = Vector2Int.FloorToInt(center / cellEdgeLength);
            grid[centerCellIndices.x + centerCellIndices.y * cols] = new KeyValuePair<Vector2?, int>(center, 1);
            
            active.Add(new KeyValuePair<Vector2, int>(center, 1));

            while (active.Count > 0)
            {
                var rIndex = Random.Range(0, active.Count);

                var (oreSize, sampleGap) = GetRandomOreValues(veinType);
                var aMinGap = active[rIndex].Value switch
                {
                    0 => veinType.chunkSampleGapSmall,
                    1 => veinType.chunkSampleGapMedium,
                    2 => veinType.chunkSampleGapLarge,
                    _ => throw new ArgumentOutOfRangeException()
                };

                var minGap = aMinGap + sampleGap;

                // Attempt to create new points in valid locations k times.
                // This is done by randomly generating points around existing points and checking if they're too close
                // to other existing points.
                var validFound = false;
                for (var i = 0; i < sampleGenAttempts; i++)
                {
                    var offset = Random.insideUnitCircle.normalized * Random.Range(minGap, minGap + sampleGap);
                    var sample = active[rIndex].Key + offset;
                    var xy = Vector2Int.FloorToInt(sample / cellEdgeLength);
                    
                    if (xy.x < 0 || xy.x >= cols) continue;
                    if (xy.y < 0 || xy.y >= cols) continue;
                    
                    if (grid[xy.x + xy.y * cols].Key != null) continue;

                    // Check if new sample is too close to existing points by comparing against neighboring cells in the grid.
                    var valid = true;
                    for (var x = -1; x <= 1; x++)
                    {
                        for (var y = -1; y <= 1; y++)
                        {
                            if (xy.x + x < 0 || xy.x + x >= cols) continue;
                            if (xy.y + y < 0 || xy.y + y >= cols) continue;
                            
                            var n = grid[xy.x + x + (xy.y + y) * cols];
                            if (n.Key == null) continue;
                            var neighbor = (Vector2)n.Key;
                            
                            var dist = Vector2.Distance(sample, neighbor);
                            var nMinGap = n.Value switch
                            {
                                0 => veinType.chunkSampleGapSmall,
                                1 => veinType.chunkSampleGapMedium,
                                2 => veinType.chunkSampleGapLarge,
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            var minDist = nMinGap + sampleGap;

                            if (dist < minDist)
                            {
                                valid = false;
                            }
                        }
                    }
                    
                    if (valid)
                    {
                        validFound = true;
                        grid[xy.x + xy.y * cols] = new KeyValuePair<Vector2?, int>(sample, oreSize);
                        active.Add(new KeyValuePair<Vector2, int>(sample, oreSize));
                    }
                }
                
                if (!validFound)
                {
                    active.RemoveAt(rIndex);
                }
            }

            return grid;
        }

        private void GenerateOreChunks(Vector2 veinCenter, Transform veinParentTransform, VeinType veinType)
        {
            const int genAttempts = 15;
            var areaWidth = veinType.veinDiameter;
            var veinRadius = areaWidth / 2;
            var veinArea = Mathf.PI * veinRadius * veinRadius;
            var mediumRadius = veinType.chunkSampleGapMedium / 2;
            var mediumArea = Mathf.PI * mediumRadius * mediumRadius;
            var maxOresEstimate = Mathf.CeilToInt(veinArea / mediumArea);
            
            var active = new List<KeyValuePair<Vector2, int>> { new(Vector2.zero, 1) };

            for (var i = 0; i < maxOresEstimate && active.Count > 0; i++)
            {
                var (oreSize, sampleGap) = GetRandomOreValues(veinType);
                var rIndex = Random.Range(0, active.Count);
                var aMinGap = active[rIndex].Value switch
                {
                    0 => veinType.chunkSampleGapSmall,
                    1 => veinType.chunkSampleGapMedium,
                    2 => veinType.chunkSampleGapLarge,
                    _ => throw new ArgumentOutOfRangeException()
                };

                for (var n = 0; n < genAttempts; n++)
                {
                    var offset = aMinGap + sampleGap;
                    var sample = active[rIndex].Key + Random.insideUnitCircle.normalized * offset;

                    var results = new Collider2D[15];
                    var resCount = Physics2D.OverlapCircleNonAlloc(veinCenter + sample, sampleGap, results, 1 << LayerMask.NameToLayer("TerrainBits"));
                    
                    var valid = true;
                    for (var x = 0; x < resCount; x++)
                    {
                        if (!results[x].CompareTag("Ore")) continue;
                        valid = false;
                        break;
                    }
                    
                    if (!valid) continue;
                    if (sample.magnitude > veinRadius) continue;

                    active.Add(new KeyValuePair<Vector2, int>(sample, oreSize));
                    
                    if (!Physics2D.OverlapPoint(veinCenter + sample)) continue;

                    if (n > 0) i++;
                    
                    SpawnOre(veinParentTransform, sample, oreSize, veinType);
                }
                
                active.RemoveAt(rIndex);
            }
        }

        private void SpawnOre(Transform veinParentTransform, Vector2 sample, int oreSize, VeinType veinType)
        {
            // NOTE: Parent and position are set in the Instantiate call, because otherwise collision checks
            // are inaccurate. Setting the position after the Instantiate call only updates the transform, not
            // the physics components.
            var oreClone = Instantiate(orePrefab, veinParentTransform.position + (Vector3)sample, Quaternion.identity, veinParentTransform);

            var spritePool = oreSize switch
            {
                0 => veinType.oreSpritesSmall,
                1 => veinType.oreSpritesMedium,
                2 => veinType.oreSpritesLarge,
                _ => null
            };

            if (spritePool == null)
            {
                throw new Exception("Invalid ore size.");
            }

            var oreSprite = spritePool[Random.Range(0, spritePool.Length)];
            oreClone.GetComponent<SpriteRenderer>().sprite = oreSprite;

            var oreCollider = oreClone.GetComponent<BoxCollider2D>();
            oreCollider.size = oreSprite.bounds.size;

            var oreInstance = oreClone.AddComponent<BreakableItemInstance>();
            oreInstance.itemSo = veinType.oreSo;
            oreInstance.toughness = veinType.oreToughness;
        }
    }
}
