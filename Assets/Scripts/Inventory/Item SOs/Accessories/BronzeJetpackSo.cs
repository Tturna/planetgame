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
        private ParticleSystem _jetpackParticles;
        private float _doubleTapTimer;
        private const float DoubleTapTime = 0.2f;

        private void Dash(Vector3 relativeDirection)
        {
            if (_doubleTapTimer > 0)
            {
                PlayerController.instance.ResetVelocity(true, false, true);
                PlayerController.instance.AddRelativeForce(relativeDirection * (900f * Time.deltaTime), ForceMode2D.Impulse);
                _doubleTapTimer = 0;
                GameUtilities.instance.StartCoroutine(DashFx(0.25f));
            }
            else
            {
                _doubleTapTimer = DoubleTapTime;
            }
        }

        public override void ResetBehavior()
        {
            _jetpackParticles = PlayerController.instance.GetJetpackParticles();
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

        private IEnumerator DashFx(float duration)
        {
            var timer = duration;
            _jetpackParticles.Play();

            while (timer > 0f)
            {
                _playerBodyTransform.localEulerAngles = Vector3.forward * Mathf.Lerp(20f, 0f, timer / duration);
                timer -= Time.deltaTime;
                yield return null;
            }
            
            _jetpackParticles.Stop();
        }
    }
}