using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TempOreThing : MonoBehaviour
{
    [SerializeField] private GameObject[] orePrefabs;
    
    private const int VeinDiameter = 5; // set a circle diameter for the ore spawn area
    private const float SampleGap = .45f;
    private const int SampleGenAttempts = 30;
    private static float CellEdge => SampleGap / Mathf.Sqrt(2);
    private Vector2?[] _grid; // array of samples representing a 2D grid
    private readonly List<Vector2> _active = new();

    private void Start()
    {
        
        // Initialize grid with -1
        var cols = Mathf.FloorToInt(VeinDiameter / CellEdge);
        var rows = cols; // rows = cols because we use a circle area
        _grid = new Vector2?[cols * rows];
        
        for (var i = 0; i < cols * rows; i++)
        {
            _grid[i] = null;
        }
        
        // Pick initial sample. This could be random but we use the center of the ore spawn area
        var iSample = Vector2.one * VeinDiameter * .5f;
        
        // Get grid indices from sample position
        var iXY = Vector2Int.FloorToInt(iSample / CellEdge);
        _grid[iXY.x + iXY.y * cols] = iSample;
        
        // Initialize active list
        _active.Add(iSample);
        
        // Spawn ore
        var initOre = Instantiate(orePrefabs[Random.Range(0, orePrefabs.Length)], transform, true);
        initOre.transform.localPosition = iSample - Vector2.one * VeinDiameter * .5f;
        
        // The meat to the bones
        while (_active.Count > 0)
        {
            var rIndex = Random.Range(0, _active.Count);

            // Attempt to create new points in valid locations k times.
            // This is done by randomly generating points around existing points and checking if they're too close
            // to other existing points.
            var validFound = false;
            for (var i = 0; i < SampleGenAttempts; i++)
            {
                var offset = Random.insideUnitCircle.normalized * Random.Range(SampleGap, 2 * SampleGap);
                var nSample = _active[rIndex] + offset;
                var xy = Vector2Int.FloorToInt(nSample / CellEdge);
                
                // Make sure we don't create samples outside of the vein area
                if (xy.x < 0 || xy.x >= cols) continue;
                if (xy.y < 0 || xy.y >= rows) continue;
                if (Vector2.Distance(Vector2.one * VeinDiameter * .5f, nSample) > VeinDiameter * .5f) continue;
                
                // Make sure this sample doesn't already exist
                if (_grid[xy.x + xy.y * cols] != null) continue;
                
                // Check if new sample is too close to existing points by comparing against neighboring cells in the grid.
                var valid = true;
                
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        // Make sure we don't check for neighbords outside the grid
                        if (xy.x + x < 0 || xy.x + x >= cols) continue;
                        if (xy.y + y < 0 || xy.y + y >= rows) continue;
                        
                        var n = _grid[xy.x + x + (xy.y + y) * cols];
                        if (n == null) continue;
                        var neighbor = (Vector2)n;
                        
                        // Make sure we don't check for neighbors outside the vein spawn circle
                        if (Vector2.Distance(Vector2.one * VeinDiameter * .5f, neighbor) > VeinDiameter * .5f) continue;
                        
                        var dist = Vector2.Distance(nSample, neighbor);

                        if (dist < SampleGap)
                        {
                            // New sample is too close to an existing sample
                            valid = false;
                        }
                    }
                }
                
                // Add valid sample to grid and active list
                if (valid)
                {
                    validFound = true;
                    _grid[xy.x + xy.y * cols] = nSample;
                    _active.Add(nSample);
                    
                    // Spawn ore
                    var oreClone = Instantiate(orePrefabs[Random.Range(0, orePrefabs.Length)], transform, true);
                    oreClone.transform.localPosition = nSample - Vector2.one * VeinDiameter * .5f;
                }
            }
            
            // If no valid sample could be generated, remove this sample from the active list
            if (!validFound)
            {
                _active.RemoveAt(rIndex);
            }
        }
    }

    private void OnDrawGizmos()
    {
        foreach (var point in _grid)
        {
            if (point == null) continue;
            var trPos = transform.position;
            Gizmos.DrawWireSphere(trPos + (Vector3)(Vector2)point - Vector3.one * VeinDiameter * .5f, .3f);
            Gizmos.DrawWireSphere(trPos, VeinDiameter * .5f);
        }
    }
}
