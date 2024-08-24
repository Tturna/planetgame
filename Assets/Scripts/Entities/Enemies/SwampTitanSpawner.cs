using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Entities.Enemies
{
    public class SwampTitanSpawner : EntityController
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private EnemySo swampTitanSo;

        private bool _spawned;
        private List<(Vector3, Vector3)> _spawnPointNormalPairs = new();
        private PlayerController _player;
        private SpriteRenderer _spriteRenderer;
        private bool _playerBelowHalfway;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private GameObject _titan;
        
        private static SwampTitanSpawner _instance;

        protected override void Start()
        {
            _instance = this;
            
            base.Start();
            TogglePhysics(false);
            ToggleAutoRotation(false);
            ToggleCollision(false);
            ToggleSpriteRenderer(false);
            
            _player = PlayerController.instance;
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            
            // Assume this object is below the planet in world space.
            // Just fire rays in like a 45 degree cone upwards to find valid spawn points.
            // Check when the player goes below the half way point in the planet and
            // randomly enable one of the spawners. In practice we probably just
            // move this spawner object to the valid position and call it a day.

            var terrainMask = LayerMask.GetMask("Terrain");
            var coneRange = 45f;
            var rayCount = 10;

            for (var i = 0; i < rayCount; i++)
            {
                var angle = 90f - coneRange / 2f + coneRange / rayCount * i;
                var x = Mathf.Cos(angle * Mathf.Deg2Rad);
                var y = Mathf.Sin(angle * Mathf.Deg2Rad);
                var direction = new Vector3(x, y);
                
                var hit = Physics2D.Raycast(transform.position, direction, 164f, terrainMask);
                if (!hit) continue;
                
                _spawnPointNormalPairs.Add((hit.point, hit.normal));
            }
            
            if (_spawnPointNormalPairs.Count == 0)
            {
                Debug.LogError("No valid spawn points found for swamp titan spawner.");
            }
        }

        private void Update()
        {
            if (_spawned) return;
            if (_player.IsInSpace) return;
            
            var playerDot = Vector3.Dot(_player.DirectionToClosestPlanet, Vector3.up);
            
            if (!_playerBelowHalfway && playerDot > 0f)
            {
                _playerBelowHalfway = true;
                
                var rng = Random.Range(0f, 1f);
                var spawnBoss = rng < 0.5f;
                
                Debug.Log($"spawn boss: {spawnBoss}");

                if (spawnBoss)
                {
                    var pair = _spawnPointNormalPairs[Random.Range(0, _spawnPointNormalPairs.Count)];
                    ToggleSpriteRenderer(true);
                    transform.position = pair.Item1;
                    transform.up = pair.Item2;
                }
            }
            else if (_playerBelowHalfway && playerDot < 0f)
            {
                _playerBelowHalfway = false;
                transform.position = _initialPosition;
                transform.rotation = _initialRotation;
            }
            
            if (!_playerBelowHalfway) return;
            
            var diffToPlayer = PlayerController.instance.transform.position - transform.position;
            var distanceToPlayer = diffToPlayer.magnitude;

            if (distanceToPlayer >= 10f) return;
            
            _titan = Instantiate(enemyPrefab, transform.position, transform.rotation, null);

            try
            {
                _titan.GetComponent<EnemyEntity>().Init(swampTitanSo);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while spawning swamp titan: " + e.Message);
            }
            
            ToggleSpriteRenderer(false);
            _spawned = true;
        }

        public override void ToggleSpriteRenderer(bool state)
        {
            if (!_spriteRenderer)
            {
                if (!TryGetComponent(out _spriteRenderer))
                {
                    Debug.LogError("Failed to get sprite renderer component.");
                    return;
                }
            }
            
            _spriteRenderer.enabled = state;
        }

        public static void ResetSwampTitanSpawner()
        {
            _instance._spawned = false;
            _instance._titan.GetComponentInChildren<HealthbarManager>().ToggleBossUIHealth(false);
            _instance.transform.position = _instance._initialPosition;
            _instance.transform.rotation = _instance._initialRotation;
            Destroy(_instance._titan);
        }
    }
}
