using Inventory.Item_SOs;
using UnityEngine;

namespace Planets
{
    public class ScrapSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject breakablePrefab;
        [SerializeField] private ItemSo scrapSo;
        
        private void Start()
        {
            // Rundown:
            // 1. Assume this object is above the planet in Unity's 2D space
            // 2. Raycast down to get the planet
            // 3. Rotate a bit to the left using the planet center as the axis of rotation
            // 4. Raycast down to get the planet's surface to start placing scrap a bit behind the space ship
            // (assuming the spaceship will be in the center of the planet or rotation 0 in Unity's 2D space)
            // 5. Place scrap on the planet's surface
            // 6. Increment the rotation by a small random amount for a bit and place scraps
            // 7. Increase the rotation increment so the scrap gets more and more scarce
            // 8. Maybe spawn the second shipwreck at the end of the scrap trail???

            var planetMask = 1 << LayerMask.NameToLayer("Terrain");
            const float rayLength = 30f;
            var initialScrapCount = Random.Range(10, 15);
            var trailScrapCount = Random.Range(10, 20);
            
            var hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, planetMask);

            if (!hit)
            {
                Debug.LogError("Scrap Spawner couldn't find a planet below it.");
                return;
            }

            var planetTransform = hit.transform.root;
            var planetPosition = planetTransform.position;
            var planetDirection = (planetPosition - transform.position).normalized;
            
            transform.RotateAround(planetPosition, Vector3.back, -10f);

            for (var i = 0; i < initialScrapCount + trailScrapCount; i++)
            {
                hit = Physics2D.Raycast(transform.position, planetDirection, rayLength, planetMask);

                if (hit)
                {
                    var scrap = Instantiate(breakablePrefab, planetTransform, true);
                    scrap.GetComponent<SpriteRenderer>().sprite = scrapSo.sprite;
                    scrap.GetComponent<BoxCollider2D>().size = scrapSo.sprite.bounds.size;
                    scrap.transform.up = hit.normal;
                    scrap.transform.position = hit.point + hit.normal * scrapSo.sprite.bounds.size.y / 2f;
                }

                float angleIncrement;

                if (i < initialScrapCount)
                {
                    angleIncrement = Random.Range(1f, 4f);
                }
                else
                {
                    var mult = i - initialScrapCount + 1;
                    angleIncrement = Random.Range(1f, 4f) * mult;
                }
                
                transform.RotateAround(planetPosition, Vector3.back, angleIncrement);
            }
        }
    }
}
