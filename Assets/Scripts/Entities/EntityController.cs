using System;
using UnityEngine;

namespace Entities
{
    public class EntityController : MonoBehaviour
    {
        [SerializeField] private float gravityMultiplier;
        protected Planet CurrentPlanet { get; set; }

        public Rigidbody2D Rigidbody { get; set; }

        protected bool CalculatePhysics { get; private set; } = true;
        protected bool CanControl { get; private set; } = true;
        protected bool FollowPlanetRotation { get; private set; } = true;

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
                    var dirToPlanet = (CurrentPlanet.transform.position - transform.position).normalized;

                    // Gravity
                    var planetGravity = CurrentPlanet.GetGravity(transform.position);
                    var totalGravity = planetGravity * gravityMultiplier;

                    Rigidbody.AddForce(dirToPlanet * totalGravity);

                    // Drag
                    Rigidbody.drag = CurrentPlanet.GetDrag(transform.position);

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

                if (this is PlayerController)
                {
                    Camera.main!.GetComponent<CameraController>().SetTargetPlanet(CurrentPlanet);
                }
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if (other.gameObject.TryGetComponent<Planet>(out _))
            {
                CurrentPlanet = null;
            }
        }
    }
}
