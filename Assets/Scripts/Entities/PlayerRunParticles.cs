using UnityEngine;

namespace Entities
{
    public class PlayerRunParticles : MonoBehaviour
    {
        [SerializeField] private float maxEmissionRate;
        [SerializeField] private float positionOffset;
        
        private PlayerController _player;
        private Rigidbody2D _playerRb;
        private ParticleSystem _runPfx;
        private ParticleSystem.EmissionModule _runPfxEmission;
        private Transform _runPfxTransform;

        private bool _canEmit;
        
        private void Start()
        {
            _player = PlayerController.instance;
            _playerRb = _player.GetComponent<Rigidbody2D>();
            _runPfx = GetComponent<ParticleSystem>();
            _runPfxEmission = _runPfx.emission;
            _runPfxTransform = _runPfx.transform;
            
            _player.Jumped += OnJump;
            _player.Grounded += OnGrounded;
            
            _canEmit = true;
            _runPfx.Play();
        }

        private void OnDestroy()
        {
            var player = PlayerController.instance;
            player.Jumped -= OnJump;
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
        }
        
        private void OnJump()
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
