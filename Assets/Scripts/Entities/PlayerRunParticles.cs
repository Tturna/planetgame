using Inventory;
using UnityEngine;

namespace Entities
{
    public class PlayerRunParticles : MonoBehaviour
    {
        [SerializeField] private float maxEmissionRate;
        [SerializeField] private float positionOffset;
        [SerializeField] private AudioSource footstepsAudioSource;
        [SerializeField] private AudioClip[] footstepsClips;
        
        private PlayerController _player;
        private Rigidbody2D _playerRb;
        private ParticleSystem _runPfx;
        private ParticleSystem.EmissionModule _runPfxEmission;
        private Transform _runPfxTransform;

        private bool _canEmit;
        private float _footstepTimer;
        private float _footstepInterval;
        
        private void Start()
        {
            _player = PlayerController.instance;
            _playerRb = _player.GetComponent<Rigidbody2D>();
            _runPfx = GetComponent<ParticleSystem>();
            _runPfxEmission = _runPfx.emission;
            _runPfxTransform = _runPfx.transform;
            
            _player.Aerial += OnAerial;
            _player.Grounded += OnGrounded;
            
            _canEmit = true;
            _runPfx.Play();
        }

        private void OnDestroy()
        {
            var player = PlayerController.instance;
            player.Aerial -= OnAerial;
            player.Grounded -= OnGrounded;
        }

        private const float MaxVelocity = 4f;
        private void Update()
        {
            if (!_canEmit) return;
            var velocity = _playerRb.velocity.magnitude;
            var nVel = velocity / MaxVelocity;
            var emissionRate = Mathf.Lerp(0, maxEmissionRate, nVel);
            _runPfxEmission.rateOverTime = emissionRate;
            
            var runDirection = _player.GetInputVector().x > 0 ? -1 : 1;
            var position = _runPfxTransform.localPosition;
            position.x = runDirection * positionOffset;
            _runPfxTransform.localPosition = position;

            if (footstepsClips.Length == 0) return;
            if (velocity < 0.1f) return;
            
            _footstepTimer += Time.deltaTime;
            if (_footstepTimer < _footstepInterval) return;
            _footstepTimer = 0f;
            _footstepInterval = Mathf.Lerp(0.5f, 0.1f, velocity / (MaxVelocity * 2));
            footstepsAudioSource.Stop();
            footstepsAudioSource.PlayOneShot(footstepsClips[Random.Range(0, footstepsClips.Length)]);
        }
        
        private void OnAerial()
        {
            _canEmit = false;
            _runPfxEmission.rateOverTime = 0f;
        }
        
        private void OnGrounded()
        {
            _canEmit = true;
        }
    }
}
