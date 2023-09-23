using Entities;
using Planets;
using UnityEngine;
using Utilities;

namespace VFX
{
    public class BackgroundImageManager : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bg1, bg2;
        [SerializeField] private Sprite defaultSprite;
    
        private PlanetGenerator _currentPlanetGen;
        private PlayerController _player;
        private Color bg2Color;
        
        public static BackgroundImageManager instance;

        private void Start()
        {
            instance = this;
            _player = PlayerController.instance;
            _player.OnEnteredPlanet += OnEnteredPlanet;
            _player.OnExitPlanet += OnExitedPlanet;

            bg1.sprite = defaultSprite;
            bg1.color = Color.black;
        }

        private void Update()
        {
            if (!_currentPlanetGen) return;
            
            // Smooth transition between backgrounds according to player position.
            var perc = _currentPlanetGen.GetDistancePercentage(_player.transform.position);
            var limitedPerc = GameUtilities.InverseLerp(0f, 0.5f, perc);
        
            var c = bg2.color;
            c.a = limitedPerc;
            bg2.color = c;
        }

        private void OnEnteredPlanet(GameObject planetObject)
        {
            _currentPlanetGen = planetObject.GetComponent<PlanetGenerator>();

            bg2.sprite = _currentPlanetGen.surfaceCameraBackground ? _currentPlanetGen.surfaceCameraBackground : defaultSprite;
            bg2Color = _currentPlanetGen.surfaceBackgroundColor;
            bg2.color = bg2Color;
        }

        private void OnExitedPlanet(GameObject planetObject)
        {
            _currentPlanetGen = null;
            bg2.sprite = defaultSprite;
            bg2.color = Color.clear;
        }
        
        public static void SetBackgroundBrightness(float brightness)
        {
            var c = instance.bg2.color;
            // var c = instance.bg2Color;
            // var a = c.a;
            // c *= brightness;
            // c.a = a;
            c.a *= brightness;
            instance.bg2.color = c;
        }
    }
}
