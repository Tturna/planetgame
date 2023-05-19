using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProcGen
{
    // [RequireComponent(typeof(Planet))]
    [RequireComponent(typeof(PlanetDecorator))]
    public class PlanetGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject cellPrefab;

        [Header("Generic Noise Settings")]
        [SerializeField] private int octaves;
        [SerializeField] private float persistence;
        [SerializeField] private float lacunarity;
        [SerializeField] private AnimationCurve noiseResultCurve;
        [SerializeField, Range(0f, 1f), Tooltip("Distance percentage at after which point values will bee smoothed to air.")]
        private float edgeSmoothThreshold;
        
        [Header("Outer Noise Settings")]
        [SerializeField] private float xOrg;
        [SerializeField] private float yOrg;
        [SerializeField] private float noiseScale;
        [SerializeField] private float isolevel;

        [Header("Inner Noise Settings")]
        [SerializeField] private AnimationCurve blendBias;
        [SerializeField] private float xInnerOrg;
        [SerializeField] private float yInnerOrg;
        [SerializeField] private float innerNoiseScale;
        [SerializeField] private float innerIsoLevel;
        
        [Header("Surface Noise Settings")]
        [SerializeField] private float xSurfaceOrg;
        [SerializeField] private float ySurfaceOrg;
        [SerializeField] private float surfaceNoiseScale;
        [SerializeField] private float surfaceNoiseStrength;
        
        [Header("Planet Properties")]
        public float diameter;
        public int resolution;
        [SerializeField] private float atmosphereRadius;
        
        // TODO: Implement max gravity radius...
        // and separate it from drag so there's a layer of gravity
        // without drag around a planet. This is so things can
        // stay in orbit.
        
        [SerializeField] private float maxDrag;
        [SerializeField] private float maxGravityMultiplier;
        [SerializeField, Range(0, 1),
         Tooltip("Distance percentage at which max drag and gravity is reached. 0 is at the edge of the atmosphere, 1 is at the center of the core")]
        private float maxPhysicsThreshold;
        
        public Sprite surfaceCameraBackground;
        public Color surfaceBackgroundColor;

        private Point[] _pointField;
        private GameObject[] _cellField;
        private List<MeshFilter> _surfaceMeshFilters = new();
        private GameObject _cellParent;
        private PlanetDecorator _decorator;
        private float SizeResRatio => diameter / resolution;
        private float Radius => diameter / 2;
        
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

        public struct Point
        {
            public Vector3 position;
            public float value;
            public bool isSet;
            public float isoLevel;
        }

        private void Start()
        {
            var startTime = Time.realtimeSinceStartupAsDouble;

            _pointField = new Point[resolution * resolution];
            _cellField = new GameObject[(resolution - 1) * (resolution - 1)];
            GeneratePlanet();

            _decorator = GetComponent<PlanetDecorator>();
            _decorator.SpawnTrees(this);
            _decorator.CreateBackgroundDecorations(this);
            _decorator.CreateBackgroundTerrain(_surfaceMeshFilters.ToArray());
            
            print($"Planet generated in: {Time.realtimeSinceStartupAsDouble - startTime} s");
            
            var atmosphereCollider = gameObject.AddComponent<CircleCollider2D>();
            atmosphereCollider.radius = atmosphereRadius;
            atmosphereCollider.isTrigger = true;
        }

        /// <summary>
        /// Turns cell index coordinates to world space coordinates relative to the planet.
        /// </summary>
        /// <param name="iterX"></param>
        /// <param name="iterY"></param>
        /// <returns></returns>
        private Vector3 GetPointRelativePosition(float iterX, float iterY)
        {
            return new Vector3(iterX * (diameter / resolution) - diameter / 2, iterY * (diameter / resolution) - diameter / 2);
        }

        /// <summary>
        /// Gets the cell's distance from the center + surface noise addition
        /// </summary>
        /// <param name="cellX"></param>
        /// <param name="cellY"></param>
        /// <returns></returns>
        private float GetCellSurfaceHeight(float cellX, float cellY)
        {
            var xc = xSurfaceOrg + cellX / resolution * surfaceNoiseScale;
            var yc = ySurfaceOrg + cellY / resolution * surfaceNoiseScale;
            var surfaceNormalized = Mathf.PerlinNoise(xc, yc);
            var surfaceAddition = surfaceNormalized * surfaceNoiseStrength;
            
            return diameter / 2 - surfaceNoiseStrength + surfaceAddition;
        }

        public Vector2 GetRelativeSurfacePoint(float angle)
        {
            var x = Mathf.Sin(angle * Mathf.Deg2Rad);
            var y = Mathf.Cos(angle * Mathf.Deg2Rad);
            var dir = new Vector2(x, y);
            return dir * Radius;
        }

        private float Noise(float x, float y)
        {
            var result = 0f;
            var amplitude = 1f;
            var frequency = 1f;

            for (var i = 0; i < octaves; i++) {
                result += amplitude * Mathf.PerlinNoise(x * frequency, y * frequency);
                amplitude *= persistence;
                frequency *= lacunarity;
                
                if (i == 0) continue;
                result *= 0.5f;
            }
            
            return noiseResultCurve.Evaluate(result);
        }

        /// <summary>
        /// Calculates point value and iso level and returns a new point based on those.
        /// </summary>
        /// <param name="iterX"></param>
        /// <param name="iterY"></param>
        /// <param name="pointPos"></param>
        /// <param name="pointRelativePosition"></param>
        /// <returns>new point calculated with noise</returns>
        private Point MakePoint(float iterX, float iterY, Vector3 pointPos, Vector3 pointRelativePosition)
        {
            // Calculate point distance from the core
            var distancePercentage = pointRelativePosition.magnitude / (diameter / 2);
                
            // Blend between outer and inner noise
            var v = blendBias.Evaluate(distancePercentage);
            var noiseX = Mathf.Lerp(xInnerOrg, xOrg, v);
            var noiseY = Mathf.Lerp(yInnerOrg, yOrg, v);
            var scale = Mathf.Lerp(innerNoiseScale, noiseScale, v);

            var noiseResult = Noise(noiseX + iterX / resolution * scale, noiseY + iterY / resolution * scale);

            // Smooth points to air at the surface
            if (distancePercentage > edgeSmoothThreshold)
            {
                var edgeSmoothPercentage = (distancePercentage - edgeSmoothThreshold) * 5;
                noiseResult = Mathf.Lerp(noiseResult, 1f, edgeSmoothPercentage);
            }
            
            return new Point
            {
                position = pointPos,
                value = noiseResult,
                isSet = true,
                isoLevel = Mathf.Lerp(innerIsoLevel, isolevel, v)
            };
        }

        private Point CalculatePoint(float x, float y)
        {
            var trPos = transform.position;
                    
            var pointRelativePosition = GetPointRelativePosition(x, y);
            var pointPos = trPos + pointRelativePosition;
            pointPos.z = 0f;
                
            // Restrict points to a circle (+- some surface noise)
            var surfaceHeight = GetCellSurfaceHeight(x, y);
            var pointRadialDistance = pointRelativePosition.magnitude;

            // If the point is not within the initial planet shape, just give it a position and set it to air.
            // This position is required so that the player can add terrain to it later.
            if (pointRadialDistance > surfaceHeight) return new Point
            {
                value = 1f,
                position = pointPos,
                isoLevel = isolevel
            };
                    
            var point = MakePoint(x, y, pointPos, pointRelativePosition);

            // Make outer most points into air to prevent a tiled surface
            // if (pointRadialDistance > surfaceHeight - 2)
            // {
            //     point.value = 1f;
            // }

            return point;
        }

        private void GeneratePlanet()
        {
            // Iterate through cells
            for (var y = 0; y < resolution - 1; y++)
            {
                for (var x = 0; x < resolution - 1; x++)
                {
                    var data = CalculateCell(y, x);
                    if (data.idx == -1) continue;
                    
                    _cellField[data.idx] = GenerateCell(data.idx, data.vertices, data.triangles);
                }
            }
        }

        private GameObject MakeCellParent()
        {
            var cp = new GameObject("Cells");
            cp.transform.SetParent(transform);
            cp.transform.localPosition = Vector3.zero;
            cp.tag = "Planet";
            cp.layer = LayerMask.NameToLayer("Terrain");

            // Give it a kinematic rigidbody so the planet can be collided with
            var rb = cp.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;

            // var cc = cp.AddComponent<CompositeCollider2D>();
            // cc.offsetDistance = 0.2f;

            return cp;
        }
        
        private GameObject MakeCellObject(int idx, Mesh mesh)
        {
            if (!_cellParent) _cellParent = MakeCellParent();
            
            // var cell = new GameObject($"Cell {idx}");
            // cell.transform.SetParent(_cellParent.transform);
            // cell.tag = "Planet";
            // cell.layer = LayerMask.NameToLayer("Terrain");
            //     
            // var meshFilter = cell.AddComponent<MeshFilter>();
            // var meshRenderer = cell.AddComponent<MeshRenderer>();
            // meshRenderer.material = cellMaterial;
            
            var cell = Instantiate(cellPrefab, _cellParent.transform);
            cell.transform.position = Vector3.zero;
            cell.name = $"Cell {idx}";

            var meshFilter = cell.GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            // only add mesh filters near the surface
            var (x, y) = GetXYFromIndex(idx);
            var p = GetPointRelativePosition(x, y);
            var mag = p.magnitude;
            if (mag > Radius * .9f)
                
            {
                _surfaceMeshFilters.Add(meshFilter);
            }

            return cell;
        }
        
        public GameObject GenerateCell(int idx, Vector3[] vertices, int[] triangles)
        {
            var mesh = new Mesh();
            mesh.name = idx.ToString();

            var cell = MakeCellObject(idx, mesh);
            // var polyCollider = cell.AddComponent<PolygonCollider2D>();
            var polyCollider = cell.GetComponent<PolygonCollider2D>();

            // Convert vertices to vector2[] for the collider
            var vertices2 = Array.ConvertAll(vertices, v3 => new Vector2(v3.x, v3.y));

            // Use vertices that were calculated above
            // Use preset triangles using the cell pattern
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            polyCollider.points = triangles.Select(trindex => vertices2[trindex]).ToArray();

            return cell;
        }
        
        /// <summary>
        /// Calculate cell index, all 8 vertex points using noise and their mesh triangle generation patterns.
        /// </summary>
        /// <param name="y"></param>
        /// <param name="x"></param>
        /// <param name="idx"></param>
        /// <param name="cornerPoints"></param>
        /// <returns></returns>
        public (int idx, Vector3[] vertices, int[] triangles) CalculateCell(int y, int x, int idx = -1, Point[] cornerPoints = null)
        {
            if (idx == -1) idx = (resolution - 1) * y + x;

            Point bl, br, tl, tr;
            if (cornerPoints == null)
            {
                bl = _pointField[idx] = CalculatePoint(x, y);
                tl = _pointField[(resolution - 1) * (y + 1) + x] = CalculatePoint(x, y + 1);
                br = _pointField[(resolution - 1) * y + x + 1] = CalculatePoint(x + 1, y);
                tr = _pointField[(resolution - 1) * (y + 1) + x + 1] = CalculatePoint(x + 1, y + 1);
            }
            else
            {
                bl = _pointField[idx] = cornerPoints[0];
                tl = _pointField[(resolution - 1) * (y + 1) + x] = cornerPoints[1];
                br = _pointField[(resolution - 1) * y + x + 1] = cornerPoints[2];
                tr = _pointField[(resolution - 1) * (y + 1) + x + 1] = cornerPoints[3];
            }

            // This skips point calculation for cells outside the planet terrain
            // Maybe it skips something else as well I can't remember lmao.
            // I guess this system is a bit fucked then
            if (!bl.isSet && !br.isSet && !tl.isSet && !tr.isSet) return (-1, null, null);

            // Figure out cell pattern
            // The pattern will be used to look up triangle generation patterns
            var byteIndex = 0;
            if (bl.value > bl.isoLevel) byteIndex |= 1;
            if (br.value > br.isoLevel) byteIndex |= 2;
            if (tr.value > tr.isoLevel) byteIndex |= 4;
            if (tl.value > tl.isoLevel) byteIndex |= 8;

            // If all corner points are considered "air", skip
            if (byteIndex is 15) return (-1, null, null);

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
            var vertices = new[] {
                bl.position,
                br.position,
                tr.position,
                tl.position,
                Vector3.Lerp(bl.position, br.position, ts[0]),
                Vector3.Lerp(br.position, tr.position, ts[1]),
                Vector3.Lerp(tl.position, tr.position, ts[2]),
                Vector3.Lerp(bl.position, tl.position, ts[3])
            };

            if (idx == 64390)
            {
                print("64390");
            }

            return (idx, vertices, _triTable[byteIndex]);
        }
        
        public Point[] GetCellCornerPoints(int idx)
        {
            var (x, y) = GetXYFromIndex(idx);
            
            var temp = new[] {
                _pointField[idx],
                _pointField[(resolution - 1) * (y + 1) + x],
                _pointField[(resolution - 1) * y + x + 1],
                _pointField[(resolution - 1) * (y + 1) + x + 1]
            };

            return temp;
        }

        /// <summary>
        /// Get cell coordinates from its index.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public (int x, int y) GetXYFromIndex(int idx)
        {
            var x = idx % (resolution - 1);
            // var y = (int)Mathf.Round((float)idx / resolution);
            var y = (idx - x) / (resolution - 1);
            return (x, y);
        }

        public GameObject GetCellFromIndex(int index)
        {
            return _cellField[index];
        }

        public Vector2Int WorldToCellPoint(Vector2 worldPoint)
        {
            var cellX = (int)((worldPoint.x + Radius) / SizeResRatio);
            var cellY = (int)((worldPoint.y + Radius) / SizeResRatio);
            return new Vector2Int(cellX, cellY);
        }
        
        /// <summary>
        /// Get the distance of the given position from the center of the planet. 1 = core, 0 = edge of atmosphere
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public float GetDistancePercentage(Vector3 position)
        {
            var distanceFromCore = (position - transform.position).magnitude;
            var perc = distanceFromCore / atmosphereRadius;
            var rev = 1 - perc;
            // var limited = rev / threshold;
            
            return Mathf.Clamp01(rev);
        }
        
        public float GetDrag(Vector3 position)
        {
            var perc = GetDistancePercentage(position);
            var limitedPerc = Utilities.InverseLerp(0f, maxPhysicsThreshold, perc);
            return maxDrag * limitedPerc;
        }

        public float GetGravity(Vector3 position)
        {
            var perc = GetDistancePercentage(position);
            var limitedPerc = Utilities.InverseLerp(0f, maxPhysicsThreshold, perc);
            return maxGravityMultiplier * limitedPerc;
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, diameter * 0.5f);
        }
    }
}
