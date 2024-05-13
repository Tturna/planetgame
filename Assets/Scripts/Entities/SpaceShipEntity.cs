using Cameras;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;

namespace Entities
{
    [RequireComponent(typeof(Interactable))]
    [RequireComponent(typeof(Animator))]
    public class SpaceShipEntity : EntityController
    {
        [FormerlySerializedAs("normalSpeed")] [SerializeField] private float normalMaxSpeed;
        [FormerlySerializedAs("cruiseSpeed")] [SerializeField] private float cruiseMaxSpeed;
        [FormerlySerializedAs("warpSpeed")] [SerializeField] private float warpMaxSpeed;
        [SerializeField] private float accelerationSpeed;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private bool horizontalThruster;
        [SerializeField] private float boostInterval;
        [SerializeField] private float boostPower;
        [SerializeField] private ParticleSystem thrusterParticles1;
        [SerializeField] private ParticleSystem thrusterParticles2;
        [SerializeField] private ParticleSystem takeOffParticles;
        [SerializeField] private ParticleSystem boostParticles;
        [SerializeField] private ParticleSystem landingParticles;
        [SerializeField] private ParticleSystem movementParticles;
        [SerializeField] private GameObject movementParticleAnchor;
        [SerializeField] private Vector2 thrusterParticleOffset;
        [SerializeField] private Transform starMapMarker;
    
        private bool _landingMode = true, _canFly;
        private bool _grounded;
        private float _boostTimer;
        private EntityController _passenger;
        private Transform _oldPassengerParent;
        private Animator _shipAnimator;
        private SpriteRenderer _spriteRenderer;
        private Interactable _interactable;
        private bool _flipped;
        private float _maxSpeed;
        private int _speedLevel;
        private float _initialLandingDistance;

        private Vector2 _inputVector;
        private float _smoothRotationInput;

        protected override void Start()
        {
            base.Start();
        
            ToggleAutoRotation(false);
            _interactable = GetComponent<Interactable>();
            _interactable.InteractedImmediate += InteractionImmediate;
            _interactable.InteractedHold += InteractionHold;
            _shipAnimator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            thrusterParticles1.transform.localPosition = thrusterParticleOffset;
            thrusterParticles2.transform.localPosition = thrusterParticleOffset;

            if (!horizontalThruster)
            {
                thrusterParticles1.transform.localRotation = Quaternion.Euler(0,0,90);
                thrusterParticles2.transform.localRotation = Quaternion.Euler(0,0,90);
            }
            else
            {
                starMapMarker.localRotation = Quaternion.Euler(0,0,-90);
            }
        }

        private void Update()
        {
            _interactable.IndicatorParent.rotation = Camera.main!.transform.rotation;
            
            if (!_passenger) return;

            if (_passenger is PlayerController)
            {
                Controls();
            }
            else
            {
                // Do something cool if an NPC or something gets into the space ship?
            }
            
            var castDir = _flipped ? transform.up : -transform.up;
            var hit = Physics2D.Raycast(transform.position, castDir, 1.25f, LayerMask.GetMask("Terrain"));
            _grounded = hit.collider;
            
            if (_grounded && _landingMode)
            {
                Rigidbody.velocity = Vector2.Lerp(Rigidbody.velocity, Vector2.zero, Time.deltaTime);
                _initialLandingDistance = 0;
                _canFly = false;
                landingParticles.Stop();
            }
            else
            {
                _canFly = true;
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            if (!_passenger) return;
            _passenger.transform.position = transform.position;
            
            if (_landingMode && !_grounded)
            {
                var terrainLayer = LayerMask.GetMask("Terrain");
                var dirToPlanet = (ClosestPlanetObject!.transform.position - transform.position).normalized;
                var hit = Physics2D.Raycast(transform.position, dirToPlanet, 100f, terrainLayer);

                if (!hit)
                {
                    Debug.LogError("No hit found for raycast to planet");
                    return;
                }

                var leftHit = Physics2D.Raycast(transform.position, dirToPlanet - transform.right * 0.1f, 100f, terrainLayer);
                var rightHit = Physics2D.Raycast(transform.position, dirToPlanet + transform.right * 0.1f, 100f, terrainLayer);
                
                var leftRightDiff = rightHit.point - leftHit.point;
                var terrainAngle = Mathf.Atan2(leftRightDiff.y, leftRightDiff.x) * Mathf.Rad2Deg;
                var hitDiff = hit.point - (Vector2)transform.position;
                var distToGround = hitDiff.magnitude;
                
                if (_initialLandingDistance == 0)
                {
                    _initialLandingDistance = distToGround;
                }
                
                var normalDistToGround = 1f - distToGround / _initialLandingDistance;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0,0,terrainAngle), normalDistToGround * 0.25f);
                landingParticles.transform.rotation = Camera.main!.transform.rotation;

                if (distToGround < 8f && !landingParticles.isEmitting)
                {
                    landingParticles.Play();
                }
                
                if (Rigidbody.velocity.magnitude > hitDiff.magnitude)
                {
                    Rigidbody.velocity = Vector2.Lerp(Rigidbody.velocity, Rigidbody.velocity.normalized * 0.2f, Time.fixedDeltaTime * 2f);
                    return;
                }
                
                Rigidbody.AddForce(hitDiff);
                return;
            }
            
