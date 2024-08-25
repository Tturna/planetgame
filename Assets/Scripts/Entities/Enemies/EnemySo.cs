using System;
using Inventory;
using UnityEngine;
using Utilities;

namespace Entities.Enemies
{
    [CreateAssetMenu(fileName = "Enemy", menuName = "SO/Enemy")]
    public class EnemySo : ScriptableObject
    {
        [Header("General")]
        public string enemyName;
        public float maxHealth;
        public float health;
        public float wakeupDelay;
        public float deathDelay;
        public float despawnTime;
        public bool spawnInAir;
        [Tooltip("The radius of the valid area check circle for the enemy to spawn. Leave 0 to use half the hitbox height.")] public float minSpawnAreaRadius;
        public float contactDamage;
        public bool isImmuneToKnockback;
        public float knockback;
        public float aggroRange;
        public float accelerationSpeed;
        public float maxSpeed;
        public float maxSlopeMultiplier;
        public bool canJump;
        public float jumpForce;
        public bool ignoreGravity;
        public MovementPattern movementPattern;
        public PhysicsMaterial2D physicsMaterial;
        public bool faceMovementDirection;
        public bool isBoss;
        public bool flipSprite;
        public Sprite bossPortrait;
        public Vector2 hitboxOffset;
        public Vector2 hitboxSize;
        public float hitboxEdgeRadius;
        public Vector2 knockbackSourcePointOffset;
        public float healthbarDistance;
        public RuntimeAnimatorController overrideAnimator;
        public Color hitPfxColor;
        public Vector2 hitSquishStretchMultiplier;
        public AudioUtilities.Clip[] hitSounds;
        public AudioUtilities.Clip[] deathSounds;
        
        [Tooltip("How long the player has to be outside the aggro range for the enemy to deaggro")]
        public float evasionTime;
        
        [Header("Attacks")]
        public float attackInterval;
        public float attackRecoveryTime;

        [Tooltip("If true, the enemy will always attack, regardless of distance")]
        public bool alwaysAttack;
        public bool useRandomAttack;
        public AttackPattern[] attacks;

        [Header("Loot")]
        public bool dropMultiple;
        public LootDrop[] lootTable;
    }
}