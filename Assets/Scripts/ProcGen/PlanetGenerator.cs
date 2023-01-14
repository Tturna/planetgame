using System;
using System.Linq;
using UnityEngine;

namespace ProcGen
{
    public class PlanetGenerator : MonoBehaviour
    {
        public GameObject debugCircle;
        public Material cellMaterial;
        public float diameter;
        public int xResolution;
        public int yResolution;
        public bool makePoints;
        
        [Header("Outer Noise Settings")]
        public float xOrg;
        public float yOrg;
        public float noiseScale;
        public float isolevel;

        [Header("Inner Noise Settings")]
        public AnimationCurve blendBias;
        public float xInnerOrg;
        public float yInnerOrg;
        public float innerNoiseScale;
        public float innerIsoLevel;
        
        [Header("Surface Noise Settings")]
        public float xSurfaceOrg;
        public float ySurfaceOrg;
        public float surfaceNoiseScale;
        public float surfaceNoiseStrength;

    
        private Point[] _field;
        private GameObject[] _circles;
        private GameObject[] _cellField;

        /*
     *      3 - (6) - 2
     *      |         |
     *     (7)       (5)
     *      |         |
     *      0 - (4) - 1
     */
    
        private readonly int[][] _triTable = {
            new[]{ 0, 3, 2, 0, 2, 1 },
            new[]{ 7, 3, 2, 4, 7, 2, 1, 4, 2 },
            new[]{ 4, 0, 3, 5, 4, 3, 2, 5, 3 },
            new[]{ 7, 3, 2, 7, 2, 5 },
            new[]{ 5, 1, 0, 6, 5, 0, 3, 6, 0 },
            new[]{ 7, 3, 6, 4, 7, 6, 4, 6, 5, 5, 1, 4 },
            new[]{ 4, 0, 3, 4, 3, 6 },
            new[]{ 7, 3, 6 },
            new[]{ 6, 2, 1, 1, 0, 7, 7, 6, 1 },
            new[]{ 6, 2, 1, 6, 1, 4 },
            new[]{ 0, 7, 4, 7, 6, 4, 6, 5, 4, 6, 2, 5 },
            new[]{ 6, 2, 5 },
            new[]{ 5, 1, 0, 5, 0, 7 },
            new[]{ 5, 1, 4 },
            new[]{ 7, 4, 0 }
        };

        private struct Point
        {
            public Vector3 Position;
            public float Value;
            public bool IsSet;
            public float IsoLevel;
        }

        void Start()
        {
            // Calculate noise and make circles for visualization
            _field = new Point[xResolution * yResolution];
            _circles = new GameObject[xResolution * yResolution];

            GameObject circleParent = null;
            if (makePoints && debugCircle)
            {
                circleParent = new GameObject("Circles");
            }

            var radius = diameter / 2;

            for (var y = 0f; y < yResolution; y++)
            {
                for (var x = 1f; x <= xResolution; x++)
                {
                    // Get point position
                    var pointWorldPos = new Vector3(x * (diameter / xResolution) - radius, y * (diameter / yResolution) - radius);
                    var pointPos = transform.position + pointWorldPos;
                    var pointRadialDistance = Vector3.Distance(pointPos, transform.position);
                
                    // Restrict points to a circle (+- some surface noise)
                    var xc = xSurfaceOrg + x / xResolution * surfaceNoiseScale;
                    var yc = ySurfaceOrg + y / yResolution * surfaceNoiseScale;
                    var surfaceNormalized = Mathf.PerlinNoise(xc, yc);
                    var surfaceAddition = surfaceNormalized * surfaceNoiseStrength;
                    var surfaceHeight = radius - surfaceNoiseStrength + surfaceAddition;

                    if (pointRadialDistance > surfaceHeight) continue;
                    
                    // Calculate point distance from the core
                    var distance = pointWorldPos.magnitude;
                    var distancePercentage = distance / radius;
                    // var surfaceMask = 1 - Mathf.Floor(distancePercentage);

                    // Blend between outer and inner noise
                    var v = blendBias.Evaluate(distancePercentage);
                    var noiseX = Mathf.Lerp(xInnerOrg, xOrg, v);
                    var noiseY = Mathf.Lerp(yInnerOrg, yOrg, v);
                    var scale = Mathf.Lerp(innerNoiseScale, noiseScale, v);
                    
                    var idx = xResolution * (int)y + (int)x - 1;
                    _field[idx] = new Point
                    {
                        Position = pointPos,
                        Value = Mathf.PerlinNoise(noiseX + x / xResolution * scale, noiseY + y / yResolution * scale),
                        IsSet = true,
                        IsoLevel = Mathf.Lerp(innerIsoLevel, isolevel, v)
                    };
                    
                    // Make outer most points into air to prevent a tiled surface
                    if (pointRadialDistance > surfaceHeight - 2)
                    {
                        _field[idx].Value = 1;
                    }

                    if (makePoints && debugCircle)
                    {
                        var circleClone = Instantiate(debugCircle, circleParent!.transform, true);
                        circleClone.transform.position = _field[idx].Position;
                        circleClone.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.black, Color.white, _field[idx].Value);
                        _circles[idx] = circleClone;
                    }
                }
            }

            CalculateMesh();
        }

