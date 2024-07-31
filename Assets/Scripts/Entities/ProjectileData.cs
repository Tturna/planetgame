using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Entities
{
    [Serializable]
    public class ProjectileData
    {
        public Sprite sprite;
        public string sortingLayerName;
        public int sortingOrder;
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
        [Tooltip("Particles to spawn when the projectile breaks. Leave empty for default pfx.")]
        public GameObject breakParticlePrefab;
        public Color breakParticleColor;
        public Gradient trailColor;
        public float trailTime;
        public Vector2 trailStartEndWidth;
        public bool useLight;
        public Color lightColor;
        [Tooltip("Animation to play when the projectile spawns. Leave empty for no animation.")]
        public AnimationClip spawnAnimation;
        [Tooltip("Animation to play after spawn animation while the projectile exists. Leave empty for no animation.")]
        public AnimationClip updateAnimation;
    }
}