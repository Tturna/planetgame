using System;
using Planets;
using UnityEngine;

namespace Entities.Entities
{
    public class EntityController : MonoBehaviour
    {
        [SerializeField] private float gravityMultiplier;
        public GameObject CurrentPlanetObject { get; protected set; }
        protected PlanetGenerator CurrentPlanetGen { get; set; }
        protected Rigidbody2D Rigidbody { get; set; }
        protected bool CalculatePhysics { get; private set; } = true;
        protected bool CanControl { get; private set; } = true;
        protected bool FollowPlanetRotation { get; private set; } = true;

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
            if (!CalculatePhysics) return;
            if (!CurrentPlanetObject) return;
                
            var trPos = transform.position;
            var dirToPlanet = (CurrentPlanetObject.transform.position - trPos).normalized;

            var planetGravity = CurrentPlanetGen.GetGravity(trPos);
            var totalGravity = planetGravity * gravityMultiplier;
                    
            Rigidbody.AddForce(dirToPlanet * totalGravity);

            Rigidbody.drag = CurrentPlanetGen.GetDrag(trPos);

            // Keep entity oriented in relation to the planet
            if (FollowPlanetRotation)
            {
                transform.LookAt(transform.position + Vector3.forward, -dirToPlanet);
            }
        }

        public void TogglePhysics(bool state) => CalculatePhysics = state;
        public void ToggleControl(bool state) => CanControl = state;
        public void ToggleAutoRotation(bool state) => FollowPlanetRotation = state;
        public virtual void ToggleSpriteRenderer(bool state) => throw new NotImplementedException();

        public void AddRelativeForce(Vector3 force, ForceMode2D forceMode)
        {
            Rigidbody.AddRelativeForce(force, forceMode);
        }
        
        protected virtual void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.CompareTag("Planet") && col.gameObject.layer == LayerMask.NameToLayer("Planet"))
            {
                CurrentPlanetObject = col.gameObject;
                CurrentPlanetGen = col.GetComponent<PlanetGenerator>();

                TriggerOnPlanetEntered(CurrentPlanetObject);
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Planet") && other.gameObject.layer == LayerMask.NameToLayer("Planet"))
            {
                CurrentPlanetObject = null;
                CurrentPlanetGen = null;
                
                TriggerOnPlanetExited(other.gameObject);
            }
        }
    }
}
