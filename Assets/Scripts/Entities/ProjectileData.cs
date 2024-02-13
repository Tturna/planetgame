using System;
using UnityEngine;

namespace Entities
{
    [Serializable]
    public class ProjectileData
    {
        public Sprite sprite;
        public float projectileSpeed;
        public float damage;
        public float knockback;
        public float critChance;
        public bool piercing;
        public bool useGravity;
        public float gravityMultiplier;
        public bool faceDirectionOfTravel;
        public bool collideWithWorld;
        public bool canHurtPlayer;
        public Color breakParticleColor;
        public Gradient trailColor;
        public float trailTime;
    }
}