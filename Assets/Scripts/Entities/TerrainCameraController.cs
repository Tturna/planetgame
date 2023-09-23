using UnityEngine;

namespace Entities
{
    public class TerrainCameraController : MonoBehaviour
    {
        [SerializeField] private RectTransform terrainRender;
        
        private Camera _mainCam;

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
