using UnityEngine;

namespace Entities
{
    [RequireComponent(typeof(Camera))]
    public class TerrainCameraController : MonoBehaviour
    {
        public Camera Camera { get; private set; }
        
        [SerializeField] private RectTransform terrainRender;
        
        private Camera _mainCam;
        
        private void Awake()
        {
            Camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            _mainCam ??= Camera.main;

            transform.position = _mainCam!.transform.position;
            
            // prevent rotation
            transform.rotation = Quaternion.identity;
            terrainRender.rotation = Quaternion.identity;
        }
    }
}
