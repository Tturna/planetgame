using UnityEngine;

public class CameraController : MonoBehaviour
{
    private GameObject _planet;
    
    private void LateUpdate()
    {
        var dirToPlanet = (_planet.transform.position - transform.position).normalized;
        transform.LookAt(transform.position + Vector3.forward, -dirToPlanet);
    }

    public void SetTargetPlanet(Planet planet)
    {
        _planet = planet.gameObject;
    }
}
