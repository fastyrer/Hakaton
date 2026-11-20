using UnityEngine;
using UnityEngine.Rendering;

namespace Hakaton.LocationKit
{
    [DisallowMultipleComponent]
    public sealed class LocationEnvironment : MonoBehaviour
    {
        public bool fog = true;
        public Color fogColor = new Color(0.10f, 0.145f, 0.17f);
        [Min(0)] public float fogStart = 13;
        [Min(1)] public float fogEnd = 53;
        public Color ambientColor = new Color(0.22f, 0.28f, 0.31f);
        public Color mainLightColor = new Color(0.53f, 0.68f, 0.82f);
        [Range(0, 3)] public float mainLightIntensity = 0.55f;
        public Light mainLight;
        public bool rain;
        public ParticleSystem rainSystem;
        public Vector2 rainArea = new Vector2(32, 32);
        [Range(4, 60)] public float rainHeight = 12;
        [Range(0, 4000)] public float rainRate = 1200;

        void Awake() => Apply();

        public void Apply()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientColor;
            RenderSettings.ambientEquatorColor = ambientColor * 0.7f;
            RenderSettings.ambientGroundColor = ambientColor * 0.4f;
            RenderSettings.skybox = null;
            RenderSettings.fog = fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = Mathf.Max(0, fogStart);
            RenderSettings.fogEndDistance = Mathf.Max(fogStart + 1, fogEnd);
            if (mainLight != null) { mainLight.color = mainLightColor; mainLight.intensity = mainLightIntensity; }
            if (rainSystem != null)
            {
                rainSystem.transform.parent.gameObject.SetActive(rain);
                var shape = rainSystem.shape;
                shape.scale = new Vector3(Mathf.Max(4, rainArea.x), Mathf.Max(4, rainArea.y), 0.1f);
                rainSystem.transform.localPosition = new Vector3(0, rainHeight, 0);
                var emission = rainSystem.emission;
                emission.rateOverTime = rainRate;
                var main = rainSystem.main;
                main.startLifetime = (rainHeight + 6) / 15;
                main.maxParticles = Mathf.Max(100, Mathf.CeilToInt(rainRate * (rainHeight + 6) / 15) + 100);
            }
            var root = GetComponent<LocationRoot>();
            if (root != null && root.game != null && root.game.player != null)
                root.game.player.view.backgroundColor = fogColor;
        }
    }
}
