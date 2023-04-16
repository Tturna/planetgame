using Entities;
using UnityEngine;

public class BackgroundImageManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bg1, bg2;
    [SerializeField] private Sprite defaultSprite;
    
    private PlayerController _player;
    private Planet _currentPlanet;

    private void Start()
    {
        _player = PlayerController.instance;
        _player.OnEnteredPlanet += OnEnteredPlanet;
        _player.OnExitPlanet += OnExitedPlanet;

        bg1.sprite = defaultSprite;
        bg1.color = Color.black;
    }

    private void Update()
    {
        // Smooth transition between backgrounds according to player position.
        var perc = _currentPlanet.GetDistancePercentage(_player.transform.position);
        var limitedPerc = Utilities.InverseLerp(0f, 0.5f, perc);
        
        var c = bg2.color;

        c.a = limitedPerc;

        bg2.color = c;
    }

    private void OnEnteredPlanet(Planet planet)
    {
        _currentPlanet = planet;

        bg2.sprite = _currentPlanet.surfaceCameraBackground ? _currentPlanet.surfaceCameraBackground : defaultSprite;
        bg2.color = _currentPlanet.surfaceBackgroundColor;
    }

    private void OnExitedPlanet(Planet planet)
    {
        _currentPlanet = null;
        bg2.sprite = defaultSprite;
    }
}
