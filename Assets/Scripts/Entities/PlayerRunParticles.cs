using UnityEngine;

namespace Entities.Entities
{
    public class PlayerRunParticles : MonoBehaviour
    {
        [SerializeField] private float maxEmissionRate;
        
        private Rigidbody2D _playerRb;
        private ParticleSystem _runPs;
        private ParticleSystem.EmissionModule _runPsEmission;

        private bool _canEmit;
        
        private void Start()
        {
            var player = PlayerController.instance;
            _playerRb = player.GetComponent<Rigidbody2D>();
            _runPs = GetComponent<ParticleSystem>();
            _runPsEmission = _runPs.emission;
            
            player.Jumped += OnJump;
            player.Grounded += OnGrounded;
            
            _canEmit = true;
            _runPs.Play();
        }
        
        private const float MaxVelocity = 4f;
        private void Update()
        {
            if (!_canEmit) return;
            var velocity = _playerRb.velocity.magnitude;
            // Debug.Log($"Velocity: {velocity}");

            var nVel = velocity / MaxVelocity;
            var emissionRate = Mathf.Lerp(0, maxEmissionRate, nVel);
            _runPsEmission.rateOverTime = emissionRate;
        }
        
        private void OnJump()
        {
            _canEmit = false;
            _runPsEmission.rateOverTime = 0f;
        }
        
        private void OnGrounded()
        {
            _canEmit = true;
        }
    }
}
