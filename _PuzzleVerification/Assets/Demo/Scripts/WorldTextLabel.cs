using UnityEngine;

namespace Hakaton.Demo
{
    // The default dynamic-font material draws through walls; world signs need depth testing.
    [ExecuteAlways, RequireComponent(typeof(TextMesh))]
    public sealed class WorldTextLabel : MonoBehaviour
    {
        public Shader depthShader;
        Material material;
        TextMesh label;
        void OnEnable()
        {
            label = GetComponent<TextMesh>();
            if (depthShader == null) depthShader = Shader.Find("Hakaton/World Text");
            if (depthShader == null || label.font == null) return;
            material = new Material(depthShader) { hideFlags = HideFlags.HideAndDontSave };
            material.mainTexture = label.font.material.mainTexture;
            GetComponent<Renderer>().sharedMaterial = material;
            Font.textureRebuilt += RefreshFont;
        }
        void RefreshFont(Font font)
        { if (material != null && label != null && label.font == font) material.mainTexture = font.material.mainTexture; }
        void OnDisable()
        {
            Font.textureRebuilt -= RefreshFont;
            if (material != null)
            { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        }
    }
}
