using UnityEngine;

namespace Entities
{
    [RequireComponent(typeof(Interactable))]
    [RequireComponent(typeof(Animator))]
    public class SpaceShipEntity : EntityController
    {
        [SerializeField] private bool horizontalThruster;
        [SerializeField] private Vector2 moveSpeed;
        [SerializeField] private ParticleSystem thrusterParticles;
        [SerializeField] private Vector2 thrusterParticleOffset;
    
        private bool _landingMode = true, _canFly;
        private bool _grounded;
        private EntityController _passenger;
        private Transform _oldPassengerParent;
        private Animator _shipAnimator;

        private Vector2 _inputVector;

        protected override void Start()
        {
            base.Start();
        
            ToggleAutoRotation(false);
            GetComponent<Interactable>().Interacted += Interaction;
            _shipAnimator = GetComponent<Animator>();
            
            thrusterParticles.transform.localPosition = thrusterParticleOffset;

            if (!horizontalThruster)
            {
                thrusterParticles.transform.localRotation = Quaternion.Euler(0,0,90);
            }
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
            
            var hit = Physics2D.Raycast(transform.position, -transform.up, 1.2f, LayerMask.GetMask("Terrain"));
            _grounded = hit.collider;
            
            if (_grounded && _landingMode)
            {
                Rigidbody.velocity = Vector2.zero;
                _canFly = false;
            }
            else
            {
                _canFly = true;
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            if (!_canFly) return;
            if (!_passenger) return;
            if (_inputVector.magnitude < 0.1f) return;
            
            var direction = horizontalThruster ? transform.right : transform.up;

            Rigidbody.AddForce(direction * (_inputVector.y * moveSpeed.y));
            var passengerRotation = _passenger.transform.rotation;
            transform.Rotate(0,0,-_inputVector.x * moveSpeed.x, Space.Self);
            _passenger.transform.rotation = passengerRotation;
        }

        private void Controls()
        {
            _inputVector.x = Input.GetAxis("Horizontal");
            _inputVector.y = Input.GetAxis("Vertical");

            if (!_landingMode && !thrusterParticles.isPlaying && _inputVector.y > 0)
            {
                thrusterParticles.Play();
            }
            else if (thrusterParticles.isPlaying && _inputVector.y <= 0)
            {
                thrusterParticles.Stop();
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                _landingMode = !_landingMode;
                _shipAnimator.SetBool("landed", _landingMode);

                if (!_landingMode)
                {
                    Rigidbody.AddRelativeForce(Vector2.up * 10, ForceMode2D.Impulse);
                }
            }
        }

        private void Interaction(GameObject sourceObject)
        {
            if (!sourceObject.TryGetComponent<EntityController>(out var sourceEntity)) return;
            if (_passenger && sourceEntity != _passenger) return;

            if (!_passenger)
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
