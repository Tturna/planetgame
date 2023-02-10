using UnityEngine;

public class PixelateEffect : MonoBehaviour
{
    public Shader pixelArtFilter;

    [Range(0, 8)]
    public int downSamples;

    private Material _pixelArtMat;

    private void OnEnable() {
        _pixelArtMat ??= new Material(pixelArtFilter);
        _pixelArtMat.hideFlags = HideFlags.HideAndDontSave;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        var width = source.width;
        var height = source.height;

        var textures = new RenderTexture[8];

        var currentSource = source;

        for (var i = 0; i < downSamples; ++i) {
            width /= 2;
            height /= 2;

            if (height < 2)
                break;

            var currentDestination = textures[i] = RenderTexture.GetTemporary(width, height, 0, source.format);
            Graphics.Blit(currentSource, currentDestination, _pixelArtMat);
            currentSource = currentDestination;
        }

        Graphics.Blit(currentSource, destination, _pixelArtMat);

        for (var i = 0; i < downSamples; ++i) {
            RenderTexture.ReleaseTemporary(textures[i]);
        }
    }
}
