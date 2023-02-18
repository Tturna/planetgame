using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProcGen
{
    [RequireComponent(typeof(Planet))]
    public class PlanetGenGPU : MonoBehaviour
    {
        [SerializeField] ComputeShader planetGenShader;
        
        [SerializeField] Material cellMaterial;
        [SerializeField] float diameter;
        [FormerlySerializedAs("xResolution")] [SerializeField] int resolution;
        
        [Header("Outer Noise Settings")]
        [SerializeField] float xOrg;
        [SerializeField] float yOrg;
        [SerializeField] float noiseScale;
        [SerializeField] float isolevel;

        [Header("Inner Noise Settings")]
        [SerializeField] AnimationCurve blendBias;
        [SerializeField] float xInnerOrg;
        [SerializeField] float yInnerOrg;
        [SerializeField] float innerNoiseScale;
        [SerializeField] float innerIsoLevel;
        
        [Header("Surface Noise Settings")]
        [SerializeField] float xSurfaceOrg;
        [SerializeField] float ySurfaceOrg;
        [SerializeField] float surfaceNoiseScale;
        [SerializeField] float surfaceNoiseStrength;
        
        [Header("Other")]
        [SerializeField] GameObject debugCircle;
        [SerializeField] bool makeDebugPoints;
        
        private Point[] _pointField;
        private GameObject[] _debugCircles;
        private GameObject[] _cellField;
        private Utilities _utilities;

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
            public float x;
            public float y;
            public float value;
            public int isSet;
            public float isoLevel;
        }

        private void Start()
        {
            var startTime = Time.realtimeSinceStartupAsDouble;
            
            _pointField = new Point[resolution * resolution];
            GameObject debugCircleParent = null;
            var trPos = transform.position;

            var radius = diameter / 2;

            // for (var y = 0f; y < resolution; y++)
            // {
            //     for (var x = 1f; x <= xResolution; x++)
            //     {
            //         CalculatePoint(x, y);
            //     }
            // }

            #region Run Compute Shader

            // Create and set buffer to shader
            const int pointDataSize = sizeof(float) * 4 + sizeof(int);
            var pointsBuffer = new ComputeBuffer(_pointField.Length, pointDataSize);
            pointsBuffer.SetData(_pointField);
            planetGenShader.SetBuffer(0, "point_field", pointsBuffer);
            
            // Init values
            planetGenShader.SetFloat("resolution", resolution);
            planetGenShader.SetFloat("planet_diameter", diameter);
            planetGenShader.SetFloat("planet_radius", radius);
            planetGenShader.SetFloat("surface_noise_strength", surfaceNoiseStrength);
            planetGenShader.SetFloat("surface_noise_scale", surfaceNoiseScale);
            planetGenShader.SetFloat("outer_noise_scale", noiseScale);
            planetGenShader.SetFloat("inner_noise_scale", innerNoiseScale);
            planetGenShader.SetFloat("outer_iso_level", isolevel);
            planetGenShader.SetFloat("inner_iso_level", innerIsoLevel);
            planetGenShader.SetFloat("core_position_x", trPos.x);
            planetGenShader.SetFloat("core_position_y", trPos.y);
            planetGenShader.SetFloat("surface_noise_origin_x", xSurfaceOrg);
            planetGenShader.SetFloat("surface_noise_origin_y", ySurfaceOrg);
            planetGenShader.SetFloat("outer_noise_origin_x", xOrg);
            planetGenShader.SetFloat("outer_noise_origin_y", yOrg);
            planetGenShader.SetFloat("inner_noise_origin_x", xInnerOrg);
            planetGenShader.SetFloat("inner_noise_origin_y", yInnerOrg);
            
            planetGenShader.Dispatch(0, resolution / 8, resolution / 8, 1);

            pointsBuffer.GetData(_pointField);
            pointsBuffer.Dispose();
            
            #endregion

            _utilities = Utilities.instance;
            CalculateMesh();

            print(Time.realtimeSinceStartupAsDouble - startTime);
            print("GPU");
            
            // Local functions to clear up the for loop above
            #region LocalFunctions
            
            Vector3 GetPointRelativePosition(float iterX, float iterY)
            {
                return new Vector3(iterX * (diameter / resolution) - radius, iterY * (diameter / resolution) - radius);
            }

            float GetSurfacePointAddition(float iterX, float iterY)
            {
                var xc = xSurfaceOrg + iterX / resolution * surfaceNoiseScale;
                var yc = ySurfaceOrg + iterY / resolution * surfaceNoiseScale;
                var surfaceNormalized = Mathf.PerlinNoise(xc, yc);
                var surfaceAddition = surfaceNormalized * surfaceNoiseStrength;
                return surfaceAddition;
            }

            Point MakePoint(float iterX, float iterY, Vector2 pointPos, Vector2 pointRelativePosition)
            {
                // Calculate point distance from the core
                var distancePercentage = pointRelativePosition.magnitude / radius;
                
                // Blend between outer and inner noise
                var v = blendBias.Evaluate(distancePercentage);
                var noiseX = Mathf.Lerp(xInnerOrg, xOrg, v);
                var noiseY = Mathf.Lerp(yInnerOrg, yOrg, v);
                var scale = Mathf.Lerp(innerNoiseScale, noiseScale, v);
                
                return new Point
                {
                    x = pointPos.x,
                    y = pointPos.y,
                    value = Mathf.PerlinNoise(noiseX + iterX / resolution * scale, noiseY + iterY / resolution * scale),
                    isSet = 1,
                    isoLevel = Mathf.Lerp(innerIsoLevel, isolevel, v)
                };
            }

            void AddDebugCircle(int idx)
            {
                // Initialize circleParent if it's null
                if (!debugCircleParent)
                {
                    _debugCircles = new GameObject[resolution * resolution];
                    debugCircleParent = new GameObject("Circles");
                }
                
                // Add debug circle
                var circleClone = Instantiate(debugCircle, debugCircleParent!.transform, true);
                circleClone.transform.position = new Vector2(_pointField[idx].x, _pointField[idx].y);
                circleClone.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.black, Color.white, _pointField[idx].value);
                _debugCircles[idx] = circleClone;
            }

            void CalculatePoint(float x, float y)
            {
                var pointRelativePosition = GetPointRelativePosition(x, y);
                var pointPos = trPos + pointRelativePosition;
                
                // Restrict points to a circle (+- some surface noise)
                var surfaceAddition = GetSurfacePointAddition(x, y);
                var surfaceHeight = radius - surfaceNoiseStrength + surfaceAddition;
                var pointRadialDistance = Vector3.Distance(pointPos, trPos);

                if (pointRadialDistance > surfaceHeight) return;

                var idx = resolution * (int)y + (int)x - 1;
                    
                _pointField[idx] = MakePoint(x, y, pointPos, pointRelativePosition);

                // Make outer most points into air to prevent a tiled surface
                if (pointRadialDistance > surfaceHeight - 2)
                {
                    _pointField[idx].value = 1;
                }

                if (makeDebugPoints && debugCircle)
                {
                    AddDebugCircle(idx);
                }
            }
            
            #endregion
        }

        // private void Update()
        // {
        //     if (update)
        //     {
        //         CalculateNoise();
        //         CalculateMesh();
        //     
        //         if (scroll)
        //         {
        //             xOrg += Time.deltaTime;
        //         }
        //     }
        // }

        private void CalculateMesh()
        {
            _cellField = new GameObject[(resolution - 1) * (resolution - 1)];
            var cellParent = MakeCellParent();
            
            // Iterate through cells
            for (var i = 0; i < resolution - 1; i++)
            {
                for (var j = 0; j < resolution - 1; j++)
                {
                    CalculateCell(i, j);
                }
            }
            
            // Local functions to clear up the for loop above
            #region LocalFunctions
            
            GameObject MakeCellParent()
            {
                var cp = new GameObject("Cells");
                cp.transform.SetParent(transform);
                cp.transform.localPosition = Vector3.zero;
                cp.tag = "Planet";
                cp.layer = LayerMask.NameToLayer("World");

                // Give it a kinematic rigidbody so the planet can be collided with
                var rb = cp.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;

                return cp;
            }

            GameObject MakeCellObject(int idx, Mesh mesh)
            {
                var cell = new GameObject($"Cell {idx}");
                cell.transform.SetParent(cellParent.transform);
                cell.tag = "Planet";
                cell.layer = LayerMask.NameToLayer("World");
                
                var meshFilter = cell.AddComponent<MeshFilter>();
                var meshRenderer = cell.AddComponent<MeshRenderer>();
                meshRenderer.material = cellMaterial;
                meshFilter.mesh = mesh;

                return cell;
            }

            void GenerateCell(int idx, Vector3[] vertices, int[] triangles)
            {
                var mesh = new Mesh();
                mesh.name = idx.ToString();

                var cell = MakeCellObject(idx, mesh);
                var polyCollider = cell.AddComponent<PolygonCollider2D>();
                    
                _cellField[idx] = cell;

                // Convert vertices to vector2[]
                var vertices2 = Array.ConvertAll(vertices, v3 => new Vector2(v3.x, v3.y));

                // Use vertices that were calculated above
                // Use preset triangles using the cell pattern
                mesh.vertices = vertices;
                mesh.triangles = triangles;

                polyCollider.points = mesh.triangles.Select(trindex => vertices2[trindex]).ToArray();
            }

            void CalculateCell(int i, int j)
            {
                var idx = (resolution - 1) * i + j;

                // Figure out the points in the current cell
                var bl = _pointField[resolution * i + j];
                var tl = _pointField[resolution * (i + 1) + j];
                var br = _pointField[resolution * i + j + 1];
                var tr = _pointField[resolution * (i + 1) + j + 1];
                
                if (bl.isSet == 0 || br.isSet == 0 || tl.isSet == 0 || tr.isSet == 0) return;
            
                // Figure out cell pattern
                // The pattern will be used to look up triangle generation patterns
                var byteIndex = 0;
                if (bl.value > bl.isoLevel) byteIndex |= 1;
                if (br.value > br.isoLevel) byteIndex |= 2;
                if (tr.value > tr.isoLevel) byteIndex |= 4;
                if (tl.value > tl.isoLevel) byteIndex |= 8;

                // If all corner points are considered "air", skip
                if (byteIndex is 15) return;

                #region Calculate Lerp Values
            
                var mins = new[] {
                    Mathf.Min(bl.value, br.value),
                    Mathf.Min(br.value, tr.value),
                    Mathf.Min(tr.value, tl.value),
                    Mathf.Min(tl.value, bl.value)
                };

                var maxes = new[] {
                    Mathf.Max(bl.value, br.value),
                    Mathf.Max(br.value, tr.value),
                    Mathf.Max(tr.value, tl.value),
                    Mathf.Max(tl.value, bl.value)
                };
            
                // Inverse Lerping
                // This is to find the relative point of the average isolevel between the corners.
                // These points are later used to place edge points between corners SMOOTHLY.
                var ts = new[]
                {
                    Utilities.InverseLerp(mins[0], maxes[0], (bl.isoLevel + br.isoLevel) / 2),
                    Utilities.InverseLerp(mins[1], maxes[1], (br.isoLevel + tr.isoLevel) / 2),
                    Utilities.InverseLerp(mins[2], maxes[2], (tr.isoLevel + tl.isoLevel) / 2),
                    Utilities.InverseLerp(mins[3], maxes[3], (tl.isoLevel + bl.isoLevel) / 2)
                };
                
                // var ts = (from x in Enumerable.Range(0, 4) select InverseLerp(mins[x], maxes[x], isolevel???)).ToArray();
                
                // Fix lerp t direction when going from bright areas to dark areas
                // Without this, some surfaces are fucked
                if (bl.value > br.value) ts[0] = 1 - ts[0];
                if (br.value > tr.value) ts[1] = 1 - ts[1];
                if (tl.value > tr.value) ts[2] = 1 - ts[2];
                if (bl.value > tl.value) ts[3] = 1 - ts[3];
            
                #endregion

                // Make a vertex list from the corner vertices above.
                // Add edge points and use ts for linear interpolation to make terrain smooth.
                var blPos = new Vector2(bl.x, bl.y);
                var brPos = new Vector2(br.x, br.y);
                var trPos = new Vector2(tr.x, tr.y);
                var tlPos = new Vector2(tl.x, tl.y);
                
                var vertices = new Vector3[] {
                    blPos,
                    brPos,
                    trPos,
                    tlPos,
                    Vector2.Lerp(blPos, brPos, ts[0]),
                    Vector2.Lerp(brPos, trPos, ts[1]),
                    Vector2.Lerp(tlPos, trPos, ts[2]),
                    Vector2.Lerp(blPos, tlPos, ts[3])
                };
                
                GenerateCell(idx, vertices, _triTable[byteIndex]);
            }

            #endregion
        }

        // private void CalculateNoise()
        // {
        //     for (var y = 0f; y < resolution; y++)
        //     {
        //         for (var x = 1f; x <= xResolution; x++)
        //         {
        //             var idx = xResolution * (int)y + (int)x - 1;
        //             _pointField[idx].Value = Mathf.PerlinNoise(xOrg + x / xResolution * noiseScale, yOrg + y / resolution * noiseScale);
        //         
        //             //_circles[idx].GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.7f, 0.7f, 0.7f), Color.black, _field[idx].Value);
        //         }
        //     }
        // }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, diameter * 0.5f);
        }
    }
}
