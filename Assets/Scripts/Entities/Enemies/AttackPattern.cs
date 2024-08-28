#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using Cameras;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Entities.Enemies
{
    [Serializable]
    public class AttackPattern
    {
        public enum AttackFunctions
        {
            Punch,
            Dash,
            Shoot,
            Teleport
        }

        public enum ReferencePoints
        {
            Self,
            Player
        }

        [Serializable]
        public struct ProjectileAttackData
        {
            public ProjectileData projectileData;
            public ReferencePoints aimReferencePoint;
            public ReferencePoints spawnReferencePoint;
            public float minAimAngleOffset;
            public float maxAimAngleOffset;
            [FormerlySerializedAs("projectileSpawnOffset")] public Vector2 minProjectileSpawnOffset;
            public Vector2 maxProjectileSpawnOffset;
            public bool tryStartFromGround;
            public bool tryOrientToGround;
        }
        
        [Serializable]
        public struct AttackParticleData
        {
            public GameObject attackParticlePrefab;
            public Vector2 attackParticleOffset;
            public bool alignAttackParticleToHorizontalDirection;
        }

        public Dictionary<AttackFunctions, Func<EnemyEntity, Vector3, Action?>> FunctionLookupTable => new()
        {
            { AttackFunctions.Punch, Punch },
            { AttackFunctions.Dash, Dash },
            { AttackFunctions.Shoot, Shoot },
            { AttackFunctions.Teleport, Teleport }
        };
        
        [UsedImplicitly] public string attackName = null!;
        public AttackFunctions attackFunction;
        [FormerlySerializedAs("attackIndex")] public int animationIndex;
        public float damage;
        public float knockback;
        public float attackRange;
        [FormerlySerializedAs("attackDistance")] [FormerlySerializedAs("attackStrength")] public float attackMagnitude;
        public float attackTime;
        public float attackDelay;
        public bool preventsMovement;
        public float cameraShakeTime;
        public float cameraShakeStrength;
        public ProjectileAttackData[]? projectileAttacks;
        public bool useRandomProjectileAttack;
        public int minProjectileAttacks;
        public int maxProjectileAttacks;
        public AttackParticleData[]? attackParticles;
        public Vector2 colliderSizeMultiplier = Vector2.one;
        public float colliderEdgeRadiusMultiplier = 1f;
        public Vector2 colliderOffset = Vector2.one;
        
        private GameObject? _projectilePrefab;
        private Dictionary<string, KeyValuePair<GameObject, ParticleSystem>> _attackParticlesDict = new();

        public Func<EnemyEntity, Vector3, Action?> GetAttack()
        {
            return FunctionLookupTable[attackFunction];
        }

        public int GetIndex()
        {
            return animationIndex;
        }

        private void PlayAttackParticles(EnemyEntity enemy)
        {
            if (attackParticles == null || attackParticles.Length == 0) return;
            
            var enemyTransform = enemy.transform;

            foreach (var attackParticleData in attackParticles)
            {
                var attackParticlePrefab = attackParticleData.attackParticlePrefab;
                var attackParticleOffset = attackParticleData.attackParticleOffset;
                var alignAttackParticleToHorizontalDirection = attackParticleData.alignAttackParticleToHorizontalDirection;
                
                var key = $"{attackName}.{attackParticlePrefab.name}"; 
                
                if (!_attackParticlesDict.ContainsKey(key))
                {
                    var obj = Object.Instantiate(attackParticlePrefab, enemyTransform)!;
                    _attackParticlesDict.Add(key, new KeyValuePair<GameObject, ParticleSystem>(obj, obj.GetComponent<ParticleSystem>()));
                }
                    
                var offsetX = enemyTransform.right * attackParticleOffset.x;
                var offsetY = enemyTransform.up * attackParticleOffset.y;

                if (enemy.relativeMoveDirection.x < 0)
                {
                    offsetX = -offsetX;
                }
                
                _attackParticlesDict[key].Key.transform.position = enemyTransform.position + offsetX + offsetY;

                if (alignAttackParticleToHorizontalDirection)
                {
                    // access the renderer module of the particle system
                    var pfxRenderer = _attackParticlesDict[key].Key.GetComponent<ParticleSystemRenderer>();
                    var flip = pfxRenderer.flip;
                    flip.x = enemy.relativeMoveDirection.x > 0 ? 1 : 0;
                    pfxRenderer.flip = flip;
                }
                
                _attackParticlesDict[key].Value.Play();
            }
        }

        private void UpdateCollider(EnemyEntity enemy)
        {
            if (colliderSizeMultiplier == Vector2.one &&
                colliderEdgeRadiusMultiplier == 1f &&
                colliderOffset == Vector2.zero)
            {
                return;
            }
            
            if (colliderSizeMultiplier == Vector2.zero &&
                colliderEdgeRadiusMultiplier == 0f &&
                colliderOffset == Vector2.zero)
            {
                return;
            }

            var enemyCollider = (BoxCollider2D)enemy.MainCollider;
            enemyCollider.size = enemy.enemySo.hitboxSize * colliderSizeMultiplier;
            enemyCollider.edgeRadius = enemy.enemySo.hitboxEdgeRadius * colliderEdgeRadiusMultiplier;
            enemyCollider.offset = enemy.enemySo.hitboxOffset + colliderOffset;

            GameUtilities.instance.DelayExecute(() =>
            {
                enemyCollider.size = enemy.enemySo.hitboxSize;
                enemyCollider.edgeRadius = enemy.enemySo.hitboxEdgeRadius;
                enemyCollider.offset = enemy.enemySo.hitboxOffset;
            }, attackTime);
        }

        public Action? Punch(EnemyEntity enemy, Vector3 directionToPlayer)
        {
            if (attackDelay > 0)
            {
                return GameUtilities.instance.DelayExecute(Action, attackDelay);
            }
            else
            {
                Action();
            }

            return null;

            void Action()
            {
                if (!enemy) return;
                
                ShootAction(enemy, directionToPlayer);
                PlayAttackParticles(enemy);
                UpdateCollider(enemy);

                var hit = Physics2D.Raycast(enemy.transform.position, directionToPlayer, attackRange, 1);
                CameraController.CameraShake(cameraShakeTime, cameraShakeStrength);

                if (!hit || !hit.transform.root.TryGetComponent<PlayerController>(out var player)) return;
                if (!player.CanBeDamaged()) return;
                var damageSourcePoint = enemy.transform.position + (Vector3)enemy.enemySo.knockbackSourcePointOffset;
                player.TakeDamage(damage, damageSourcePoint);
                player.Knockback(damageSourcePoint, knockback);
            }
        }
        
        public Action? Dash(EnemyEntity enemy, Vector3 directionToPlayer)
        {
            if (attackDelay > 0)
            {
                return GameUtilities.instance.DelayExecute(Action, attackDelay);
            }
            else
            {
                Action();
            }

            return null;

            void Action()
            {
                if (!enemy) return;
                ShootAction(enemy, directionToPlayer);
                PlayAttackParticles(enemy);
                UpdateCollider(enemy);
                
                var oldKnockback = enemy.currentKnockback;
                enemy.currentKnockback = knockback;
                enemy.AddRelativeForce(enemy.relativeMoveDirection * attackMagnitude, ForceMode2D.Impulse);
                GameUtilities.instance.StartCoroutine(ActionCoroutine());
                CameraController.CameraShake(cameraShakeTime, cameraShakeStrength);
                GameUtilities.instance.DelayExecute(() => enemy.currentKnockback = oldKnockback, attackTime);
            }

            IEnumerator ActionCoroutine()
            {
                var timer = attackTime;
                var rb = enemy.GetComponent<Rigidbody2D>();
                var vel = rb.velocity;

                while (timer > 0)
                {
                    rb.velocity = vel;
                    timer -= Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }
            }
        }

        public Action? Shoot(EnemyEntity enemy, Vector3 directionToPlayer)
        {
            if (attackDelay > 0)
            {
                return GameUtilities.instance.DelayExecute(() =>
                {
                    ShootAction(enemy, directionToPlayer);
                    PlayAttackParticles(enemy);
                    UpdateCollider(enemy);
                }, attackDelay);
            }

            ShootAction(enemy, directionToPlayer);
            PlayAttackParticles(enemy);
            UpdateCollider(enemy);
            return null;
        }
        
        private void ShootAction(EnemyEntity enemy, Vector3 directionToPlayer)
        {
            if (!enemy) return;
            if (projectileAttacks is not { Length: > 0 }) return;
            
            if (_projectilePrefab == null)
            {
                _projectilePrefab = GameUtilities.instance.GetProjectilePrefab();
            }
            
            var rot = enemy.transform.eulerAngles;
            ObjectPooler.CreatePoolIfDoesntExist("Projectile Pool", _projectilePrefab, 10, true);
            CameraController.CameraShake(cameraShakeTime, cameraShakeStrength);
            
            var attackCount = Random.Range(minProjectileAttacks, maxProjectileAttacks);

            for (var i = 0; i < attackCount; i++)
            {
                ProjectileAttackData projectileAttackData;
                
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (useRandomProjectileAttack)
                {
                    projectileAttackData = projectileAttacks[Random.Range(0, projectileAttacks.Length)];
                }
                else
                {
                    projectileAttackData = projectileAttacks[i % projectileAttacks.Length];
                }

                var angleOffset = Random.Range(projectileAttackData.minAimAngleOffset, projectileAttackData.maxAimAngleOffset);
                
                if (projectileAttackData.spawnReferencePoint == ReferencePoints.Player)
                {
                    rot.z = PlayerController.instance.transform.eulerAngles.z + angleOffset;
                }
                else
                {
                    if (enemy.relativeMoveDirection.x < 0)
                    {
                        angleOffset = -angleOffset;
                    }

                    // ReSharper disable once ConvertSwitchStatementToSwitchExpression
                    switch (projectileAttackData.aimReferencePoint)
                    {
                        case ReferencePoints.Self:
                            rot.z = (enemy.relativeMoveDirection.x < 0 ? 180 : 0) + angleOffset;
                            break;
                        case ReferencePoints.Player:
                            rot.z = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg + angleOffset;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(projectileAttackData.aimReferencePoint),
                                "Aim reference point is not implemented.");
                    }
                }
                
                var projectile = ObjectPooler.GetObject("Projectile Pool");

                if (projectile == null)
                {
                    throw new NullReferenceException("Projectile is null from object pooler.");
                }

                var spawnOffsetX = Random.Range(projectileAttackData.minProjectileSpawnOffset.x, projectileAttackData.maxProjectileSpawnOffset.x);
                var spawnOffsetY = Random.Range(projectileAttackData.minProjectileSpawnOffset.y, projectileAttackData.maxProjectileSpawnOffset.y);
                
                if (enemy.relativeMoveDirection.x < 0)
                {
                    spawnOffsetX = -spawnOffsetX;
                }
                
                var tr = enemy.transform;
                var spawnOffset = tr.right * spawnOffsetX + tr.up * spawnOffsetY;
                var spawnPoint = projectileAttackData.spawnReferencePoint switch
                {
                    ReferencePoints.Self => tr.position + spawnOffset,
                    ReferencePoints.Player => tr.position + enemy.GetVectorToPlayer() + spawnOffset,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(projectileAttackData.spawnReferencePoint),
                        "Spawn reference point is not implemented.")
                };

                if (projectileAttackData.tryStartFromGround)
                {
                    var dir = projectileAttackData.spawnReferencePoint == ReferencePoints.Player
                        ? -PlayerController.instance.transform.up
                        : -tr.up;
                    
                    var hit = Physics2D.Raycast(spawnPoint, dir, 10f, GameUtilities.BasicMovementCollisionMask);
                    
                    if (hit)
                    {
                        var groundDir = (hit.point - (Vector2)spawnPoint).normalized;
                        spawnPoint = hit.point - groundDir * (projectileAttackData.projectileData.sprite.bounds.size.y * 0.4f);
                    }
                }

                if (projectileAttackData.tryOrientToGround)
                {
                    var spawnToPlayer = projectileAttackData.spawnReferencePoint == ReferencePoints.Player;
                    
                    var dir = spawnToPlayer
                        ? -PlayerController.instance.transform.up
                        : -tr.up;
                    
                    var hit = Physics2D.Raycast(spawnPoint, dir, 10f, GameUtilities.BasicMovementCollisionMask);

                    if (hit)
                    {
                        rot.z = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90;
                    }
                }
                
                projectile.transform.position = spawnPoint;
                projectile.transform.eulerAngles = rot;
                var entity = projectile.GetComponent<ProjectileEntity>();
                entity.Init(projectileAttacks[i % projectileAttacks.Length].projectileData, enemy.ClosestPlanetGen);
            }
        }

        public Action? Teleport(EnemyEntity enemy, Vector3 directionToPlayer)
        {
            // Save the player position before attack delay to prevent a bullshit tp
            // where the player doesn't have a chance of dodging.
            var playerPos = enemy.transform.position + enemy.GetVectorToPlayer();
            
            if (attackDelay > 0)
            {
                return GameUtilities.instance.DelayExecute(Action, attackDelay);
            }
            else
            {
                Action();
            }

            return null;

            void Action()
            {
                if (!enemy) return;
                ShootAction(enemy, directionToPlayer);
                PlayAttackParticles(enemy);

                enemy.transform.position = playerPos;
            }
        }
    }
}
