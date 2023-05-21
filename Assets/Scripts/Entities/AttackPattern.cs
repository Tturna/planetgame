using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;

namespace Entities
{
    [Serializable]
    public class AttackPattern
    {
        public enum AttackFunctions { Punch, Dash }

        public Dictionary<AttackFunctions, Action<EnemyEntity>> FunctionLookupTable => new()
        {
            { AttackFunctions.Punch, Punch },
            { AttackFunctions.Dash, Dash }
        };
        
        public AttackFunctions attackFunction;
        [FormerlySerializedAs("attackIndex")] public int animationIndex;
        public float damage;
        public float knockback;
        [FormerlySerializedAs("attackStrength")] public float attackDistance;
        public float attackDelay;

        public Action<EnemyEntity> GetAttack()
        {
            return FunctionLookupTable[attackFunction];
        }

        public int GetIndex()
        {
            return animationIndex;
        }

        public void Punch(EnemyEntity enemy)
        {
            void Action()
            {
                if (!enemy) return; // Check if enemy is dead
                
                var dir = enemy.GetVectorToPlayer().normalized;

                var hit = Physics2D.Raycast(enemy.transform.position, dir, attackDistance, 1);

                if (!hit || !hit.transform.root.TryGetComponent<PlayerController>(out var player)) return;
                player.TakeDamage(damage);
                player.Knockback(enemy.transform.position, knockback);
            }

            if (attackDelay > 0)
            {
                GameUtilities.instance.DelayExecute(Action, attackDelay);
            }
            else
            {
                Action();
            }
        }
        
        public void Dash(EnemyEntity enemy)
        {
            void Action()
            {
                enemy.AddRelativeForce(enemy.relativeMoveDirection * attackDistance, ForceMode2D.Impulse);
            }

            if (attackDelay > 0)
            {
                GameUtilities.instance.DelayExecute(Action, attackDelay);
            }
            else
            {
                Action();
            }
        }
    }
}
