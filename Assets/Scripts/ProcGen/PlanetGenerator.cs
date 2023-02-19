using System;
using System.Linq;
using UnityEngine;

namespace ProcGen
{
    [RequireComponent(typeof(Planet))]
    public class PlanetGenerator : MonoBehaviour
    {
        [SerializeField] Material cellMaterial;
        [SerializeField] float diameter;
        [SerializeField] int resolution;
        
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

        private Point[] _pointField;
        
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
            GeneratePlanet();

            print(Time.realtimeSinceStartupAsDouble - startTime);
            print("Total");
        }

        private Vector3 GetPointRelativePosition(float iterX, float iterY)
        {
            return new Vector3(iterX * (diameter / resolution) - diameter / 2, iterY * (diameter / resolution) - diameter / 2);
        }

        private float GetSurfacePointAddition(float iterX, float iterY)
        {
            var xc = xSurfaceOrg + iterX / resolution * surfaceNoiseScale;
            var yc = ySurfaceOrg + iterY / resolution * surfaceNoiseScale;
            var surfaceNormalized = Mathf.PerlinNoise(xc, yc);
            var surfaceAddition = surfaceNormalized * surfaceNoiseStrength;
            return surfaceAddition;
        }

        private Point MakePoint(float iterX, float iterY, Vector3 pointPos, Vector3 pointRelativePosition)
        {
            // Calculate point distance from the core
            var distancePercentage = pointRelativePosition.magnitude / (diameter / 2);
                
            // Blend between outer and inner noise
            var v = blendBias.Evaluate(distancePercentage);
            var noiseX = Mathf.Lerp(xInnerOrg, xOrg, v);
            var noiseY = Mathf.Lerp(yInnerOrg, yOrg, v);
            var scale = Mathf.Lerp(innerNoiseScale, noiseScale, v);
                
            return new Point
            {
                position = pointPos,
                value = Mathf.PerlinNoise(noiseX + iterX / resolution * scale, noiseY + iterY / resolution * scale),
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
            var surfaceAddition = GetSurfacePointAddition(x, y);
            var surfaceHeight = diameter / 2 - surfaceNoiseStrength + surfaceAddition;
            var pointRadialDistance = Vector3.Distance(pointPos, trPos);

            if (pointRadialDistance > surfaceHeight) return new Point();
                    
            var point = MakePoint(x, y, pointPos, pointRelativePosition);

            // Make outer most points into air to prevent a tiled surface
            if (pointRadialDistance > surfaceHeight - 2)
            {
                point.value = 1;
            }

            return point;
        }

        private void GeneratePlanet()
        {
            var cellParent = MakeCellParent();
            
            // Iterate through cells
            for (var y = 0; y < resolution - 1; y++)
            {
                for (var x = 0; x < resolution - 1; x++)
                {
                    var data = CalculateCell(y, x);
                    if (data.idx == -1) continue;
                    
                    GenerateCell(data.idx, data.vertices, data.triangles);
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
                cp.layer = LayerMask.NameToLayer("Terrain");

                // Give it a kinematic rigidbody so the planet can be collided with
                var rb = cp.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;

                // var cc = cp.AddComponent<CompositeCollider2D>();
                // cc.offsetDistance = 0.2f;

                return cp;
            }

            GameObject MakeCellObject(int idx, Mesh mesh)
            {
                var cell = new GameObject($"Cell {idx}");
                cell.transform.SetParent(cellParent.transform);
                cell.tag = "Planet";
                cell.layer = LayerMask.NameToLayer("Terrain");
                
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

                // Convert vertices to vector2[] for the collider
                var vertices2 = Array.ConvertAll(vertices, v3 => new Vector2(v3.x, v3.y));

                // Use vertices that were calculated above
                // Use preset triangles using the cell pattern
                mesh.vertices = vertices;
                mesh.triangles = triangles;

                polyCollider.points = triangles.Select(trindex => vertices2[trindex]).ToArray();
            }

            #endregion
        }

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

            if (!bl.isSet || !br.isSet || !tl.isSet || !tr.isSet) return (-1, null, null);
        
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

            return (idx, vertices, _triTable[byteIndex]);
        }
        
        public Point[] GetCellCornerPoints(int idx)
        {
            var (x, y) = GetXYFromIndex(idx);
            
            return new[]
            {
                _pointField[idx],
                _pointField[(resolution - 1) * (y + 1) + x],
                _pointField[(resolution - 1) * y + x + 1],
                _pointField[(resolution - 1) * (y + 1) + x + 1]
            };
        }

        public (int x, int y) GetXYFromIndex(int idx)
        {
            var x = idx % (resolution - 1);
            var y = Mathf.FloorToInt((float)idx / resolution) + 1;
            return (x, y);
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, diameter * 0.5f);
        }
    }
}
