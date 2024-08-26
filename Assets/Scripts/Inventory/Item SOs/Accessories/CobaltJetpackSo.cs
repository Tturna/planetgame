using Entities;
using UnityEngine;
using Utilities;

namespace Inventory.Item_SOs.Accessories
{
    [CreateAssetMenu(fileName = "Cobalt Jetpack", menuName = "SO/Accessories/Cobalt Jetpack")]
    public class CobaltJetpackSo : BasicAccessorySo
    {
        private PlayerController _playerController;
        private Transform _playerBodyTransform;
        private Rigidbody2D _playerRigidbody;
        private ParticleSystem _jetpackParticles1, _jetpackParticles2;
        private GameObject _jetpackLight;
        private bool _particlesPlaying;
        private float _jetpackLightSpawnTimer;
        private const float JetpackLightSpawnInterval = 0.1f;

        public override void ResetBehavior()
        {
            _playerController = PlayerController.instance;
            _playerBodyTransform = _playerController.GetBodyTransform();
            _playerRigidbody = _playerController.GetComponent<Rigidbody2D>();
            (_jetpackParticles1, _jetpackParticles2) = _playerController.GetJetpackParticles();
            _jetpackLight = _playerController.GetJetpackLight();
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
            
            ObjectPooler.CreatePoolIfDoesntExist("JetpackLight", _jetpackLight, 21);
            
            if (_jetpackLightSpawnTimer < JetpackLightSpawnInterval)
            {
                _jetpackLightSpawnTimer += Time.deltaTime;
            }
            else
            {
                _jetpackLightSpawnTimer = 0f;
                var lightClone = ObjectPooler.GetObject("JetpackLight");
                var lightMoveDir = _playerController.transform.TransformDirection(-forceDir);

                if (lightClone)
                {
                    lightClone.transform.position = _playerBodyTransform.position + lightMoveDir * 0.5f;
                    
                    GameUtilities.TimedUpdate(() =>
                    {
                        if (!lightClone || !lightClone.activeSelf) return false;

                        var hit = Physics2D.Raycast(lightClone.transform.position, lightMoveDir, 0.15f,
                            GameUtilities.BasicMovementCollisionMask);

                        if (hit)
                        {
                            lightClone.SetActive(false);
                            return false;
                        }
                        
                        lightClone.transform.position += lightMoveDir * (Time.deltaTime * 7f);
                        return true;
                    }, 1f, () =>
                    {
                        if (lightClone && lightClone.activeSelf)
                        {
                            lightClone.SetActive(false);
                        }
                    });
                }
            }

            if (_particlesPlaying) return;
            
            _particlesPlaying = true;
            _jetpackParticles1.Play();
            _jetpackParticles2.Play();

            // Play jetpack sound
        }
    }
}