            if (!_canFly) return;
            
            _smoothRotationInput = Mathf.Lerp(_smoothRotationInput, -_inputVector.x, Time.fixedDeltaTime * 4.5f);
            
            var passengerRotation = _passenger.transform.rotation;
            var velocityV = Mathf.Clamp01(Rigidbody.velocity.magnitude / warpMaxSpeed);
            // TODO: Consider making shared method for these easing functions.
            // return x < 0.5 ? 16 * x * x * x * x * x : 1 - Math.pow(-2 * x + 2, 5) / 2;
            var rotationLimit = velocityV < 0.5f
                ? 16 * velocityV * velocityV * velocityV * velocityV * velocityV
                : 1 - Mathf.Pow(-2 * velocityV + 2, 5) / 2;
            transform.Rotate(0,0, _smoothRotationInput * rotationSpeed / (1 + rotationLimit * 10f), Space.Self);
            _passenger.transform.rotation = passengerRotation;

            HandleMovementParticles();

            if (_inputVector.magnitude < 0.1f)
            {
                Rigidbody.velocity = Vector2.Lerp(Rigidbody.velocity, Vector2.zero, Time.fixedDeltaTime * .5f);
                return;
            }

            if (_inputVector.y < 0)
            {
                var multiplier = 1f + Mathf.Abs(_inputVector.y) * 4;
                Rigidbody.velocity = Vector2.Lerp(Rigidbody.velocity, Vector2.zero, Time.fixedDeltaTime * multiplier);
                _speedLevel = 0;
                return;
            }
            
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (_speedLevel == 1 && Rigidbody.velocity.magnitude < normalMaxSpeed)
            {
                _speedLevel = 0;
            }
            else if (_speedLevel == 2 && Rigidbody.velocity.magnitude < cruiseMaxSpeed)
            {
                _speedLevel = 1;
            }

            var direction = horizontalThruster ? transform.right : transform.up;
            var maxSpeed = _speedLevel switch
            {
                0 => normalMaxSpeed,
                1 => cruiseMaxSpeed,
                2 => warpMaxSpeed,
                _ => normalMaxSpeed
            };

            if (Rigidbody.velocity.magnitude < maxSpeed)
            {
                Rigidbody.AddForce(direction * (_inputVector.y * accelerationSpeed));
            }
        }

        private void Controls()
        {
            _inputVector.x = Input.GetAxis("Horizontal");
            _inputVector.y = Input.GetAxis("Vertical");

            if (!_landingMode && !thrusterParticles1.isEmitting && _inputVector.y > 0)
            {
                thrusterParticles1.Play();
                thrusterParticles2.Play();
            }
            else if (thrusterParticles1.isPlaying && _inputVector.y <= 0)
            {
                thrusterParticles1.Stop();
                thrusterParticles2.Stop();
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                _landingMode = !_landingMode;
                _shipAnimator.SetBool("landed", _landingMode);
                // TogglePhysics(_landingMode);
                // Toggle landing lights?

                if (_landingMode)
                {
                    movementParticles.Stop();
                    CameraController.SetZoomMultiplierSmooth(1f, _grounded ? 1f : 3f);
                }
                else
                {
                    var dir = _flipped ? Vector2.down : Vector2.up;
                    Rigidbody.AddRelativeForce(dir * 8, ForceMode2D.Impulse);
                    movementParticles.Play();
                    CameraController.SetZoomMultiplierSmooth(1.8f, 1f);

                    if (_grounded)
                    {
                        takeOffParticles.Play();
                    }
                }
            }

            if (_boostTimer > 0)
            {
                _boostTimer -= Time.deltaTime;
            }
            else if (!_landingMode && _speedLevel < 2 && Input.GetKeyDown(KeyCode.Space))
            {
                _speedLevel++;
                
                var dir = horizontalThruster ? transform.right : transform.up;
                Rigidbody.velocity = Vector2.zero;
                Rigidbody.AddForce(dir * boostPower, ForceMode2D.Impulse);
                _boostTimer = boostInterval;
                boostParticles.Play();
                CameraController.CameraShake(0.3f, 0.5f);
                
                var zoomMultiplier = CameraController.zoomMultiplier;
                CameraController.SetZoomMultiplierSmooth(zoomMultiplier * 1.5f, 0.075f);
                GameUtilities.instance.DelayExecute(() =>
                {
                    CameraController.SetZoomMultiplierSmooth(zoomMultiplier, 1.25f);
                }, 0.3f);
            }

            if (horizontalThruster)
            {
                var camRight = CameraController.instance.transform.right;
                var shipRight = transform.right;
                var dot = Vector3.Dot(camRight, shipRight);
                _flipped = dot < 0;
                _spriteRenderer.flipY = _flipped;
                
            }
        }

