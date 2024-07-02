using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;

namespace Entities.Enemies
{
    [Serializable]
    public class AttackPattern
    {
        public enum AttackFunctions
        {
            Punch,
            Dash,
            Shoot
        }

        public Dictionary<AttackFunctions, Action<EnemyEntity, Vector3>> FunctionLookupTable => new()
        {
            { AttackFunctions.Punch, Punch },
            { AttackFunctions.Dash, Dash },
            { AttackFunctions.Shoot, Shoot }
        };
        
        public AttackFunctions attackFunction;
        [FormerlySerializedAs("attackIndex")] public int animationIndex;
        public float damage;
        public float knockback;
        [FormerlySerializedAs("attackStrength")] public float attackDistance;
        [Tooltip("Delay before attack is executed. Probably deprecated soon.")]
        public float attackDelay;
        public bool preventsMovement;
        public ProjectileData[] projectiles;

        public Action<EnemyEntity, Vector3> GetAttack()
        {
            return FunctionLookupTable[attackFunction];
        }

        public int GetIndex()
        {
            return animationIndex;
        }

        public void Punch(EnemyEntity enemy, Vector3 direction)
        {
            if (attackDelay > 0)
            {
                GameUtilities.instance.DelayExecute(Action, attackDelay);
            }
            else
            {
                Action();
            }

            return;

            void Action()
            {
                if (!enemy) return;
                
                var dir = enemy.GetVectorToPlayer().normalized;

                var hit = Physics2D.Raycast(enemy.transform.position, dir, attackDistance, 1);

                if (!hit || !hit.transform.root.TryGetComponent<PlayerController>(out var player)) return;
                player.TakeDamage(damage);
                player.Knockback(enemy.transform.position, knockback);
            }
        }
        
        public void Dash(EnemyEntity enemy, Vector3 direction)
        {
            if (attackDelay > 0)
            {
                GameUtilities.instance.DelayExecute(Action, attackDelay);
            }
            else
            {
                Action();
            }

            return;

            void Action()
            {
                enemy.AddRelativeForce(enemy.relativeMoveDirection * attackDistance, ForceMode2D.Impulse);
            }
        }

        private GameUtilities _gameUtilities;
        private GameObject _projectilePrefab;
        public void Shoot(EnemyEntity enemy, Vector3 direction)
        {
            if (_gameUtilities == null)
            {
                _gameUtilities = GameUtilities.instance;
            }
            
            if (_projectilePrefab == null)
            {
                _projectilePrefab = _gameUtilities.GetProjectilePrefab();
            }

            if (attackDelay > 0)
            {
                GameUtilities.instance.DelayExecute(Action, attackDelay);
            }
            else
            {
                Action();
            }

            return;

            void Action()
            {
                var rot = enemy.transform.eulerAngles;
                rot.z = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                var projectile = GameUtilities.Spawn(_projectilePrefab, enemy.transform.position + direction, rot, null);
                var entity = projectile.GetComponent<ProjectileEntity>();
                entity.Init(projectiles[0]);
            }
        }
    }
}
