using System.Collections.Generic;
using System.Linq;
using Environment;
using Planets;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Entities.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private PlanetFauna.FaunaSpawnData[] spaceEnemies;
        [SerializeField] private float nighttimeSpawnRateMultiplier;
        [SerializeField] private float enemySpawnRateMultiplier = 1f;
        [SerializeField] private int enemySpawnCap = 10;
        public Vector2 spawnIntervalMinMax;
        
        private PlayerController player;
        private Rigidbody2D playerRigidbody;
        private float spawnTimer;
        private readonly List<GameObject> spawnedEnemyObjects = new();
        private int terrainMask;
        private PlanetFauna.FaunaSpawnData[] enemiesToSpawn;
        private bool playerIsInSpace;
        private float _spawnRateMultiplier;
        private int _spawnCap;
        
        private GameObject currentPlayerPlanetObject;
        private PlanetFauna currentPlanetFauna;
        private EnvironmentManager currentEnvironmentManager;
        private EnvironmentManager.TimeOfDay? currentTimeOfDay;

        private void Start()
        {
            player = PlayerController.instance;
            playerRigidbody = player.GetComponent<Rigidbody2D>();
            terrainMask = LayerMask.GetMask("Terrain");
            _spawnRateMultiplier = enemySpawnRateMultiplier;
            _spawnCap = enemySpawnCap;
            
            player.OnEnteredPlanet += SetCurrentPlanet;
        }

        private void OnDestroy()
        {
            player.OnEnteredPlanet -= SetCurrentPlanet;
        }

        private void Update()
        {
            if (!player)
            {
                Debug.LogError("Enemy Spawner can't access player.");
            }

            if (currentTimeOfDay != currentEnvironmentManager.AccurateTimeOfDay)
            {
                currentTimeOfDay = currentEnvironmentManager.AccurateTimeOfDay;
                var isDay = currentEnvironmentManager.IsDay;
                
                var alltimeFauna = currentPlanetFauna.alltimeFauna;
                PlanetFauna.FaunaSpawnData[] extraFauna;

                if (isDay)
                {
                    extraFauna = currentPlanetFauna.daytimeFauna;
                    _spawnRateMultiplier = enemySpawnRateMultiplier;
                }
                else
                {
                    extraFauna = currentPlanetFauna.nighttimeFauna;
                    _spawnRateMultiplier = enemySpawnRateMultiplier * nighttimeSpawnRateMultiplier;
                }

                if (alltimeFauna.Length == 0 && extraFauna.Length == 0)
                {
                    enemiesToSpawn = null;
                    return;
                }
                
                enemiesToSpawn = alltimeFauna.Concat(extraFauna).ToArray();
            }

            if (enemiesToSpawn == null || enemiesToSpawn.Length == 0) return;

            if (spawnTimer > 0f)
            {
                spawnTimer -= Time.deltaTime * _spawnRateMultiplier;
                return;
            }

            spawnTimer = Random.Range(spawnIntervalMinMax.x, spawnIntervalMinMax.y);
            
            for (var i = 0; i < spawnedEnemyObjects.Count; i++)
            {
                var spawnedEnemyObject = spawnedEnemyObjects[i];

                if (!spawnedEnemyObject || !spawnedEnemyObject.activeInHierarchy)
                {
                    spawnedEnemyObjects.Remove(spawnedEnemyObject);
                }
            }

            if (spawnedEnemyObjects.Count >= enemySpawnCap) return;
            
            var randomFaunaData = enemiesToSpawn[Random.Range(0, enemiesToSpawn.Length)];
            var rng = Random.Range(0f, 1f);
            
            if (rng > randomFaunaData.spawnChance) return;

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

            // var previousPoint = Vector2.zero;
            for (var a = startAngle; a < endAngle; a++)
            {
                var angle = a % 360;
                var unitX = Mathf.Cos(angle * Mathf.Deg2Rad) * 1.05f;
                var unitY = Mathf.Sin(angle * Mathf.Deg2Rad) * 0.75f;
                var wiggle = Mathf.Sin(angle * 0.5f);
                var relativeSpawnPoint = new Vector3(unitX, unitY) * (20f + wiggle * 3f);
                var spawnPoint = player.transform.position + relativeSpawnPoint;
                var halfHitboxHeight = randomFaunaData.enemySo.hitboxSize.y * 0.5f;
                float validAreaCheckRadius;
                
                if (randomFaunaData.enemySo.minSpawnAreaRadius > 0)
                {
                    validAreaCheckRadius = randomFaunaData.enemySo.minSpawnAreaRadius;
                }
                else
                {
                    validAreaCheckRadius = halfHitboxHeight;
                }

                if (randomFaunaData.enemySo.spawnInAir)
                {
                    var validAreaCheck = Physics2D.OverlapCircle(spawnPoint, validAreaCheckRadius, terrainMask);
                    
                    if (validAreaCheck) continue;
                    var enemyObject = Instantiate(enemyPrefab);
                    var enemy = enemyObject.GetComponent<EnemyEntity>();
                    enemy.Init(randomFaunaData.enemySo);
                    enemyObject.transform.position = spawnPoint;
                    spawnedEnemyObjects.Add(enemyObject);
                    break;
                }

                if (playerIsInSpace) break;

                var planetDirection = (player.CurrentPlanetObject!.transform.position - spawnPoint).normalized;
                var hit = Physics2D.Raycast(spawnPoint, planetDirection, 5f, terrainMask);
                
                if (!hit) continue;

                var spawnPointOnPlanet = (Vector3)hit.point - planetDirection * (validAreaCheckRadius + 0.05f);
                var openAreaCheckHit = Physics2D.OverlapCircle(spawnPointOnPlanet, validAreaCheckRadius, terrainMask);
                
                if (openAreaCheckHit) continue;

                var enemyObjectOnPlanet = Instantiate(enemyPrefab);
                var enemyOnPlanet = enemyObjectOnPlanet.GetComponent<EnemyEntity>();
                enemyOnPlanet.Init(randomFaunaData.enemySo);
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

        private void SetCurrentPlanet(GameObject currentPlanetObject)
        {
            if (!currentPlanetObject)
            {
                currentPlayerPlanetObject = null;
                currentPlanetFauna = null;
                currentEnvironmentManager = null;
                enemiesToSpawn = spaceEnemies;
                playerIsInSpace = true;
                return;
            }
            
            currentPlayerPlanetObject = currentPlanetObject;
            currentPlanetFauna = currentPlayerPlanetObject.GetComponentInChildren<PlanetFauna>();
            currentEnvironmentManager = currentPlayerPlanetObject.GetComponentInChildren<EnvironmentManager>();
            playerIsInSpace = false;
            
            if (!currentPlanetFauna)
            {
                Debug.LogError("Current planet object doesn't have a PlanetFauna component.");
                return;
            }
            
            if (!currentEnvironmentManager)
            {
                Debug.LogError("Current planet object doesn't have an EnvironmentManager component.");
                return;
            }
            
            enemiesToSpawn = currentPlanetFauna.alltimeFauna;
        }
    }
}
