using System;
using UnityEngine;

namespace Entities.Enemies
{
    public class SwampTitanSpawner : EntityController
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private EnemySo swampTitanSo;

        private bool _spawned;

        private void Update()
        {
            if (_spawned) return;
            
            var diffToPlayer = PlayerController.instance.transform.position - transform.position;
            var distanceToPlayer = diffToPlayer.magnitude;

            if (distanceToPlayer >= 10f) return;
            
            var titan = Instantiate(enemyPrefab, transform.position, transform.rotation, null);

            try
            {
                titan.GetComponent<EnemyEntity>().Init(swampTitanSo);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while spawning swamp titan: " + e.Message);
            }
            
            gameObject.SetActive(false);
            _spawned = true;
        }
    }
}
