using UnityEngine;

public class CameraController : MonoBehaviour
{
    private GameObject _planet;
    
    private void LateUpdate()
    {
        if (!_planet) return;

        var trPos = transform.position;
        var dirToPlanet = (_planet.transform.position - trPos).normalized;
        transform.LookAt(trPos + Vector3.forward, -dirToPlanet);
    }

    public void SetTargetPlanet(Planet planet)
    {
        _planet = planet.gameObject;
    }
}
