using Entities;
using UnityEngine;

namespace Inventory.Item_SOs.Accessories
{
    [CreateAssetMenu(fileName = "Cobalt Jetpack", menuName = "SO/Accessories/Cobalt Jetpack")]
    public class CobaltJetpackSo : BasicAccessorySo
    {
        private PlayerController _playerController;
        private Transform _playerBodyTransform;
        private Rigidbody2D _playerRigidbody;
        private ParticleSystem _jetpackParticles1, _jetpackParticles2;
        private bool _particlesPlaying;

        public override void ResetBehavior()
        {
            _playerController = PlayerController.instance;
            _playerBodyTransform = _playerController.GetBodyTransform();
            _playerRigidbody = _playerController.GetComponent<Rigidbody2D>();
            (_jetpackParticles1, _jetpackParticles2) = _playerController.GetJetpackParticles();
        }
        
        public override void UpdateProcess()
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {
                _jetpackParticles1.Stop();
                _jetpackParticles2.Stop();
                _particlesPlaying = false;
                return;
            }

            var localRot = _playerBodyTransform.localRotation;

            if (!Input.GetKey(KeyCode.Space))
            {
                if (_playerBodyTransform.localRotation != Quaternion.identity)
                {
                    _playerBodyTransform.localRotation = Quaternion.Lerp(localRot, Quaternion.identity, Time.deltaTime * 10f);
                }
                
                return;
            }
            
            if (PlayerStatsManager.JetpackCharge <= 0) return;

            Vector3 forceDir;

            if (!_playerController.IsInSpace)
            {
                var inputVector = _playerController.GetInputVector();
                var targetRotation = Quaternion.Euler(0, 0, inputVector.x * -30f);
                _playerBodyTransform.localRotation = Quaternion.Lerp(localRot, targetRotation, Time.deltaTime * 10f);
                forceDir = new Vector3(inputVector.x * .5f, 1f, 0f).normalized;
            }
            else
            {
                forceDir = Vector3.up;
            }
            
            PlayerStatsManager.ChangeJetpackCharge(-Time.deltaTime);

            if (_playerRigidbody.velocity.magnitude < 30f)
            {
                _playerController.AddRelativeForce(forceDir * (2500f * Time.deltaTime), ForceMode2D.Force);
            }

            if (_particlesPlaying) return;
            
            _particlesPlaying = true;
            _jetpackParticles1.Play();
            _jetpackParticles2.Play();

            // Play jetpack sound
        }
    }
}