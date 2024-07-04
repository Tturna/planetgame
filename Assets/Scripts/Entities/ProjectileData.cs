using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Entities
{
    [Serializable]
    public class ProjectileData
    {
        public Sprite sprite;
        [FormerlySerializedAs("projectileSpeed")] public float minProjectileSpeed;
        public float maxProjectileSpeed;
        public float lifetime;
        public float damage;
        public float knockback;
        public float critChance;
        public bool piercing;
        public bool useGravity;
        public float gravityMultiplier;
        public bool faceDirectionOfTravel;
        public bool collideWithWorld;
        public bool canHurtPlayer;
        public bool canHurtEnemies;
        public float collisionActivationDelay;
        public Color breakParticleColor;
        public Gradient trailColor;
        public float trailTime;
        public bool useLight;
        public Color lightColor;
    }
}