        private void HandleMovementParticles()
        {
            var velocity = Rigidbody.velocity;
            var velocityMagnitude = velocity.magnitude;
            var deciVelMag = velocityMagnitude * 0.1f;
            var velocityAngle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            
            var atmosphereRadius = ClosestPlanetGen!.atmosphereRadius;
            var distFromAtmosphere = DistanceFromClosestPlanet - atmosphereRadius;
            var distV = Mathf.Clamp01(distFromAtmosphere / (atmosphereRadius * 0.5f) * (deciVelMag + 0.25f));
            
            var velocityV = Mathf.Clamp01(velocityMagnitude / warpMaxSpeed);
            // return 1 - Math.cos((x * Math.PI) / 2);
            var velInOutSine = 1 - Mathf.Cos(velocityV * distV * Mathf.PI / 2);
            // return x < 0.5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2;
            var velInOutCubic = velocityV < 0.5f
                ? 4 * velocityV * velocityV * velocityV
                : 1 - Mathf.Pow(-2 * velocityV + 2, 3) / 2;
            // return x * x * x * x
            var velInQuart = velocityV * velocityV * velocityV * velocityV;

            var emissionModule = movementParticles.emission;
            var sizeOverLifetimeModule = movementParticles.sizeOverLifetime;
            var velocityOverlifetimeModule = movementParticles.velocityOverLifetime;
            var colorOverLifetimeModule = movementParticles.colorOverLifetime;

            emissionModule.rateOverTime = velocityMagnitude * 0.6f;
            
            velocityOverlifetimeModule.y = velInOutCubic * 20f;

            var alpha = velInOutSine * 0.5f;
            var gradient = new Gradient();
            var colorKeys = gradient.colorKeys;
            var alphaKeys = new []
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(alpha, 0.15f),
                new GradientAlphaKey(0f, 1f)
            };
            gradient.SetKeys(colorKeys, alphaKeys);
            var minMaxGradient = new ParticleSystem.MinMaxGradient(gradient);
            colorOverLifetimeModule.color = minMaxGradient;
            
            sizeOverLifetimeModule.y = new ParticleSystem.MinMaxCurve(1 + velInQuart * 20f, 1 + velInQuart * 120f);
            
            movementParticleAnchor.transform.localPosition = velocity.normalized * 10f;
            movementParticleAnchor.transform.rotation = Quaternion.Euler(0,0,velocityAngle - 90);
        }

        private void InteractionImmediate(GameObject sourceObject)
        {
            if (_passenger && !_landingMode)
            {
                _interactable.canHoldInteract = true;
                return;
            }
            _interactable.canHoldInteract = false;
            TogglePassenger(sourceObject);
        }
        
        private void InteractionHold(GameObject sourceObject)
        {
            if (_landingMode) return;
            TogglePassenger(sourceObject);
        }

        private void TogglePassenger(GameObject sourceObject)
        {
            if (!sourceObject.TryGetComponent<EntityController>(out var sourceEntity)) return;
            if (_passenger && sourceEntity != _passenger) return;

            if (!_passenger)
            {
                TogglePhysics(false);
                
                _passenger = sourceEntity;
                _passenger.ToggleControl(false);
                _passenger.TogglePhysics(false);
                // _passenger.ToggleCollision(false);
                _passenger.ToggleSpriteRenderer(false);

                var passengerTransform = _passenger.transform;
                _oldPassengerParent = passengerTransform.parent;
                
                passengerTransform.SetParent(transform);
            }
            else
            {
                if (_landingMode)
                {
                    TogglePhysics(true);
                }
                
                _passenger.ToggleControl(true);
                _passenger.TogglePhysics(true);
                // _passenger.ToggleCollision(true);
                _passenger.ToggleSpriteRenderer(true);
                
                _passenger.transform.SetParent(_oldPassengerParent);
                _oldPassengerParent = null;
                
                _passenger = null;
            }
        }
    }
}
