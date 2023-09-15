using System;
using System.Collections.Generic;
using UnityEngine;

namespace Entities.Entities.Enemies
{
    [Serializable]
    public class MovementPattern
    {
        public enum MovementTypes
        {
            MeleeWalker,
            RangedWalker,
            MeleeJumper,
            RangedJumper,
            MeleeFlyer,
            RangedFlyer
        }
        
        public struct MovementFunctionData
        {
            public EnemySo enemySo;
            public Rigidbody2D rb;
            public Animator anim;
            public Transform playerTr;
            public float distanceToPlayer;
            public float dotToPlayer;
            public Vector3 relativeMoveDirection;
        }
        
        public MovementTypes movementType;
        
        public Dictionary<MovementTypes, Action<MovementFunctionData>> MovementLookupTable => new()
        {
            { MovementTypes.MeleeWalker, MeleeWalker },
            { MovementTypes.RangedWalker, RangedWalker },
            { MovementTypes.MeleeJumper, MeleeJumper },
            { MovementTypes.RangedJumper, RangedJumper },
            { MovementTypes.MeleeFlyer, MeleeFlyer },
            { MovementTypes.RangedFlyer, RangedFlyer }
        };
        
        private int _terrainLayerMask = -1;
        private int _terrainBitsLayerMask = -1;
        private bool _jumping;
        private float _jumpCooldown;

        public Action<MovementFunctionData> GetMovement()
        {
            return MovementLookupTable[movementType];
        }

        public void Init()
        {
            _lastJumpTime = 0f;
            _jumpCooldown = 0f;
            _terrainLayerMask = LayerMask.GetMask("Terrain");
            _terrainBitsLayerMask = LayerMask.GetMask("TerrainBits");
        }

        private Vector3 GetWalkForceOnTerrain(Transform entityTr, Vector3 relativeMoveDirection, float accelerationSpeed, float maxSpeed, float maxSlopeMultiplier)
        {
            const float rayBelowLength = .35f;
            const float raySideLength = .4f;
            
            // Figure out the slope angle of the terrain
            var rayStartPoint = entityTr.position - entityTr.up * 0.25f;

            // Raycast "below". It's actually a bit to the side as well
            var rayBelowDirection = new Vector2(.1f * relativeMoveDirection.x, -.4f).normalized;
            var hitBelow = Physics2D.Raycast(rayStartPoint, rayBelowDirection, rayBelowLength, _terrainLayerMask);
            
            // Raycast a bit to the side, depending on movement direction
            var raySideDirection = new Vector2(.1f * relativeMoveDirection.x, -.2f).normalized;
            var hitSide = Physics2D.Raycast(rayStartPoint, raySideDirection, raySideLength, _terrainLayerMask);
            
            var force = relativeMoveDirection.x * entityTr.right * accelerationSpeed;
            
            if (hitBelow && hitSide)
            {
                // Move direction is the vector from the bottom raycast to the side raycast
                var direction = (hitSide.point - hitBelow.point).normalized;
                
                // Check if the direction is upwards relative to the entity
                var dot = Vector3.Dot(entityTr.up, direction);

                if (dot > 0)
                {
                    force = direction * (accelerationSpeed * Mathf.Clamp(1f + dot * 4f, 1f, maxSlopeMultiplier));
                }
            }
            
            return force;
        }

        private void HandleGroundCheck(Transform entityTr, Animator anim)
        {
            var mask = _terrainLayerMask | _terrainBitsLayerMask;
            var hit = Physics2D.CircleCast(entityTr.position, 0.2f, -entityTr.up, 0.4f, mask);

            if (!hit || _jumpCooldown > 0) return;
            _jumping = false;
            anim.SetBool("jumping", false);
        }

        private void HandleWalkerJump(Transform entityTr, Rigidbody2D rb, Animator entityAnim, Vector3 relativeMoveDirection, float jumpForce)
        {
            if (_jumpCooldown > 0)
            {
                _jumpCooldown -= Time.deltaTime;
                return;
            }
            
            if (_jumping) return;
            var hit = Physics2D.Raycast(entityTr.position, relativeMoveDirection, .5f, _terrainLayerMask);
            if (!hit) return;
            
            rb.AddForce(entityTr.up * jumpForce, ForceMode2D.Impulse);
            _jumping = true;
            _jumpCooldown = 0.5f;
            entityAnim.SetBool("jumping", true);
        }

        // Movement functions
        private void MeleeWalker(MovementFunctionData data)
        {
            HandleGroundCheck(data.rb.transform, data.anim);
            HandleWalkerJump(data.rb.transform, data.rb, data.anim, data.relativeMoveDirection, data.enemySo.jumpForce);
            
            var force = GetWalkForceOnTerrain(data.rb.transform, data.relativeMoveDirection, data.enemySo.accelerationSpeed, data.enemySo.maxSpeed, data.enemySo.maxSlopeMultiplier);
            var localVelocity = data.rb.GetVector(data.rb.velocity);
            
            if ((data.relativeMoveDirection.x > 0 && localVelocity.x < data.enemySo.maxSpeed) ||
                (data.relativeMoveDirection.x < 0 && localVelocity.x > -data.enemySo.maxSpeed))
            {
                data.rb.AddForce(force);
            }
        }

        private void RangedWalker(MovementFunctionData data)
        {
            throw new NotImplementedException();
        }
        
        private void MeleeJumper(MovementFunctionData data)
        {
            throw new NotImplementedException();
        }

        private float _lastJumpTime;
        private void RangedJumper(MovementFunctionData data)
        {
            HandleGroundCheck(data.rb.transform, data.anim);

            _jumpCooldown -= Time.deltaTime;
            if (_jumping || _jumpCooldown > 0) return;
            var timeSinceLastJump = Time.time - _lastJumpTime;

            if (timeSinceLastJump > 1.8f)
            {
                data.anim.SetBool("jumping", true);
            }
            
            if (!(timeSinceLastJump > 2f)) return;
            _jumping = true;
            _lastJumpTime = Time.time;
            _jumpCooldown = 0.15f;
            data.rb.AddForce((data.rb.transform.up + data.relativeMoveDirection).normalized * data.enemySo.jumpForce, ForceMode2D.Impulse);
        }
        
        private void MeleeFlyer(MovementFunctionData data)
        {
            throw new NotImplementedException();
        }
        
        private void RangedFlyer(MovementFunctionData data)
        {
            throw new NotImplementedException();
        }
    }
}
