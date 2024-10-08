using System;
using Cameras;
using Inventory;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;

namespace Entities
{
    [RequireComponent(typeof(Interactable))]
    [RequireComponent(typeof(Animator))]
    public class SpaceShipEntity : EntityController, IDamageable
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
        [SerializeField] private float startupTime;
        
        [Header("References/Components")]
        [SerializeField] private ParticleSystem thrusterParticles1;
        [SerializeField] private ParticleSystem thrusterParticles2;
        [SerializeField] private ParticleSystem takeOffParticles;
        [SerializeField] private ParticleSystem boostParticles;
        [SerializeField] private ParticleSystem landingParticles;
        [SerializeField] private ParticleSystem lowHealthParticles;
        [SerializeField] private ParticleSystem collisionParticles;
        [SerializeField] private GameObject effectParent;
        [SerializeField] private ParticleSystem explosionParticles;
        [SerializeField] private Transform starMapMarker;
        [SerializeField] private AudioSource genericAudioSource;
        [SerializeField] private AudioSource explosionAudioSource;
        [SerializeField] private AudioSource thrusterAudioSourceOne;
        [SerializeField] private AudioSource thrusterAudioSourceTwo;
        [SerializeField] private AudioSource boostAudioSource;
        [SerializeField] private AudioUtilities.Clip thrusterStartClip;
        [SerializeField] private AudioUtilities.Clip thrusterLoopClip;
        [SerializeField] private AudioUtilities.Clip explosionClip;
        [SerializeField] private AudioUtilities.Clip startupClip;
        [SerializeField] private AudioUtilities.Clip boostClip;
        [SerializeField] private AudioUtilities.Clip collisionClip;
    
        private bool _landingMode = true, _canFly;
        private bool _grounded;
        private float _boostTimer;
        private bool _boostReady = true;
        private EntityController _passenger;
        private Transform _oldPassengerParent;
        private Animator _shipAnimator;
        private SpriteRenderer _spriteRenderer;
        private Interactable _interactable;
        private ParticleSystem movementParticles;
        private GameObject movementParticleAnchor;
        private bool _flipped;
        private float _maxSpeed;
        private int _speedLevel = -1;
        private float _initialLandingDistance;
        private Quaternion _initialLandingRotation;
        private int _collisionLayerMask;
        private float _invincibilityTimer;
        private bool _thrusterFiring;
        private bool _thrusterOnePlaying;
        private float _startupTimer;
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

