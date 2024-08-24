using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Inventory
{
    public class LootboxSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject lootboxPrefab;
        [SerializeField] private LootDrop[] items;
        [SerializeField] private Vector2Int lootCountPerBoxMinMax;
        [SerializeField, Range(1, 360)] private int maxLootboxes;
        public float wiggleMult;
        public float wiggleAmount;
        public float baseDistance;

        private void Start()
        {
            var terrainMask = 1 << LayerMask.NameToLayer("Terrain");
            List<KeyValuePair<Vector3, Vector3>> validPositionNormalPairs = new();
            var lootboxParent = new GameObject("Lootbox Parent");
            lootboxParent.transform.SetParent(transform.parent);
            var startAngle = Random.Range(0, 360);
            
            for (var i = startAngle; i < startAngle + 360; i++)
            {
                var angle = i % 360;
                var x = Mathf.Cos(angle * Mathf.Deg2Rad);
                var y = Mathf.Sin(angle * Mathf.Deg2Rad);
                var wiggle = Mathf.Sin(angle * wiggleMult) * wiggleAmount;
                var direction = new Vector3(x, y);
                var position = transform.position + direction * baseDistance;
                position += direction * wiggle;

                var hit = Physics2D.Raycast(position, -direction, 5f, terrainMask);
                
                if (!hit) continue;
                
                var spawnPoint = (Vector3)hit.point + direction * 0.5f;
                var openAreaCheckHit = Physics2D.OverlapCircle(spawnPoint, 0.4f, terrainMask);
                
                if (openAreaCheckHit) continue;

                if (maxLootboxes < 360)
                {
                    validPositionNormalPairs.Add(new KeyValuePair<Vector3, Vector3>(spawnPoint, hit.normal));
                }
                else
                {
                    SpawnLootBox(spawnPoint, hit.normal, lootboxParent.transform);
                }
            }
            
            if (maxLootboxes == 360) return;
            
            var ratio = 360f / maxLootboxes;
            var limitedPosition = 0f;

            for (var i = 0; i < maxLootboxes; i++)
            {
                var idx = Mathf.FloorToInt(limitedPosition);
                
                if (idx >= validPositionNormalPairs.Count)
                {
                    break;
                }
                
                var positionNormalPair = validPositionNormalPairs[idx];
                SpawnLootBox(positionNormalPair.Key, positionNormalPair.Value, lootboxParent.transform);
                limitedPosition += ratio;
            }
        }

        private void SpawnLootBox(Vector3 spawnPoint, Vector3 normal, Transform parent)
        {
            var trapRng = Random.Range(0, 10);
            var isTrap = trapRng == 0;
            var lootbox = Instantiate(lootboxPrefab, parent);
            lootbox.transform.position = spawnPoint;
            lootbox.transform.up = normal;

            if (!isTrap)
            {
                var lootCount = Random.Range(lootCountPerBoxMinMax.x, lootCountPerBoxMinMax.y);
                List<LootDrop> lootItems = new();

                for (var j = 0; j < lootCount; j++)
                {
                    lootItems.Add(items[Random.Range(0, items.Length)]);
                }
                
                lootbox.GetComponent<LootboxManager>().Init(lootItems.ToArray(), false);
            }
            else
            {
                lootbox.GetComponent<LootboxManager>().Init(null, true);
            }
        }
    }
}
