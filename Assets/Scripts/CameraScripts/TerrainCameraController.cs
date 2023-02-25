using UnityEngine;

namespace CameraScripts
{
    public class TerrainCameraController : MonoBehaviour
    {
        private Camera mainCam;

        private void LateUpdate()
        {
            mainCam ??= Camera.main;

            transform.position = mainCam.transform.position;
        }
    }
}
