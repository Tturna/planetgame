using System;
using UnityEngine;

namespace Entities.Entities
{
    [Serializable]
    public class ProjectileData
    {
        public Sprite sprite;
        public float projectileSpeed;
        public float damage;
        public float knockback;
        public bool piercing;
        public bool useGravity;
        public float gravityMultiplier;
        public bool faceDirectionOfTravel;
        public bool collideWithWorld;
        public bool canHurtPlayer;
        public Gradient trailColor;
        public float trailTime;
    }
}