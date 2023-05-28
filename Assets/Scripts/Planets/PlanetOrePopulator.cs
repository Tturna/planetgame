// This script populates a planet with ore veins. Ore veins are generated using
// the Poisson-Disc sampling algorithm to keep ore chunks from overlapping.
// Implemented by following this video:
// https://www.youtube.com/watch?v=flQgnCUxHlw

using System.Collections.Generic;
using UnityEngine;

namespace Planets
{
    public class PlanetOrePopulator : MonoBehaviour
    {
        [System.Serializable]
        private struct VeinType
        {
            public ScriptableObject oreSo;
            [Range(0f, 1f)] public float oreSpawnHeight;
            public float veinDiameter;
            public float chunkSampleGap;
            public Sprite[] oreSprites;
            public int oreToughness;
        }
        
        [SerializeField] private GameObject orePrefab;
        [SerializeField] private VeinType[] veinTypes;
        
        [Header("Shit to be removed probably")]
        [SerializeField, Range(0.1f, 1f)] private float oreSpawnHeight;
        
        [SerializeField] private int chunkSampleGenAttempts; // k
        [SerializeField] private float veinSampleGap;
        [SerializeField] private int veinSampleGenAttempts;
        
        private Transform _oreParentTransform;
        private PlanetGenerator _planetGen;

        public void GenerateVeins()
        {
            _planetGen = GetComponent<PlanetGenerator>();
            
            var oreParentObject = new GameObject("Ore Parent");
            _oreParentTransform = oreParentObject.transform;
            _oreParentTransform.parent = transform;
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

                // TODO: Each vein type should have a rarity and...
                // they should all generate with their own sampling configuration, but
                // still not overlap with other veins.
                var veinType = veinTypes[Random.Range(0, veinTypes.Length)];

                var chunkGrid = PoissonDisc(veinType.veinDiameter, veinType.chunkSampleGap, chunkSampleGenAttempts);

                foreach (var oreChunk in chunkGrid)
                {
                    if (oreChunk == null) continue;
                    var chunkPos = (Vector2)oreChunk;
                    var corChunkPos = chunkPos - Vector2.one * veinType.veinDiameter * .5f;
                    
                    if (corChunkPos.magnitude > veinType.veinDiameter * 0.5f) continue;
                    if (!Physics2D.OverlapPoint((Vector2)veinParentTransform.position + corChunkPos)) continue;

                    var oreClone = Instantiate(orePrefab, veinParentTransform, true);
                    oreClone.transform.localPosition = corChunkPos;
                    
                    var oreSprite = veinType.oreSprites[Random.Range(0, veinType.oreSprites.Length)];
                    oreClone.GetComponent<SpriteRenderer>().sprite = oreSprite;
                    
                    var oreInstance = oreClone.AddComponent<OreInstance>();
                    oreInstance.oreSo = veinType.oreSo;
                    oreInstance.oreToughness = veinType.oreToughness;
                }
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
    }
}
