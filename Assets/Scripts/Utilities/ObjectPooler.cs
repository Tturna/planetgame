using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Utilities
{
    public static class ObjectPooler
    {
        private struct PoolData
        {
            public List<GameObject> pool;
            public GameObject stencilObject;
            public int backupObjectIndex;
            public bool dynamicSize;
        }
        
        private static readonly Dictionary<string, PoolData> PoolsDict = new();

        public static void CreatePool(string poolName, GameObject stencilObject, int initialPoolSize, bool dynamicSize = false)
        {
            if (string.IsNullOrEmpty(poolName))
            {
                throw new ArgumentException("Pool name cannot be null or empty!");
            }
            
            if (stencilObject == null)
            {
                throw new ArgumentException("Stencil object cannot be null!");
            }
            
            if (PoolsDict.ContainsKey(poolName))
            {
                return;
            }
            
            if (initialPoolSize < 1)
            {
                throw new ArgumentException("Initial pool size must be at least 1!");
            }

            var pool = new List<GameObject>();
            
            for (var i = 0; i < initialPoolSize; i++)
            {
                var obj = UnityEngine.Object.Instantiate(stencilObject);
                obj.SetActive(false);
                pool.Add(obj);
            }
            
            var poolData = new PoolData()
            {
                pool = pool,
                stencilObject = stencilObject,
                backupObjectIndex = 0,
                dynamicSize = dynamicSize
            };
            
            PoolsDict.Add(poolName, poolData);
        }
        
        [CanBeNull]
        public static GameObject GetObject(string poolName)
        {
            if (!PoolsDict.ContainsKey(poolName))
            {
                Debug.LogError($"Pool with name '{poolName}' does not exist!");
                return null;
            }
            
            var poolData = PoolsDict[poolName];
            var pool = poolData.pool;

            foreach (var obj in pool)
            {
                if (obj.activeSelf) continue;
                obj.SetActive(true);
                return obj;
            }

            if (poolData.dynamicSize)
            {
                var obj = UnityEngine.Object.Instantiate(poolData.stencilObject);
                pool.Add(obj);

                return obj;
            }
            
            var backupObject = pool[poolData.backupObjectIndex];
            poolData.backupObjectIndex++;
            if (poolData.backupObjectIndex >= pool.Count) poolData.backupObjectIndex = 0;
            
            // This is required because structs are value types, not reference types
            PoolsDict[poolName] = poolData;
            
            return backupObject;
        }
    }
}
