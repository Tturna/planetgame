using System;
using Entities.Enemies;
using UnityEngine;
using UnityEngine.Serialization;

namespace Planets
{
    public class PlanetFauna : MonoBehaviour
    {
        [Serializable]
        public struct FaunaSpawnData
        {
            public EnemySo enemySo;
            [Range(0, 1)] public float spawnChance;
        }
        
        public FaunaSpawnData[] alltimeFauna;
        [FormerlySerializedAs("spawnableEnemies")] public FaunaSpawnData[] daytimeFauna;
        public FaunaSpawnData[] nighttimeFauna;
    }
}