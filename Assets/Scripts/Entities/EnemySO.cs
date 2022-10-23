using UnityEngine;

namespace Entities
{
    [CreateAssetMenu(fileName = "Enemy", menuName = "SO/Enemy")]
    public class EnemySO : ScriptableObject
    {
        public float maxHealth;
        public float health;
        public float contactDamage;
        public float knockback;
        public float aggroRange;
        public float moveSpeed;
        
        [Tooltip("How long the player has to be outside the aggro range for the enemy to deaggro")]
        public float evasionTime;
        public float attackInterval;
        public float attackRecoveryTime;
        
        [Tooltip("How far the player has to be for the enemy to use a long attack")]
        public float attackRangeThreshold;

        public bool isBoss;
        public bool flipSprite;
        public Sprite bossPortrait;
        public Vector2 hitboxOffset;
        public Vector2 hitboxSize;

        public RuntimeAnimatorController overrideAnimator;
        public AttackPattern[] shortAttacks;
        public AttackPattern[] longAttacks;
    }
}