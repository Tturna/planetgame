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
        [Header("Settings")]
        [FormerlySerializedAs("normalSpeed")] [SerializeField] private float normalMaxSpeed;
        [FormerlySerializedAs("cruiseSpeed")] [SerializeField] private float cruiseMaxSpeed;
        [FormerlySerializedAs("warpSpeed")] [SerializeField] private float warpMaxSpeed;
        [SerializeField] private float accelerationSpeed;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private bool horizontalThruster;
        [SerializeField] private Vector2 thrusterParticleOffset;
        [SerializeField] private float boostInterval;
        [SerializeField] private float boostPower;
        [SerializeField] private float hullHealth;
        [SerializeField] private float maxHullHealth;
        [SerializeField] private float fuelLevel;
        [SerializeField] private float maxFuelLevel;
        
        [Header("References/Components")]
        [SerializeField] private ParticleSystem thrusterParticles1;
        [SerializeField] private ParticleSystem thrusterParticles2;
        [SerializeField] private ParticleSystem takeOffParticles;
        [SerializeField] private ParticleSystem boostParticles;
        [SerializeField] private ParticleSystem landingParticles;
        [SerializeField] private ParticleSystem lowHealthParticles;
        [SerializeField] private ParticleSystem movementParticles;
        [SerializeField] private GameObject movementParticleAnchor;
        [SerializeField] private ParticleSystem collisionParticles;
        [SerializeField] private Transform starMapMarker;
    
        private bool _landingMode = true, _canFly;
        private bool _grounded;
        private float _boostTimer;
        private bool _boostReady = true;
        private EntityController _passenger;
        private Transform _oldPassengerParent;
        private Animator _shipAnimator;
        private SpriteRenderer _spriteRenderer;
        private Interactable _interactable;
        private bool _flipped;
        private float _maxSpeed;
        private int _speedLevel = -1;
        private float _initialLandingDistance;
        private int _collisionLayerMask;
        private float _invincibilityTimer;

        private Vector2 _inputVector;
        private float _smoothRotationInput;

        protected override void Start()
        {
            base.Start();

            _collisionLayerMask = GameUtilities.BasicMovementCollisionMask;
            ToggleAutoRotation(false);
            _interactable = GetComponent<Interactable>();
            _interactable.OnInteractImmediate += InteractionImmediate;
            _interactable.OnInteractHold += InteractionHold;
            _shipAnimator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            _invincibilityTimer = 2f;
            
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
            _interactable.IndicatorParent.rotation = CameraController.instance.mainCam.transform.rotation;
            _invincibilityTimer -= Time.deltaTime;
            
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
            var hitCollider = Physics2D.OverlapCircle(transform.position + castDir * 0.5f, 0.7f, _collisionLayerMask);
            _grounded = hitCollider;
            
            if (_canFly && _grounded && _landingMode)
            {
                Touchdown();
            }
            
            StatsUIManager.instance.UpdateShipLocationUI(transform.position);
            StatsUIManager.instance.UpdateShipAngleUI(transform.eulerAngles.z);
            StatsUIManager.instance.UpdateShipVelocityUI(Rigidbody.velocity.magnitude);

            if (fuelLevel > 0)
            {
                fuelLevel -= _inputVector.normalized.magnitude * Time.deltaTime;
                StatsUIManager.instance.UpdateShipFuelUI(fuelLevel, maxFuelLevel);

                if (fuelLevel <= 0)
                {
                    StatsUIManager.instance.UpdateShipFuelUI(0, maxFuelLevel);
                    _speedLevel = 0;
                    StatsUIManager.instance.UpdateShipGearUI(_speedLevel);
                }
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            lowHealthParticles.transform.localRotation = Quaternion.Euler(0, 0, _flipped ? 180 : 0);
            
            if (!_passenger) return;
            _passenger.transform.position = transform.position;
            var passengerRotation = _passenger.transform.rotation;
            
            if (_landingMode && !_grounded)
            {
                var dirToPlanet = (ClosestPlanetObject!.transform.position - transform.position).normalized;
                var hit = Physics2D.Raycast(transform.position, dirToPlanet, 100f, _collisionLayerMask);

                if (!hit)
                {
                    Debug.LogError("No hit found for raycast to planet");
                    return;
                }

                var leftHit = Physics2D.Raycast(transform.position, dirToPlanet - transform.right * 0.1f, 100f, _collisionLayerMask);
                var rightHit = Physics2D.Raycast(transform.position, dirToPlanet + transform.right * 0.1f, 100f, _collisionLayerMask);
                
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
                landingParticles.transform.rotation = CameraController.instance.mainCam.transform.rotation;
                _passenger.transform.rotation = passengerRotation;

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
            // if (fuelLevel <= 0) return;
            
            _smoothRotationInput = Mathf.Lerp(_smoothRotationInput, -_inputVector.x, Time.fixedDeltaTime * 4.5f);
            
            var velocityV = Mathf.Clamp01(Rigidbody.velocity.magnitude / warpMaxSpeed);
            // TODO: Consider making shared method for these easing functions.
            // return x < 0.5 ? 16 * x * x * x * x * x : 1 - Math.pow(-2 * x + 2, 5) / 2;
            var rotationLimit = velocityV < 0.5f
                ? 16 * velocityV * velocityV * velocityV * velocityV * velocityV
                : 1 - Mathf.Pow(-2 * velocityV + 2, 5) / 2;
            transform.Rotate(0,0, _smoothRotationInput * rotationSpeed / (1 + rotationLimit * 10f), Space.Self);
            _passenger.transform.rotation = passengerRotation;

            HandleMovementParticles();
            HandleCollisionWarning();

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
                StatsUIManager.instance.UpdateShipGearUI(_speedLevel);
                return;
            }

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (_speedLevel == 1 && Rigidbody.velocity.magnitude < normalMaxSpeed)
            {
                _speedLevel = 0;
                StatsUIManager.instance.UpdateShipGearUI(_speedLevel);
            }
            else if (_speedLevel == 2 && Rigidbody.velocity.magnitude < cruiseMaxSpeed)
            {
                _speedLevel = 1;
                StatsUIManager.instance.UpdateShipGearUI(_speedLevel);
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
                ToggleLandingMode();
            }

            if (_boostTimer > 0)
            {
                _boostTimer -= Time.deltaTime;
            }
            else
            {
                if (!_boostReady)
                {
                    StatsUIManager.instance.UpdateShipBoostStatusUI(true);
                    _boostReady = true;
                }

                if (!_landingMode && _speedLevel < 2 && Input.GetKeyDown(KeyCode.Space))
                {
                    Boost();
                }
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
            var distFromAtmosphere = DistanceToClosestPlanet - atmosphereRadius;
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

        private void Boost()
        {
            if (fuelLevel < 5f) return;
            
            _speedLevel++;
            fuelLevel -= 5f;
            StatsUIManager.instance.UpdateShipGearUI(_speedLevel);
            StatsUIManager.instance.UpdateShipBoostStatusUI(false);
            _boostReady = false;
            
            var dir = horizontalThruster ? transform.right : transform.up;
            Rigidbody.velocity = Vector2.zero;
            Rigidbody.AddForce(dir * boostPower, ForceMode2D.Impulse);
            _boostTimer = boostInterval;
            boostParticles.Play();
            CameraController.CameraShake(0.2f, 0.6f + _speedLevel * 0.2f);
            UIUtilities.UIShake(0.2f, 2.25f + _speedLevel * 0.75f);
            
            var zoomMultiplier = CameraController.zoomMultiplier;
            CameraController.SetZoomMultiplierSmooth(zoomMultiplier * (1.25f + _speedLevel * 0.25f), 0.075f);
            GameUtilities.instance.DelayExecute(() =>
            {
                CameraController.SetZoomMultiplierSmooth(zoomMultiplier, 1.25f);
            }, 0.3f);
        }

        private void ToggleLandingMode()
        {
            // Prevent landing when taking off
            if (!_landingMode && !_canFly) return;
            
            if ((_landingMode || _grounded) && fuelLevel <= 0) return;
            
            _landingMode = !_landingMode;
            // TogglePhysics(_landingMode);
            // Toggle landing lights?

            if (_landingMode)
            {
                _shipAnimator.SetBool("landing_gear", true);
                movementParticles.Stop();
                CameraController.SetZoomMultiplierSmooth(1f, _grounded ? 1f : 3f);
            }
            else
            {
                var dir = _flipped ? Vector2.down : Vector2.up;
                Rigidbody.AddRelativeForce(dir * 15, ForceMode2D.Impulse);
                CameraController.SetZoomMultiplierSmooth(1.8f, 1f);

                if (_grounded)
                {
                    takeOffParticles.Play();
                    CameraController.CameraShake(0.125f, 0.35f);
                }
                
                GameUtilities.instance.DelayExecute(() =>
                {
                    _shipAnimator.SetBool("landing_gear", false);
                    dir = horizontalThruster ? Vector2.right : Vector2.up;
                    movementParticles.Play();
                    _canFly = true;
                    Boost();
                    // Rigidbody.AddRelativeForce(dir * 20, ForceMode2D.Impulse);
                    // CameraController.CameraShake(0.125f, 0.35f);
                }, 1f);
            }
        }

        private void Touchdown()
        {
            Rigidbody.velocity = Vector2.Lerp(Rigidbody.velocity, Vector2.zero, Time.deltaTime);
            _initialLandingDistance = 0;
            _canFly = false;
            landingParticles.Stop();
            StatsUIManager.instance.UpdateShipGearUI(-1);
            
            GameUtilities.instance.DelayExecute(() =>
            {
                _shipAnimator.SetTrigger("landed");
            }, 0.25f);
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
                
                StatsUIManager.instance.ShowShipHUD(hullHealth, fuelLevel, maxHullHealth, maxFuelLevel);
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
                
                StatsUIManager.instance.HideShipHUD();
            }
        }

        private void HandleCollisionWarning()
        {
            var velmag = Rigidbody.velocity.magnitude;
            if (velmag < 10f)
            {
                StatsUIManager.instance.HideDangerIcon();
                return;
            }
            
            var castDir = Rigidbody.velocity.normalized;
            var hit = Physics2D.Raycast(transform.position, castDir, velmag * 2.5f, _collisionLayerMask);

            if (!hit)
            {
                hit = Physics2D.CircleCast(transform.position, 0.5f, castDir, velmag * 2.5f, _collisionLayerMask);
                if (!hit)
                {
                    StatsUIManager.instance.HideDangerIcon();
                    return;
                }
            }
            
            StatsUIManager.instance.ShowDangerIcon();
            StatsUIManager.instance.UpdateDangerIcon(transform.position + (Vector3)castDir * 7f);
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (hullHealth <= 0) return;
            if (_invincibilityTimer > 0) return;
            
            var contact = other.GetContact(0);
            var hitVelocityDir = other.relativeVelocity.normalized;
            
            var dot = Vector2.Dot(contact.normal, hitVelocityDir);
            var correctedHitVelocity = other.relativeVelocity.magnitude * dot;
            
            if (correctedHitVelocity < 9f) return;

            _invincibilityTimer = 0.2f;

            var damage = (correctedHitVelocity - 9f) * 3f;
            hullHealth = Mathf.Clamp(hullHealth - damage, 0, maxHullHealth);
            StatsUIManager.instance.UpdateShipHullUI(hullHealth, maxHullHealth, true);
            UIUtilities.UIShake(0.2f, 4f);
            collisionParticles.transform.position = contact.point;
            var collisionPfxAngle = Mathf.Atan2(-hitVelocityDir.y, -hitVelocityDir.x) * Mathf.Rad2Deg + 90;
            collisionParticles.transform.rotation = Quaternion.Euler(0,0,collisionPfxAngle);

            var forceOverLifetimeModule = collisionParticles.forceOverLifetime;
            if (CurrentPlanetObject != null)
            {
                forceOverLifetimeModule.x = DirectionToClosestPlanet.x * 33f;
                forceOverLifetimeModule.y = DirectionToClosestPlanet.y * 33f;
            }
            else
            {
                forceOverLifetimeModule.x = 0f;
                forceOverLifetimeModule.y = 0f;
            }
            
            collisionParticles.Play();

            if (hullHealth <= 0)
            {
                TogglePassenger(_passenger.gameObject);
                CameraController.CameraShake(0.75f, 1f);
                PlayerController.instance.Death();
                Destroy(gameObject);
            }
            else
            {
                CameraController.CameraShake(0.2f, 0.7f);

                if (!lowHealthParticles.isEmitting && hullHealth / maxHullHealth < 0.15f)
                {
                    lowHealthParticles.Play();
                }
            }
        }
    }
}
