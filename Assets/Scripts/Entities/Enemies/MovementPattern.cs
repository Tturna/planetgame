using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Utilities;

namespace Entities.Enemies
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
            [CanBeNull] public Transform playerTr;
            public float? distanceToPlayer;
            public float? dotToPlayer;
            public Vector3 relativeMoveDirection;
        }
        
        // Set in inspector
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
        
        private int _collisionLayerMask = -1;
        private bool _jumping;
        private float _jumpCooldown;
        private float _stuckTimer;

        public Action<MovementFunctionData> GetMovement()
        {
            return MovementLookupTable[movementType];
        }

        public void Init()
        {
            _lastJumpTime = 0f;
            _jumpCooldown = 0f;
            _collisionLayerMask = GameUtilities.BasicMovementCollisionMask;
        }

        private Vector3 GetWalkForceOnTerrain(MovementFunctionData data)
        {
            var entityTr = data.rb.transform;
            var relativeMoveDirection = data.relativeMoveDirection;
            var accelerationSpeed = data.enemySo.accelerationSpeed;
            var maxSlopeMultiplier = data.enemySo.maxSlopeMultiplier;
            var hbVertEdgeDist = data.enemySo.hitboxSize.y / 2f + data.enemySo.hitboxEdgeRadius;
            var rayBelowLength = hbVertEdgeDist * 1.1f;
            var raySideLength = hbVertEdgeDist * 1.25f;
            
            // Figure out the slope angle of the terrain
            var rayStartPoint = entityTr.position - entityTr.up * hbVertEdgeDist / 2f;

            // Raycast "below". It's actually a bit to the side as well
            var rayBelowDirection = new Vector2(.1f * relativeMoveDirection.x, -.4f).normalized;
            var hitBelow = Physics2D.Raycast(rayStartPoint, rayBelowDirection, rayBelowLength, _collisionLayerMask);
            
            // Raycast a bit to the side, depending on movement direction
            var raySideDirection = new Vector2(.1f * relativeMoveDirection.x, -.15f).normalized;
            var hitSide = Physics2D.Raycast(rayStartPoint, raySideDirection, raySideLength, _collisionLayerMask);
            
            // Debug.DrawLine(rayStartPoint, rayStartPoint + (Vector3)rayBelowDirection * rayBelowLength, Color.red);
            // Debug.DrawLine(rayStartPoint, rayStartPoint + (Vector3)raySideDirection * raySideLength, Color.blue);
            
            var force = relativeMoveDirection.x * entityTr.right * accelerationSpeed;
            
            if (hitBelow && hitSide)
            {
                // Move direction is the vector from the bottom raycast to the side raycast
                var direction = (Vector3)(hitSide.point - hitBelow.point).normalized;
                // Debug.DrawLine(entityTr.position, entityTr.position + direction * 3f, Color.green);
                
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
            var hit = Physics2D.CircleCast(entityTr.position, 0.2f, -entityTr.up, 0.4f, _collisionLayerMask);

            if (!hit || _jumpCooldown > 0) return;
            _jumping = false;
            anim.SetBool("jumping", false);
        }

        private void HandleWalkerJump(MovementFunctionData data)
        {
            if (!data.enemySo.canJump) return;
            
            var rb = data.rb;
            var entityTr = rb.transform;
            var entityAnim = data.anim;
            var relativeMoveDirection = data.relativeMoveDirection;
            var jumpForce = data.enemySo.jumpForce;
            
            if (_jumpCooldown > 0)
            {
                _jumpCooldown -= Time.deltaTime;
                return;
            }
            
            if (_jumping) return;

            if (relativeMoveDirection.magnitude > 0.1f && Mathf.Abs(rb.GetVector(rb.velocity).x) < 0.1f)
            {
                _stuckTimer -= Time.deltaTime;
            }
            else
            {
                _stuckTimer = 0.5f;
            }
            
            var horizontalHit = Physics2D.Raycast(entityTr.position, relativeMoveDirection, 1f, _collisionLayerMask);
            if (!horizontalHit && _stuckTimer > 0) return;
            
            rb.AddForce(entityTr.up * jumpForce, ForceMode2D.Impulse);
            _jumping = true;
            _jumpCooldown = 0.5f;
            entityAnim.SetBool("jumping", true);
        }

        // Movement functions
        private void MeleeWalker(MovementFunctionData data)
        {
            HandleGroundCheck(data.rb.transform, data.anim);
            HandleWalkerJump(data);
            
            var force = GetWalkForceOnTerrain(data);
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
            var rb = data.rb;
            var tr = rb.transform;
            
            if (data.enemySo.faceMovementDirection && rb.velocity.magnitude > 0.1f)
            {
                tr.up = rb.velocity.normalized;
            }
            
            if (!data.playerTr) return;
            
            var playerDirection = data.playerTr.position - tr.position;
            var direction = playerDirection.normalized;

            if (rb.velocity.magnitude > 0.1f)
            {
                direction = Vector3.Lerp(-rb.velocity.normalized, direction, 0.75f);
            }

            rb.AddForce(direction * data.enemySo.accelerationSpeed);
            
            if (rb.velocity.magnitude > data.enemySo.maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * data.enemySo.maxSpeed;
            }
        }
        
        private void RangedFlyer(MovementFunctionData data)
        {
            throw new NotImplementedException();
        }
    }
}
