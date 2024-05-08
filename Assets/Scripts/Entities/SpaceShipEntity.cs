using UnityEngine;

namespace Entities
{
    [RequireComponent(typeof(Interactable))]
    public class SpaceShipEntity : EntityController
    {
        [SerializeField] private Vector2 moveSpeed;
    
        private bool _canFly;
        private EntityController _passenger;
        private Transform _oldPassengerParent;

        private Vector2 _inputVector;

        protected override void Start()
        {
            base.Start();
        
            ToggleAutoRotation(false);
            GetComponent<Interactable>().Interacted += Interaction;
        }

        private void Update()
        {
            if (!_passenger) return;

            _passenger.transform.position = transform.position;
        
            if (_passenger is PlayerController)
            {
                Controls();
            }
            else
            {
                // Do something cool if an NPC or something gets into the space ship?
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            if (!_canFly) return;
            if (!_passenger) return;
            if (_inputVector.magnitude < 0.1f) return;

            Rigidbody.AddForce(transform.up * (_inputVector.y * moveSpeed.y));
            var passengerRotation = _passenger.transform.rotation;
            transform.Rotate(0,0,-_inputVector.x * moveSpeed.x, Space.Self);
            _passenger.transform.rotation = passengerRotation;
        }

        private void Controls()
        {
            _inputVector.x = Input.GetAxis("Horizontal");
            _inputVector.y = Input.GetAxis("Vertical");

            if (Input.GetKey(KeyCode.Space))
            {
                _inputVector.y = 1;
            }
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                _inputVector.y = -1;
            }
        }

        private void Interaction(GameObject sourceObject)
        {
            if (!sourceObject.TryGetComponent<EntityController>(out var sourceEntity)) return;
            if (_passenger && sourceEntity != _passenger) return;
            
            _canFly = !_canFly;

            if (_canFly)
            {
                // TogglePhysics(true);
                
                _passenger = sourceEntity;
                _passenger.ToggleControl(false);
                _passenger.TogglePhysics(false);
                _passenger.ToggleSpriteRenderer(false);

                var passengerTransform = _passenger.transform;
                _oldPassengerParent = passengerTransform.parent;
                
                passengerTransform.SetParent(transform);
            }
            else
            {
                // TogglePhysics(false);
                
                _passenger.ToggleControl(true);
                _passenger.TogglePhysics(true);
                _passenger.ToggleSpriteRenderer(true);
                
                _passenger.transform.SetParent(_oldPassengerParent);
                _oldPassengerParent = null;
                
                _passenger = null;
            }
        }
    }
}
