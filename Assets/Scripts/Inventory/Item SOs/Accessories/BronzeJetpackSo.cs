using System.Collections;
using Entities;
using UnityEngine;
using Utilities;

namespace Inventory.Item_SOs.Accessories
{
    [CreateAssetMenu(fileName = "Bronze Jetpack", menuName = "SO/Accessories/Bronze Jetpack")]
    public class BronzeJetpackSo : BasicAccessorySo
    {
        private PlayerController _playerController;
        private Transform _playerBodyTransform;
        private Rigidbody2D _playerRigidbody;
        private ParticleSystem _jetpackParticles1, _jetpackParticles2;
        private GameObject _jetpackLight;
        private float _doubleTapTimer;
        private bool _particlesPlaying;
        private const float DoubleTapTime = 0.2f;
        private float _jetpackLightSpawnTimer;
        private const float JetpackLightSpawnInterval = 0.1f;

        private void Dash(Vector3 relativeDirection, Vector3 forceDir)
        {
            if (_doubleTapTimer > 0)
            {
                PlayerController.instance.ResetVelocity(true, false, true);
                PlayerController.instance.AddRelativeForce(relativeDirection * 20f, ForceMode2D.Impulse);
                _doubleTapTimer = 0;
                GameUtilities.instance.StartCoroutine(DashFx(0.33f, relativeDirection, forceDir));
            }
            else
            {
                _doubleTapTimer = DoubleTapTime;
            }
        }

        public override void ResetBehavior()
        {
            _playerController = PlayerController.instance;
            _playerBodyTransform = _playerController.GetBodyTransform();
            _doubleTapTimer = 0f;
            _playerRigidbody = _playerController.GetComponent<Rigidbody2D>();
            (_jetpackParticles1, _jetpackParticles2) = _playerController.GetJetpackParticles();
            _jetpackLight = _playerController.GetJetpackLight();
        }
        
        public override void UpdateProcess()
        {
            var forceDir = Vector3.up;
            
            if (Input.GetKeyUp(KeyCode.Space))
            {
                _jetpackParticles1.Stop();
                _jetpackParticles2.Stop();
                _particlesPlaying = false;
                return;
            }
            
            if (Input.GetKeyDown(KeyCode.A))
            {
                Dash(Vector3.left, forceDir);
            }
            
            if (Input.GetKeyDown(KeyCode.D))
            {
                Dash(Vector3.right, forceDir);
            }
            
            if (_doubleTapTimer > 0)
            {
                _doubleTapTimer -= Time.deltaTime;
            }
            else
            {
                _doubleTapTimer = 0;
            }

            if (!_playerController.IsInSpace) return;
            if (PlayerStatsManager.JetpackCharge <= 0) return;
            if (!Input.GetKey(KeyCode.Space)) return;

            PlayerStatsManager.ChangeJetpackCharge(-Time.deltaTime);

            if (_playerRigidbody.velocity.magnitude < 30f)
            {
                _playerController.AddRelativeForce(forceDir * (1000f * Time.deltaTime), ForceMode2D.Force);
            }
            
            JetpackLights(forceDir);
            
            if (_particlesPlaying) return;
            
            _particlesPlaying = true;
            _jetpackParticles1.Play();
            _jetpackParticles2.Play();
        }

        private void JetpackLights(Vector3 forceDir)
        {
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
        }

        private static float TiltFunction(float nTimer)
        {
            var tiltV = -Mathf.Pow(nTimer - 0.5f, 2) * 4f + 1f;
            var powered = Mathf.Pow(tiltV, 1f / 4f);
            return powered;
        }

        private static float TiltEase(float nTimer)
        {
            return Mathf.Sqrt(1 - Mathf.Pow(nTimer - 1, 2));
        }
        
        private IEnumerator DashFx(float duration, Vector3 relativeDirection, Vector3 forceDir)
        {
            var timer = duration;
            _jetpackParticles1.Play();
            _jetpackParticles2.Play();
            var dirMult = relativeDirection.x > 0 ? -1 : 1;

            while (timer > 0f)
            {
                var nTimer = 1 - timer / duration;
                // var ease = Mathf.Pow(TiltEase(nTimer), 1.5f);
                // var tiltV = Mathf.Sqrt(TiltFunction(ease));
                var ease = TiltEase(nTimer);
                var tiltV = TiltFunction(ease);
                var tilt = Mathf.Lerp(0f, 30f, tiltV);
                // Debug.Log($"nTimer: {nTimer},\nEase: {ease},\nTiltV: {tiltV},\nTilt: {tilt}");
                _playerBodyTransform.localEulerAngles = Vector3.forward * (dirMult * tilt);
                JetpackLights(forceDir);
                timer -= Time.deltaTime;
                yield return null;
            }
            
            _playerBodyTransform.localEulerAngles = Vector3.zero;
            _jetpackParticles1.Stop();
            _jetpackParticles2.Stop();
        }
    }
}