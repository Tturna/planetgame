using UnityEngine;

namespace CameraScripts
{
    public class TerrainCameraController : MonoBehaviour
    {
        [SerializeField] private RectTransform terrainRender;
        
        private Camera mainCam;

        private void LateUpdate()
        {
            mainCam ??= Camera.main;

            transform.position = mainCam!.transform.position;
            
            // prevent rotation
            transform.rotation = Quaternion.identity;
            terrainRender.rotation = Quaternion.identity;
        }
    }
}
