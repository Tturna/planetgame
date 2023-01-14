using UnityEngine;

namespace Environment
{
    [RequireComponent(typeof(Planet))]
    public class FogManager : MonoBehaviour
    {
        private GameObject _planet;
        private GameObject[] _fogObjects;
    
        // Start is called before the first frame update
        void Start()
        {
            _planet = gameObject;
            _fogObjects = GameObject.FindGameObjectsWithTag("Fog");
        }

        // Update is called once per frame
        void Update()
        {
            foreach (var fogObject in _fogObjects)
            {
                var pos = fogObject.transform.position;
                var dirToPlanet = (_planet.transform.position - pos).normalized;
                fogObject.transform.LookAt(pos + Vector3.forward, -dirToPlanet);
                
                fogObject.transform.Translate(Vector3.right * (Time.deltaTime * 0.1f), Space.Self);
            }
        }
    }
}
