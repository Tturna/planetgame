using Entities;
using UnityEngine;

namespace Inventory.Item_SOs.Accessories
{
    public class CobaltJetpackSo : BasicAccessorySo
    {
        private ParticleSystem _jetpackParticles1, _jetpackParticles2;
        private bool _particlesPlaying;
        
        public override void UpdateProcess()
        {
            if (_jetpackParticles1 == null)
            {
                (_jetpackParticles1, _jetpackParticles2) = PlayerController.instance.GetJetpackParticles();
            }

            if (Input.GetKey(KeyCode.Space))
            {
                if (PlayerStatsManager.GetJetpackCharge() > 0)
                {
                    PlayerStatsManager.ChangeJetpackCharge(-Time.deltaTime);
                    PlayerController.instance.AddRelativeForce(Vector3.up * (3000f * Time.deltaTime),
                        ForceMode2D.Force);

                    if (!_particlesPlaying)
                    {
                        _particlesPlaying = true;
                        _jetpackParticles1.Play();
                        _jetpackParticles2.Play();
                    }

                    // Play jetpack sound

                    return;
                }
            }

            _jetpackParticles1.Stop();
            _jetpackParticles2.Stop();
            _particlesPlaying = false;
        }
    }
}