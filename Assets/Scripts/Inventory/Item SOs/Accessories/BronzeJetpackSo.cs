using System.Collections;
using Entities;
using UnityEngine;
using Utilities;

namespace Inventory.Item_SOs.Accessories
{
    [CreateAssetMenu(fileName = "Bronze Jetpack", menuName = "SO/Accessories/Bronze Jetpack")]
    public class BronzeJetpackSo : BasicAccessorySo
    {
        private Transform _playerBodyTransform;
        private ParticleSystem _jetpackParticles1, _jetpackParticles2;
        private float _doubleTapTimer;
        private const float DoubleTapTime = 0.2f;

        private void Dash(Vector3 relativeDirection)
        {
            if (_doubleTapTimer > 0)
            {
                PlayerController.instance.ResetVelocity(true, false, true);
                PlayerController.instance.AddRelativeForce(relativeDirection * 20f, ForceMode2D.Impulse);
                _doubleTapTimer = 0;
                GameUtilities.instance.StartCoroutine(DashFx(0.33f, relativeDirection));
            }
            else
            {
                _doubleTapTimer = DoubleTapTime;
            }
        }

        public override void ResetBehavior()
        {
            (_jetpackParticles1, _jetpackParticles2) = PlayerController.instance.GetJetpackParticles();
            _playerBodyTransform = PlayerController.instance.GetBodyTransform();
            _doubleTapTimer = 0f;
        }
        
        public override void UpdateProcess()
        {

            if (Input.GetKeyDown(KeyCode.A))
            {
                Dash(Vector3.left);
            }
            
            if (Input.GetKeyDown(KeyCode.D))
            {
                Dash(Vector3.right);
            }
            
            if (_doubleTapTimer > 0)
            {
                _doubleTapTimer -= Time.deltaTime;
            }
            else
            {
                _doubleTapTimer = 0;
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
        
        private IEnumerator DashFx(float duration, Vector3 relativeDirection)
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
                timer -= Time.deltaTime;
                yield return null;
            }
            
            _playerBodyTransform.localEulerAngles = Vector3.zero;
            _jetpackParticles1.Stop();
            _jetpackParticles2.Stop();
        }
    }
}