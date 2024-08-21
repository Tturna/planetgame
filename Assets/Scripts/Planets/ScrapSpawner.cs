using Inventory;
using Inventory.Entities;
using Inventory.Item_SOs;
using UnityEngine;

namespace Planets
{
    public class ScrapSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject breakablePrefab;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private ItemSo[] secondShipwreckLoot;
        [SerializeField] private ItemSo scrapSo;
        [SerializeField] private Sprite shipwreckOneSprite;
        [SerializeField] private Sprite shipwreckTwoSprite;
        
        private void Start()
        {
            // Rundown:
            // 1. Assume this object is above the planet in Unity's 2D space
            // 2. Raycast down to get the planet
            // 2.1. Spawn the first shipwreck
            // 3. Rotate a bit to the left using the planet center as the axis of rotation
            // 4. Raycast down to get the planet's surface to start placing scrap a bit behind the shipwreck
            // (assuming the shipwreck will be in the center of the planet or rotation 0 in Unity's 2D space)
            // 5. Place scrap on the planet's surface
            // 6. Increment the rotation by a small random amount for a bit and place scraps
            // 7. Increase the rotation increment so the scrap gets more and more scarce
            // 8. Spawn the second shipwreck at the end of the scrap trail

            var planetMask = 1 << LayerMask.NameToLayer("Terrain");
            const float rayLength = 30f;
            var initialScrapCount = Random.Range(10, 15);
            var trailScrapCount = Random.Range(7, 15);
            
            var hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, planetMask);

            if (!hit)
            {
                Debug.LogError("Scrap Spawner couldn't find a planet below it.");
                return;
            }

            var planetTransform = hit.transform.root;
            var planetPosition = planetTransform.position;
            
            var shipwreckOne = new GameObject("Shipwreck One");
            var shipwreckOneSr = shipwreckOne.AddComponent<SpriteRenderer>();
            shipwreckOne.transform.SetParent(planetTransform);
            shipwreckOneSr.sprite = shipwreckOneSprite;
            shipwreckOneSr.sortingOrder = 1;
            shipwreckOne.transform.position = hit.point + hit.normal * shipwreckOneSprite.bounds.size.y / 4f;
            shipwreckOne.transform.up = hit.normal;

            transform.RotateAround(planetPosition, Vector3.back, -10f);

            for (var i = 0; i < initialScrapCount + trailScrapCount; i++)
            {
                var planetDirection = (planetPosition - transform.position).normalized;
                hit = Physics2D.Raycast(transform.position, planetDirection, rayLength, planetMask);

                if (hit)
                {
                    var scrap = Instantiate(breakablePrefab, planetTransform, true);
                    scrap.GetComponent<SpriteRenderer>().sprite = scrapSo.sprite;
                    scrap.GetComponent<BoxCollider2D>().size = scrapSo.sprite.bounds.size;
                    scrap.GetComponent<BreakableItemInstance>().itemSo = scrapSo;
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
                    angleIncrement = Random.Range(1f, 3f) * mult;
                }
                
                transform.RotateAround(planetPosition, Vector3.back, angleIncrement);
            }
            
            var planetDir = (planetPosition - transform.position).normalized;
            hit = Physics2D.Raycast(transform.position, planetDir, rayLength, planetMask);
            var shipwreckTwo = new GameObject("Shipwreck Two");
            var shipwreckTwoSr = shipwreckTwo.AddComponent<SpriteRenderer>();
            shipwreckTwo.transform.SetParent(planetTransform);
            shipwreckTwoSr.sprite = shipwreckTwoSprite;
            shipwreckTwoSr.sortingOrder = 1;
            shipwreckTwo.transform.position = hit.point + hit.normal * shipwreckTwoSprite.bounds.size.y / 4f;
            shipwreckTwo.transform.up = hit.normal;

            var horizontalOffset = -Random.Range(1f, 2f) * secondShipwreckLoot.Length / 2f;
            
            foreach (var item in secondShipwreckLoot)
            {
                Vector3 itemPosition = hit.point + hit.normal * item.sprite.bounds.size.y;
                itemPosition += transform.right * horizontalOffset;
                // here you could use InventoryManager.SpawnItem but it's after this in script
                // execution order.
                var itemEntity = Instantiate(itemPrefab);
                itemEntity.GetComponent<ItemEntity>().item = new Item(item);
                itemEntity.transform.position = itemPosition;
                horizontalOffset += Random.Range(1f, 2f);
            }
        }
    }
}
