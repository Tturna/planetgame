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
        [SerializeField] public Collider2D MainCollider { get; protected set; }
        [CanBeNull] public GameObject ClosestPlanetObject { get; private set; }
        [CanBeNull] public GameObject CurrentPlanetObject { get; private set; }
        [CanBeNull] public PlanetGenerator ClosestPlanetGen { get; private set; }
        [CanBeNull] public PlanetGenerator CurrentPlanetGen { get; private set; }
        public bool IsAlive { get; protected set; } = true;
        public Vector2 DirectionToClosestPlanet { get; private set; }
        public float DistanceToClosestPlanet { get; private set; } = float.MaxValue;
        protected Rigidbody2D Rigidbody { get; set; }
        protected bool CalculatePhysics { get; private set; } = true;
        public bool CanControl { get; private set; } = true;
        protected bool FollowPlanetRotation { get; private set; } = true;
        protected bool CanCollide { get; private set; } = true;
        public bool IsInSpace { get; private set; } = true;

        protected float closestPlanetCheckTimer;
        protected const float ClosestPlanetCheckInterval = 5f;

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
            MainCollider = GetComponent<Collider2D>();
            
            if (TryGetComponent<Rigidbody2D>(out var rb))
            {
                Rigidbody = rb;
                closestPlanetCheckTimer = ClosestPlanetCheckInterval;
                
                // Bump entity so it calculates planet relations on start
                // Rigidbody.AddRelativeForce(Vector2.up * 0.11f, ForceMode2D.Impulse);
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
            // if (!CalculatePhysics && !FollowPlanetRotation) return;

            // This is here to prevent useless calculations when an entity is stationary.
            // This also breaks space flight as the player is stationary in relationship to the ship,
            // so it will never calculate the closest planet while flying.
            // if (Rigidbody.velocity.magnitude > 0.1f)
            {
                // Prevent useless calculations when the entity is on a planet
                if (!CurrentPlanetObject || !CurrentPlanetGen)
                {
                    if (closestPlanetCheckTimer < ClosestPlanetCheckInterval)
                    {
                        closestPlanetCheckTimer += Time.fixedDeltaTime;
                    }
                    else
                    {
                        closestPlanetCheckTimer = 0f;
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

            var posDiff = CalculatePlanetRelation();
            
            if (!CalculatePhysics) return;
            if (!CurrentPlanetObject) return;
            if (!CurrentPlanetGen) return;

            var dirToPlanet = posDiff.normalized;

            var planetGravity = CurrentPlanetGen.GetGravity(DistanceToClosestPlanet);
            var totalGravity = planetGravity * gravityMultiplier;
                    
            Rigidbody.AddForce(dirToPlanet * totalGravity);

            Rigidbody.drag = CurrentPlanetGen.GetDrag(DistanceToClosestPlanet);

            if (FollowPlanetRotation)
            {
                transform.LookAt(transform.position + Vector3.forward, -dirToPlanet);
            }
        }
        
        protected void SetCurrentPlanet([CanBeNull] PlanetGenerator planetGen)
        {
            // ReSharper disable once Unity.NoNullPropagation
            var planetObject = planetGen?.gameObject;
            
            CurrentPlanetGen = planetGen;
            CurrentPlanetObject = planetObject;

            if (planetGen)
            {
                ClosestPlanetGen = planetGen;
                ClosestPlanetObject = planetObject;
            }
            
            IsInSpace = planetGen == null;
        }

        protected Vector2 CalculatePlanetRelation()
        {
            if (!ClosestPlanetObject)
            {
                throw new NullReferenceException("Closest planet object is null.");
            }
            
            Vector2 posDiff = ClosestPlanetObject.transform.position - transform.position;
            DirectionToClosestPlanet = posDiff.normalized;
            DistanceToClosestPlanet = posDiff.magnitude;
            
            if (DistanceToClosestPlanet < ClosestPlanetGen!.atmosphereRadius)
            {
                if (CurrentPlanetObject == ClosestPlanetObject) return posDiff;
                // Debug.Log($"{name} is entering atmosphere of {ClosestPlanetObject.name}");
                SetCurrentPlanet(ClosestPlanetGen);
                TriggerOnPlanetEntered(CurrentPlanetObject);
            }
            else
            {
                if (CurrentPlanetObject != ClosestPlanetObject) return posDiff;
                // Debug.Log($"{name} is exiting atmosphere of {ClosestPlanetObject.name}");
                SetCurrentPlanet(null);
                TriggerOnPlanetExited(ClosestPlanetObject);
            }
            
            return posDiff;
        }

        public void TogglePhysics(bool state) => CalculatePhysics = state;
        public void ToggleControl(bool state) => CanControl = state;
        public void ToggleAutoRotation(bool state) => FollowPlanetRotation = state;
        public virtual void ToggleSpriteRenderer(bool state) => throw new NotImplementedException("ToggleSpriteRenderer() not implemented. Override this method in a derived class.");

        public void ToggleCollision(bool state)
        {
            CanCollide = state;
            MainCollider.enabled = state;
        }

        public void AddRelativeForce(Vector3 force, ForceMode2D forceMode)
        {
            Rigidbody.AddRelativeForce(force, forceMode);
        }
    }
}
