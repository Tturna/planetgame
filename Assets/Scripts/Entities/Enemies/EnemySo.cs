using System;
using Inventory;
using UnityEngine;

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
        public float contactDamage;
        public bool isImmuneToKnockback;
        public float knockback;
        public float aggroRange;
        public float accelerationSpeed;
        public float maxSpeed;
        public float maxSlopeMultiplier;
        public bool canJump;
        public float jumpForce;
        public MovementPattern movementPattern;
        public bool isBoss;
        public bool flipSprite;
        public Sprite bossPortrait;
        public Vector2 hitboxOffset;
        public Vector2 hitboxSize;
        public float hitboxEdgeRadius;
        public Vector2 knockbackSourcePointOffset;
        public float healthbarDistance;
        public RuntimeAnimatorController overrideAnimator;
        
        [Tooltip("How long the player has to be outside the aggro range for the enemy to deaggro")]
        public float evasionTime;
        
        [Header("Attacks")]
        public float attackInterval;
        public float attackRecoveryTime;

        [Tooltip("If true, the enemy will always attack, regardless of distance")]
        public bool alwaysAttack;
        public bool useRandomAttack;
        public AttackPattern[] attacks;
        
        [Serializable]
        public struct LootDrop
        {
            public Item item;
            [Range(0f, 100f)] public float dropChance;
        }

        [Header("Loot")]
        public bool dropMultiple;
        public LootDrop[] lootTable;
    }
}