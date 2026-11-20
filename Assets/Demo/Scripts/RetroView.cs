using UnityEngine;
using UnityEngine.UI;

namespace Hakaton.Demo
{
    public sealed class RetroView : MonoBehaviour
    {
        public Camera worldCamera;
        public Material displayMaterial;
        public RawImage display;
        public int verticalResolution = 240;
        RenderTexture target;

        void OnEnable() => Resize();
        void LateUpdate()
        {
            int width = Mathf.RoundToInt(verticalResolution * (float)Screen.width / Mathf.Max(1, Screen.height));
            if (target == null || target.width != width) Resize();
        }

        void Resize()
        {
            if (worldCamera == null || display == null) return;
            if (target != null) { target.Release(); Destroy(target); }
            int width = Mathf.Max(240, Mathf.RoundToInt(verticalResolution * (float)Screen.width / Mathf.Max(1, Screen.height)));
            target = new RenderTexture(width, verticalResolution, 24, RenderTextureFormat.ARGB32)
            { name = "PS1 · 240 lines", filterMode = FilterMode.Point, antiAliasing = 1 };
            target.Create();
            worldCamera.targetTexture = target;
            worldCamera.aspect = (float)width / verticalResolution;
            display.texture = target;
            display.material = displayMaterial;
        }

        void OnDisable()
        {
            if (worldCamera != null) worldCamera.targetTexture = null;
            if (target != null) { target.Release(); Destroy(target); target = null; }
        }
    }
}
