using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Planets;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Entities.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private EnemySo[] spaceEnemies;
        public float enemySpawnRateMultiplier = 1f;
        public int enemySpawnCap = 10;
        public Vector2 spawnIntervalMinMax;
        
        private PlayerController player;
        private Rigidbody2D playerRigidbody;
        private GameObject currentPlayerPlanetObject;
        [CanBeNull] private PlanetFauna currentPlanetFauna;
        private float spawnTimer;
        private readonly List<GameObject> spawnedEnemyObjects = new();
        private int terrainMask;
        private EnemySo[] enemiesToSpawn;
        private bool playerIsInSpace;

        private void Start()
        {
            player = PlayerController.instance;
            playerRigidbody = player.GetComponent<Rigidbody2D>();
            terrainMask = LayerMask.GetMask("Terrain");
        }

        private void Update()
        {
            if (!player)
            {
                Debug.LogError("Enemy Spawner can't access player.");
            }

            if (currentPlayerPlanetObject != player.CurrentPlanetObject)
            {
                currentPlayerPlanetObject = player.CurrentPlanetObject;

                if (currentPlayerPlanetObject)
                {
                    currentPlanetFauna = currentPlayerPlanetObject.GetComponent<PlanetFauna>();
                    playerIsInSpace = false;
                }
                else
                {
                    currentPlanetFauna = null;
                    playerIsInSpace = true;
                }
                
                if (!playerIsInSpace)
                {
                    enemiesToSpawn = currentPlanetFauna!.spawnableEnemies;
                }
                else
                {
                    enemiesToSpawn = spaceEnemies;
                }
            }

            if (enemiesToSpawn.Length == 0) return;

            if (spawnTimer > 0f)
            {
                spawnTimer -= Time.deltaTime * enemySpawnRateMultiplier;
                return;
            }

            spawnTimer = Random.Range(spawnIntervalMinMax.x, spawnIntervalMinMax.y);
            
            // Debug.Log("Spawning enemy...");
            
            for (var i = 0; i < spawnedEnemyObjects.Count; i++)
            {
                var spawnedEnemyObject = spawnedEnemyObjects[i];

                if (!spawnedEnemyObject || !spawnedEnemyObject.activeInHierarchy)
                {
                    spawnedEnemyObjects.Remove(spawnedEnemyObject);
                }
            }

            if (spawnedEnemyObjects.Count >= enemySpawnCap)
            {
                // Debug.Log("Enemy cap reached. Spawning skipped.");
                return;
            }

            var playerVelocity = playerRigidbody.velocity;
            var playerVelocityDirection = playerVelocity.normalized;
            var velocityAngle = Mathf.Atan2(playerVelocityDirection.y, playerVelocityDirection.x) * Mathf.Rad2Deg;
            var spawnInMoveDirection = Random.Range(0f, 1f) < 0.5f;
            float startAngle, endAngle;

            if (spawnInMoveDirection && playerVelocity.magnitude > 0.1f)
            {
                startAngle = velocityAngle - 15f;
                endAngle = velocityAngle - 15f + 360f;
            }
            else
            {
                startAngle = Random.Range(0f, 360f);
                endAngle = startAngle + 360f;
            }
            
            var randomEnemySo = enemiesToSpawn[Random.Range(0, enemiesToSpawn.Length)];
            // Debug.Log($"Spawning {randomEnemySo.enemyName}...");

            // var previousPoint = Vector2.zero;
            
            for (var a = startAngle; a < endAngle; a++)
            {
                var angle = a % 360;
                var unitX = Mathf.Cos(angle * Mathf.Deg2Rad) * 1.05f;
                var unitY = Mathf.Sin(angle * Mathf.Deg2Rad) * 0.75f;
                var wiggle = Mathf.Sin(angle * 0.5f);
                var relativeSpawnPoint = new Vector3(unitX, unitY) * (20f + wiggle * 3f);
                var spawnPoint = player.transform.position + relativeSpawnPoint;

                if (randomEnemySo.spawnInAir)
                {
                    // Debug.Log("Spawning enemy in air...");
                    var enemyObject = Instantiate(enemyPrefab);
                    var enemy = enemyObject.GetComponent<EnemyEntity>();
                    enemy.Init(randomEnemySo);
                    enemyObject.transform.position = spawnPoint;
                    spawnedEnemyObjects.Add(enemyObject);
                    break;
                }

                if (playerIsInSpace)
                {
                    // Debug.LogError("Player is in space but enemy is not set to spawn in air.");
                    break;
                }

                var planetDirection = (player.CurrentPlanetObject!.transform.position - spawnPoint).normalized;
                var hit = Physics2D.Raycast(spawnPoint, planetDirection, 5f, terrainMask);
                
                if (!hit)
                {
                    // Debug.Log("Ground check raycast failed.");
                    continue;
                }

                var halfHitboxHeight = randomEnemySo.hitboxSize.y * 0.5f;
                var spawnPointOnPlanet = (Vector3)hit.point - planetDirection * (halfHitboxHeight + 0.05f);
                var openAreaCheckRadius = halfHitboxHeight;
                var openAreaCheckHit = Physics2D.OverlapCircle(spawnPointOnPlanet, openAreaCheckRadius, terrainMask);
                
                if (openAreaCheckHit)
                {
                    continue;
                }

                var enemyObjectOnPlanet = Instantiate(enemyPrefab);
                var enemyOnPlanet = enemyObjectOnPlanet.GetComponent<EnemyEntity>();
                enemyOnPlanet.Init(randomEnemySo);
                enemyObjectOnPlanet.transform.position = spawnPointOnPlanet;
                spawnedEnemyObjects.Add(enemyObjectOnPlanet);
                break;

                // if (previousPoint != Vector2.zero)
                // {
                //     var color = a < startAngle + 15f ? Color.green : Color.red;
                //     Debug.DrawLine(previousPoint, spawnPoint, color);
                // }

                // previousPoint = spawnPoint;
            }
        }
    }
}