        private void Update()
        {
            // if (update)
            // {
            //     CalculateNoise();
            //     CalculateMesh();
            //
            //     if (scroll)
            //     {
            //         xOrg += Time.deltaTime;
            //     }
            // }
        }

        private void CalculateMesh()
        {
            _cellField = new GameObject[(yResolution - 1) * (xResolution - 1)];
            
            var cellParent = new GameObject("Cells");
            cellParent.transform.SetParent(transform);
            cellParent.transform.localPosition = Vector3.zero;
            cellParent.tag = "Planet";
            cellParent.layer = LayerMask.NameToLayer("World");

            var rb = cellParent.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            
            // cellParent.AddComponent<CompositeCollider2D>();
            
            // Iterate through cells
            for (var i = 0; i < yResolution - 1; i++)
            {
                for (var j = 0; j < xResolution - 1; j++)
                {
                    var idx = (xResolution - 1) * i + j;

                    // Figure out the points in the current cell
                    Point bl = _field[xResolution * i + j];
                    Point br = _field[xResolution * i + j + 1];
                    Point tl = _field[xResolution * (i + 1) + j];
                    Point tr = _field[xResolution * (i + 1) + j + 1];
                    
                    if (!bl.IsSet || !br.IsSet || !tl.IsSet || !tr.IsSet) continue;
                
                    // Figure out cell pattern
                    var byteIndex = 0;
                    if (bl.Value > bl.IsoLevel) byteIndex |= 1;
                    if (br.Value > br.IsoLevel) byteIndex |= 2;
                    if (tr.Value > tr.IsoLevel) byteIndex |= 4;
                    if (tl.Value > tl.IsoLevel) byteIndex |= 8;

                    if (byteIndex is 15)
                    {
                        continue;
                    }

                    #region Lerp Shit
                
                    var mins = new[]
                    {
                        Mathf.Min(bl.Value, br.Value),
                        Mathf.Min(br.Value, tr.Value),
                        Mathf.Min(tr.Value, tl.Value),
                        Mathf.Min(tl.Value, bl.Value)
                    };

                    var maxes = new[]
                    {
                        Mathf.Max(bl.Value, br.Value),
                        Mathf.Max(br.Value, tr.Value),
                        Mathf.Max(tr.Value, tl.Value),
                        Mathf.Max(tl.Value, bl.Value)
                    };
                
                    var ts = new[]
                    {
                        (isolevel - mins[0]) / (maxes[0] - mins[0]),
                        (isolevel - mins[1]) / (maxes[1] - mins[1]),
                        (isolevel - mins[2]) / (maxes[2] - mins[2]),
                        (isolevel - mins[3]) / (maxes[3] - mins[3])
                    };

                    // Fix lerp t direction when going from bright areas to dark areas
                    // Without this, some surfaces are fucked
                    if (bl.Value > br.Value) ts[0] = 1 - ts[0];
                    if (br.Value > tr.Value) ts[1] = 1 - ts[1];
                    if (tl.Value > tr.Value) ts[2] = 1 - ts[2];
                    if (bl.Value > tl.Value) ts[3] = 1 - ts[3];
                
                    #endregion

                    // Make a vertex list from the corner vertices above
                    // Add edges and use ts for linear interpolation
                    var vertices = new[]
                    {
                        bl.Position,
                        br.Position,
                        tr.Position,
                        tl.Position,
                        Vector3.Lerp(bl.Position, br.Position, ts[0]),
                        Vector3.Lerp(br.Position, tr.Position, ts[1]),
                        Vector3.Lerp(tl.Position, tr.Position, ts[2]),
                        Vector3.Lerp(bl.Position, tl.Position, ts[3])
                    };

                    var mesh = new Mesh();
                    mesh.name = idx.ToString();
                    var cell = new GameObject($"Cell {idx}");
                    cell.transform.SetParent(cellParent.transform);
                    cell.tag = "Planet";
                    cell.layer = LayerMask.NameToLayer("World");
                    // cell.transform.position = transform.position + new Vector3(j * (10f / width) - 5, i * (10f / height) - 5);
                    
                    var meshFilter = cell.AddComponent<MeshFilter>();
                    var meshRenderer = cell.AddComponent<MeshRenderer>();
                    var polyCollider = cell.AddComponent<PolygonCollider2D>();

                    meshRenderer.material = cellMaterial;
                    meshFilter.mesh = mesh;

                    // Convert vertices to vector2[]
                    var vertices2 = Array.ConvertAll(vertices, (v3) => new Vector2(v3.x, v3.y));
                    
                    _cellField[idx] = cell;

                    // Use vertices that were calculated above
                    // Use preset triangles using the cell pattern
                    mesh.vertices = vertices;
                    mesh.triangles = _triTable[byteIndex];

                    polyCollider.points = mesh.triangles.Select(trindex => vertices2[trindex]).ToArray();
                }
            }
        }

        private void CalculateNoise()
        {
            for (var y = 0f; y < yResolution; y++)
            {
                for (var x = 1f; x <= xResolution; x++)
                {
                    var idx = xResolution * (int)y + (int)x - 1;
                    _field[idx].Value = Mathf.PerlinNoise(xOrg + x / xResolution * noiseScale, yOrg + y / yResolution * noiseScale);
                
                    //_circles[idx].GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.7f, 0.7f, 0.7f), Color.black, _field[idx].Value);
                }
            }
        }
    }
}
