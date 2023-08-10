using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities
{
    public static class ObjectPooler
    {
        private struct PoolData
        {
            public GameObject[] pool;
            public int backupObjectIndex;
        }
        
        private static readonly Dictionary<Guid, PoolData> PoolsDict = new();

        /// <summary>
        /// Create a new object pool for the given object and return the pool's unique ID
        /// </summary>
        /// <param name="stencilObject"></param>
        /// <param name="poolSize"></param>
        /// <returns></returns>
        public static Guid CreatePool(GameObject stencilObject, int poolSize)
        {
            var pool = new GameObject[poolSize];
            var id = Guid.NewGuid();
            
            for (var i = 0; i < poolSize; i++)
            {
                pool[i] = UnityEngine.Object.Instantiate(stencilObject);
                pool[i].SetActive(false);
            }
            
            var poolData = new PoolData()
            {
                pool = pool,
                backupObjectIndex = 0
            };
            
            PoolsDict.Add(id, poolData);
            
            return id;
        }
        
        /// <summary>
        /// Return an available object from the pool with the given ID, or loop through the pool if none are available
        /// </summary>
        /// <param name="poolId"></param>
        /// <returns></returns>
        public static GameObject GetObject(Guid poolId)
        {
            if (!PoolsDict.ContainsKey(poolId))
            {
                Debug.LogError($"Pool with ID {poolId} does not exist!");
                return null;
            }
            
            var poolData = PoolsDict[poolId];
            var pool = poolData.pool;

            foreach (var obj in pool)
            {
                if (obj == null || obj.activeSelf) continue;
                
                obj.SetActive(true);
                return obj;
            }
            
            var backupObject = pool[poolData.backupObjectIndex];
            poolData.backupObjectIndex++;
            if (poolData.backupObjectIndex >= pool.Length) poolData.backupObjectIndex = 0;
            
            // This is required because structs are value types, not reference types
            PoolsDict[poolId] = poolData;
            
            return backupObject;
        }
    }
}
