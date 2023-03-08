using System;
using ProcGen;
using UnityEngine;

namespace Entities
{
    public class EntityController : MonoBehaviour
    {
        [SerializeField] private float gravityMultiplier;
        protected Planet CurrentPlanet { get; set; }
        protected PlanetGenerator CurrentPlanetGen { get; set; }
        public Rigidbody2D Rigidbody { get; set; }
        protected bool CalculatePhysics { get; private set; } = true;
        protected bool CanControl { get; private set; } = true;
        protected bool FollowPlanetRotation { get; private set; } = true;

        #region Events
        
        public delegate void EnteredPlanetHandler(Planet enteredPlanet);
        public delegate void ExitedPlanetHandler(Planet exitedPlanet);
        
        public event EnteredPlanetHandler OnEnteredPlanet;
        public event ExitedPlanetHandler OnExitPlanet;

        private void TriggerOnPlanetEntered(Planet enteredPlanet)
        {
            OnEnteredPlanet?.Invoke(enteredPlanet);
        }
        
        private void TriggerOnPlanetExited(Planet exitedPlanet)
        {
            OnExitPlanet?.Invoke(exitedPlanet);
        }
        
        #endregion

        protected virtual void Start()
        {
            if (TryGetComponent<Rigidbody2D>(out var rb))
            {
                Rigidbody = rb;
            }
            else
            {
                Debug.Log("Disabling physics for " + gameObject.name);
                TogglePhysics(false);
                ToggleControl(false);
                ToggleAutoRotation(false);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!Rigidbody) return;

            if (CalculatePhysics)
            {
                if (CurrentPlanet)
                {
                    var trPos = transform.position;
                    var dirToPlanet = (CurrentPlanet.transform.position - trPos).normalized;

                    // Gravity
                    var planetGravity = CurrentPlanet.GetGravity(trPos);
                    var totalGravity = planetGravity * gravityMultiplier;
                    
                    // if (this is PlayerController) Debug.Log(totalGravity);
                    
                    Rigidbody.AddForce(dirToPlanet * totalGravity);

                    // Drag
                    Rigidbody.drag = CurrentPlanet.GetDrag(trPos);

                    // Keep entity oriented in relation to the planet
                    if (FollowPlanetRotation)
                    {
                        transform.LookAt(transform.position + Vector3.forward, -dirToPlanet);
                    }
                }
            }
        }

        public void TogglePhysics(bool state) => CalculatePhysics = state;
        public void ToggleControl(bool state) => CanControl = state;
        public void ToggleAutoRotation(bool state) => FollowPlanetRotation = state;
        public virtual void ToggleSpriteRenderer(bool state) => throw new NotImplementedException();

        protected virtual void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.TryGetComponent<Planet>(out var planet))
            {
                CurrentPlanet = planet;
                CurrentPlanetGen = CurrentPlanet.GetComponent<PlanetGenerator>();

                TriggerOnPlanetEntered(CurrentPlanet);
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.TryGetComponent<Planet>(out var planet))
            {
                CurrentPlanet = null;
                CurrentPlanetGen = null;
                
                TriggerOnPlanetExited(planet);
            }
        }
    }
}
