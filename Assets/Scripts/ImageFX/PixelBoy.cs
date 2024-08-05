//PIXELBOY BY @WTFMIG EAT A BUTT WORLD BAHAHAHAHA POOP MY PANTS

using Cameras;
using UnityEngine;

namespace ImageFX
{
    [ExecuteInEditMode]
    [AddComponentMenu("Image Effects/PixelBoy")]
    public class PixelBoy : MonoBehaviour
    {
        public int w = 720;
        private int h;
        private Camera mainCam;

        private void Update()
        {
            mainCam ??= CameraController.instance.mainCam;
            var ratio = (float)mainCam.pixelHeight / mainCam.pixelWidth;
            h = Mathf.RoundToInt(w * ratio);
        
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            source.filterMode = FilterMode.Point;
            var buffer = RenderTexture.GetTemporary(w, h, -1);
            buffer.filterMode = FilterMode.Point;
            Graphics.Blit(source, buffer);
            Graphics.Blit(buffer, destination);
            RenderTexture.ReleaseTemporary(buffer);
        }
    }
}