using System;
using JetBrains.Annotations;
using Planets;
using UnityEngine;
using Utilities;

namespace Entities
{
    public class EntityController : MonoBehaviour
    {
        [SerializeField] protected float gravityMultiplier;
        [SerializeField] protected Collider2D mainCollider;
        [CanBeNull] public GameObject ClosestPlanetObject { get; private set; }
        [CanBeNull] public GameObject CurrentPlanetObject { get; private set; }
        [CanBeNull] protected PlanetGenerator ClosestPlanetGen { get; set; }
        [CanBeNull] protected PlanetGenerator CurrentPlanetGen { get; set; }
        public bool IsAlive { get; protected set; } = true;
        public float DistanceFromClosestPlanet { get; private set; } = float.MaxValue;
        protected Rigidbody2D Rigidbody { get; set; }
        protected bool CalculatePhysics { get; private set; } = true;
        public bool CanControl { get; private set; } = true;
        protected bool FollowPlanetRotation { get; private set; } = true;
        protected bool CanCollide { get; private set; } = true;

        private float _closestPlanetCheckTimer;
        private const float ClosestPlanetCheckInterval = 5f;

        #region Events
        
        public delegate void EnteredPlanetHandler(GameObject enteredPlanetObject);
        public delegate void ExitedPlanetHandler(GameObject exitedPlanetObject);
        
        public event EnteredPlanetHandler OnEnteredPlanet;
        public event ExitedPlanetHandler OnExitPlanet;

        private void TriggerOnPlanetEntered(GameObject enteredPlanet)
        {
            OnEnteredPlanet?.Invoke(enteredPlanet);
        }
        
        private void TriggerOnPlanetExited(GameObject exitedPlanet)
        {
            OnExitPlanet?.Invoke(exitedPlanet);
        }
        
        #endregion

        protected virtual void Start()
        {
            if (TryGetComponent<Rigidbody2D>(out var rb))
            {
                Rigidbody = rb;
                _closestPlanetCheckTimer = ClosestPlanetCheckInterval;
                
                // Bump entity so it calculates planet relations on start
                Rigidbody.AddRelativeForce(Vector2.up * 0.11f, ForceMode2D.Impulse);
            }
            else
            {
                TogglePhysics(false);
                ToggleControl(false);
                ToggleAutoRotation(false);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!Rigidbody) return;

            // This is here to prevent useless calculations when an entity is stationary.
            // This also breaks space flight as the player is stationary in relationship to the ship,
            // so it will never calculate the closest planet while flying.
            // if (Rigidbody.velocity.magnitude > 0.1f)
            {
                // Prevent useless calculations when the entity is on a planet
                if (!CurrentPlanetObject || !CurrentPlanetGen)
                {
                    if (_closestPlanetCheckTimer < ClosestPlanetCheckInterval)
                    {
                        _closestPlanetCheckTimer += Time.fixedDeltaTime;
                    }
                    else
                    {
                        _closestPlanetCheckTimer = 0f;
                        var closestDist = float.MaxValue;
                        
                        foreach (var planet in GameUtilities.GetAllPlanets())
                        {
                            var dist = Vector3.Distance(transform.position, planet.transform.position);

                            if (!(dist < closestDist)) continue;
                            closestDist = dist;
                            ClosestPlanetGen = planet;
                            ClosestPlanetObject = planet.gameObject;
                        }
                    }
                }
            }

            if (!ClosestPlanetGen) return;
            if (!ClosestPlanetObject) return;
            
            var posDiff = CalculatePlanetRelations();
            
            if (!CalculatePhysics) return;
            if (!CurrentPlanetObject) return;
            if (!CurrentPlanetGen) return;

            var dirToPlanet = posDiff.normalized;

            var planetGravity = CurrentPlanetGen.GetGravity(DistanceFromClosestPlanet);
            var totalGravity = planetGravity * gravityMultiplier;
                    
            Rigidbody.AddForce(dirToPlanet * totalGravity);

            Rigidbody.drag = CurrentPlanetGen.GetDrag(DistanceFromClosestPlanet);

            if (FollowPlanetRotation)
            {
                transform.LookAt(transform.position + Vector3.forward, -dirToPlanet);
            }
        }

        private Vector2 CalculatePlanetRelations()
        {
            Vector2 posDiff = ClosestPlanetObject!.transform.position - transform.position;
            DistanceFromClosestPlanet = posDiff.magnitude;
            
            if (DistanceFromClosestPlanet < ClosestPlanetGen!.atmosphereRadius)
            {
                if (CurrentPlanetObject == ClosestPlanetObject) return posDiff;
                // Debug.Log($"{name} is entering atmosphere of {ClosestPlanetObject.name}");
                CurrentPlanetObject = ClosestPlanetObject;
                CurrentPlanetGen = ClosestPlanetGen;
                TriggerOnPlanetEntered(CurrentPlanetObject);
            }
            else
            {
                if (CurrentPlanetObject != ClosestPlanetObject) return posDiff;
                // Debug.Log($"{name} is exiting atmosphere of {ClosestPlanetObject.name}");
                CurrentPlanetObject = null;
                CurrentPlanetGen = null;
                TriggerOnPlanetExited(ClosestPlanetObject);
            }
            
            return posDiff;
        }

        public void TogglePhysics(bool state) => CalculatePhysics = state;
        public void ToggleControl(bool state) => CanControl = state;
        public void ToggleAutoRotation(bool state) => FollowPlanetRotation = state;
        public virtual void ToggleSpriteRenderer(bool state) => throw new NotImplementedException();

        public void ToggleCollision(bool state)
        {
            CanCollide = state;
            mainCollider.enabled = state;
        }

        public void AddRelativeForce(Vector3 force, ForceMode2D forceMode)
        {
            Rigidbody.AddRelativeForce(force, forceMode);
        }
    }
}
