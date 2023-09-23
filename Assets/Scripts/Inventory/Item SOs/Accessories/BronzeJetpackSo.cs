using Entities;
using UnityEngine;

namespace Inventory.Item_SOs.Accessories
{
    [CreateAssetMenu(fileName = "Bronze Jetpack", menuName = "SO/Accessories/Bronze Jetpack")]
    public class BronzeJetpackSo : BasicAccessorySo
    {
        private ParticleSystem _jetpackParticles;
        private bool _particlesPlaying;
        
        public override void UpdateProcess()
        {
            if (_jetpackParticles == null)
            {
                _jetpackParticles = PlayerController.instance.GetJetpackParticles();
            }

            if (Input.GetKey(KeyCode.Space))
            {
                if (PlayerStatsManager.GetJetpackCharge() > 0)
                {
                    PlayerStatsManager.ChangeJetpackCharge(-Time.deltaTime);
                    PlayerController.instance.AddRelativeForce(Vector3.up * (3000f * Time.deltaTime), ForceMode2D.Force);
                    
                    if (!_particlesPlaying)
                    {
                        _particlesPlaying = true;
                        _jetpackParticles.Play();
                    }
                    
                    // Play jetpack sound

                    return;
                }
            }
            
            _jetpackParticles.Stop();
            _particlesPlaying = false;
        }
    }
}