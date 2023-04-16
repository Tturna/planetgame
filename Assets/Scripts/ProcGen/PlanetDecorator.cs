using UnityEngine;

namespace ProcGen
{
    public class PlanetDecorator : MonoBehaviour
    {
        [SerializeField] private Sprite[] treePool;
        [SerializeField] private float minimumSpawnHeight;
        [SerializeField] private float treeCount;
        
        public void SpawnTrees(PlanetGenerator planetGen)
        {
            // start at angle 0 and increment by random amounts
            var angle = 0f;
            for (var i = 0; i < treeCount; i++)
            {
                // Get surface height and check against minimum spawn height
                var point = (Vector3)planetGen.GetRelativeSurfacePoint(angle);
                point += transform.position;

                // spawn trees
                var tree = new GameObject("Tree");
                tree.transform.SetParent(transform);
                var sr = tree.AddComponent<SpriteRenderer>();
                sr.sprite = treePool[Random.Range(0, treePool.Length)];
                
                var dirToPlanet = (transform.position - point).normalized;
                
                // raycast below each tree to find the surface point
                var hit = Physics2D.Raycast(point, dirToPlanet);
                
                var spriteHeight = 
                tree.transform.position = hit.point;
                
                // make the tree face perpendicular to the planet
                tree.transform.LookAt((Vector3)hit.point + Vector3.forward, -dirToPlanet);

                // Increment angle by random amount
                angle += Random.Range(5, 20);
            }
        }
    }
}