            movementParticleAnchor = GameUtilities.instance.GetSpaceMoveParticleAnchor();
            movementParticles = movementParticleAnchor.GetComponentInChildren<ParticleSystem>();
        }

        private void Update()
        {
            _interactable.IndicatorParent.rotation = CameraController.instance.mainCam.transform.rotation;
            _invincibilityTimer -= Time.deltaTime;
            
            if (!_passenger) return;

            if (_startupTimer < startupTime)
            {
                _startupTimer += Time.deltaTime;
            }

            if (_startupTimer >= startupTime && _passenger is PlayerController)
            {
                Controls();

                _interactable.canHoldInteract = InventoryManager.isWearingJetpack;
            }
            else
            {
                // Do something cool if an NPC or something gets into the space ship?
            }
            
            var castDir = _flipped ? transform.up : -transform.up;
            var hitCollider = Physics2D.OverlapCircle(transform.position + castDir * 0.7f, 0.7f, _collisionLayerMask);
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
            
            UpdateThrusterSound();
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
                var dirToPlanet = (Vector3)DirectionToClosestPlanet;
                var hit = Physics2D.Raycast(transform.position, dirToPlanet, 100f, _collisionLayerMask);

                if (!hit)
                {
                    Debug.LogError("No hit found for raycast to planet");
                    return;
                }

                var leftHit = Physics2D.Raycast(transform.position, dirToPlanet - transform.right * 0.1f, 100f, _collisionLayerMask);
                var rightHit = Physics2D.Raycast(transform.position, dirToPlanet + transform.right * 0.1f, 100f, _collisionLayerMask);
                var averageNormal = (leftHit.normal + rightHit.normal) / 2;
                var hitDiff = hit.point - (Vector2)transform.position;
                var distToGround = hitDiff.magnitude;
                
                if (_initialLandingDistance == 0)
                {
                    _initialLandingDistance = distToGround;
                    _initialLandingRotation = transform.rotation;
                }
                
                var normalDistToGround = 1f - distToGround / _initialLandingDistance;
                var targetRotation = Quaternion.LookRotation(Vector3.forward, averageNormal.normalized);
                transform.rotation = Quaternion.Slerp(_initialLandingRotation, targetRotation, normalDistToGround * 1.1f);
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
                ToggleThrusterSound(true);
            }
            else if (thrusterParticles1.isPlaying && _inputVector.y <= 0)
            {
                thrusterParticles1.Stop();
                thrusterParticles2.Stop();
                ToggleThrusterSound(false);
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

        private void ToggleThrusterSound(bool isPlaying)
        {
            _thrusterFiring = isPlaying;

            if (!_thrusterFiring) return;
            
            thrusterAudioSourceOne.clip = thrusterStartClip.audioClip;
            thrusterAudioSourceOne.volume = thrusterStartClip.volume;
            thrusterAudioSourceTwo.clip = thrusterLoopClip.audioClip;
            thrusterAudioSourceTwo.volume = thrusterLoopClip.volume;
            
            thrusterAudioSourceOne.PlayScheduled(AudioSettings.dspTime + 0.1d);
            var startDuration = (double)thrusterStartClip.audioClip.samples / thrusterStartClip.audioClip.frequency;
            thrusterAudioSourceTwo.loop = true;
            thrusterAudioSourceTwo.PlayScheduled(AudioSettings.dspTime + startDuration + 0.1d);
        }

        private void UpdateThrusterSound()
        {
            if (!_thrusterFiring)
            {
                var volume = Mathf.Max(thrusterAudioSourceOne.volume, thrusterAudioSourceTwo.volume);
                volume = Mathf.Lerp(volume, 0, Time.deltaTime * 5f);
                thrusterAudioSourceOne.volume = volume;
                thrusterAudioSourceTwo.volume = volume;
                
                if (volume <= 0)
                {
                    thrusterAudioSourceOne.Stop();
                    thrusterAudioSourceTwo.Stop();
                }
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
            
            movementParticleAnchor.transform.position = transform.position + (Vector3)velocity.normalized * 10f;
            movementParticleAnchor.transform.rotation = Quaternion.Euler(0,0,velocityAngle - 90);
        }

        private void InteractionImmediate(GameObject sourceObject)
        {
            if (_passenger && !_landingMode)
            {
                if (InventoryManager.isWearingJetpack)
                {
                    _interactable.canHoldInteract = true;
                }
                
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
            _boostTimer = boostInterval;
            
            var dir = horizontalThruster ? transform.right : transform.up;
            var localDir = horizontalThruster ? Vector2.right : Vector2.up;
            Rigidbody.velocity = Vector2.zero;
            Rigidbody.AddForce(dir * boostPower, ForceMode2D.Impulse);
            
            var boostTransform = boostParticles.transform;
            boostTransform.localPosition = localDir * boostTransform.localPosition.magnitude;
            boostTransform.right = dir;
            boostParticles.Play();
            
            CameraController.CameraShake(0.2f, 0.6f + _speedLevel * 0.2f);
            UIUtilities.UIShake(0.2f, 2.25f + _speedLevel * 0.75f);
            boostAudioSource.PlayOneShot(boostClip.audioClip, boostClip.volume);
            
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
            // Prevent cancelling a landing
            if (_landingMode && !_grounded) return;
            // Prevent landing in space
            if (!CurrentPlanetObject) return;
            
            _landingMode = !_landingMode;
            // TogglePhysics(_landingMode);
            // Toggle landing lights?

            if (_landingMode)
            {
                _shipAnimator.SetBool("landing_gear", true);
                movementParticles.Stop();
                gameObject.layer = LayerMask.NameToLayer("Enemy");
                CameraController.SetZoomMultiplierSmooth(1f, _grounded ? 1f : 3f);
            }
            else
            {
                // quiter boost for takeoff before aerial boost
                var dir = _flipped ? Vector2.down : Vector2.up;
                Rigidbody.AddRelativeForce(dir * 15, ForceMode2D.Impulse);
                boostAudioSource.PlayOneShot(boostClip.audioClip, boostClip.volume * 0.75f);
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
                    gameObject.layer = LayerMask.NameToLayer("Default");
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
                _passenger.ToggleCollision(false);
                _passenger.ToggleSpriteRenderer(false);
                var passengerTransform = _passenger.transform;
                _oldPassengerParent = passengerTransform.parent;
                passengerTransform.SetParent(transform);
                
                StatsUIManager.instance.ShowShipHUD(hullHealth, fuelLevel, maxHullHealth, maxFuelLevel);
                genericAudioSource.PlayOneShot(startupClip.audioClip, startupClip.volume);
                _startupTimer = 0f;

                if (_passenger is PlayerController && !_landingMode)
                {
                    CameraController.SetZoomMultiplierSmooth(1.8f, 1f);
                }
            }
            else
            {
                if (_landingMode)
                {
                    TogglePhysics(true);
                }
                
                _passenger.ToggleControl(true);
                _passenger.TogglePhysics(true);
                _passenger.ToggleCollision(true);
                _passenger.ToggleSpriteRenderer(true);
                _passenger.transform.SetParent(_oldPassengerParent);
                _oldPassengerParent = null;
                
                if (_passenger is PlayerController)
                {
                    CameraController.SetZoomMultiplierSmooth(1f, 1f);
                }
                
                _passenger = null;
                InventoryManager.RefreshEquippedItem();
                StatsUIManager.instance.HideShipHUD();
                genericAudioSource.Stop();
                _startupTimer = 0f;
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
        
        public bool CanBeDamaged()
        {
            return !_landingMode && hullHealth > 0;
        }

        public void TakeDamage(float amount, Vector3 damageSourcePosition)
        {
            hullHealth = Mathf.Clamp(hullHealth - amount, 0, maxHullHealth);
            StatsUIManager.instance.UpdateShipHullUI(hullHealth, maxHullHealth, true);
            UIUtilities.UIShake(0.2f, 4f);
            genericAudioSource.PlayOneShot(collisionClip.audioClip, collisionClip.volume);
            
            if (hullHealth <= 0)
            {
                Death();
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

        public void Knockback(Vector3 damageSourcePosition, float amount) { }

        public void Death(bool despawn = false)
        {
            TogglePassenger(_passenger.gameObject);

            if (!CurrentPlanetObject)
            {
                // ReSharper disable once RedundantAssignment
                var forceOverLifetimeModule = collisionParticles.forceOverLifetime;
                forceOverLifetimeModule = explosionParticles.forceOverLifetime;
                forceOverLifetimeModule.enabled = false;

                if (explosionParticles.subEmitters.subEmittersCount > 0)
                {
                    forceOverLifetimeModule = explosionParticles.subEmitters.GetSubEmitterSystem(0).forceOverLifetime;
                    forceOverLifetimeModule.enabled = false;
                }
            }
            
            effectParent.transform.SetParent(null);
            explosionParticles.transform.rotation = CameraController.instance.mainCam.transform.rotation;
            explosionParticles.Play();
            explosionAudioSource.PlayOneShot(explosionClip.audioClip, explosionClip.volume);
            Destroy(effectParent, 3f);
            CameraController.CameraShake(0.75f, 1f);
            PlayerController.instance.Death();
            
            GameUtilities.instance.DelayExecute(() =>
            {
                Destroy(explosionParticles.gameObject);
            }, 2f);
            
            Destroy(gameObject);
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
            TakeDamage(damage, other.transform.position);
            
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
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Rigidbody.velocity.magnitude < 10f) return;
            if (!other.transform.root.TryGetComponent<IDamageable>(out var damageable)) return;
            if (!damageable.CanBeDamaged()) return;

            float damageMultiplier;
            if (!other.gameObject.TryGetComponent<Rigidbody2D>(out var otherRb))
            {
                damageMultiplier = Rigidbody.velocity.magnitude * 0.1f;
                damageable.TakeDamage(20f * damageMultiplier, transform.position);
                damageable.Knockback(transform.position, 20f * damageMultiplier);
                return;
            }
            
            // other moving away -> lower relative velocity
            // other moving towards -> higher relative velocity
            var otherVelocity = otherRb.velocity;
            var shipVelocity = Rigidbody.velocity;
            var dot = Vector2.Dot(otherVelocity.normalized, shipVelocity.normalized);
            var otherDirectionalVelocity = otherVelocity.magnitude * dot;
            var relativeHitVelocity = otherDirectionalVelocity - Rigidbody.velocity.magnitude;
            
            if (relativeHitVelocity < 10f) return;
            
            damageMultiplier = relativeHitVelocity * 0.1f;
            damageable.TakeDamage(20f * damageMultiplier, transform.position);
            damageable.Knockback(transform.position, 20f * damageMultiplier);
        }
    }
}
