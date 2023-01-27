using System;
using Entities;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private GameObject _planet;
    private PlayerController _player;

    private void Start()
    {
        _player = PlayerController.instance;
        _player.OnEnteredPlanet += SetTargetPlanet;
    }

    private void LateUpdate()
    {
        if (!_planet) return;

        var trPos = transform.position;
        var dirToPlanet = (_planet.transform.position - trPos).normalized;
        transform.LookAt(trPos + Vector3.forward, -dirToPlanet);
    }

    private void SetTargetPlanet(Planet planet)
    {
        _planet = planet.gameObject;
    }
}
