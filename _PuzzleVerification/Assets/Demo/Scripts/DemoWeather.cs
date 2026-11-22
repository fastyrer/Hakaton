using UnityEngine;

namespace Hakaton.Demo
{
    public sealed class DemoWeather : MonoBehaviour
    {
        public const float OutdoorRainVolume=.07f,ShelteredRainVolume=.02f;
        public DemoPlayer player;
        public ParticleSystem rain;
        public bool useShelterVolumes;
        public Collider[] shelters;
        public bool IsSheltered { get; private set; }
        public float RainFade {get;set;}=1;
        AudioSource rainAudio;
        AudioLowPassFilter filter;
        AudioClip rainfall;
        bool ownsRainfall;

        void Start()
        {
            rainfall=Resources.Load<AudioClip>("Audio/Rain soft loop");
            if(rainfall==null)
            {
            // Fallback only when the shared recorded asset is absent.
            const int rate = 22050;
            var samples = new float[rate * 4];
            var random = new System.Random(2000);
            float low = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float noise = (float)random.NextDouble() * 2 - 1;
                low = Mathf.Lerp(low, noise, 0.08f);
                float modulation = 0.8f + 0.15f * Mathf.Sin(i * Mathf.PI * 2 / samples.Length * 3);
                samples[i] = (noise * 0.13f + low * 0.75f) * modulation;
            }
            rainfall = AudioClip.Create("Rain outside the house", samples.Length, 1, rate, false);
            rainfall.SetData(samples, 0);ownsRainfall=true;
            }
            rainAudio = gameObject.AddComponent<AudioSource>();
            rainAudio.clip = rainfall;
            rainAudio.loop = true;
            rainAudio.volume = OutdoorRainVolume*RainFade;
            rainAudio.spatialBlend = 0;
            filter = gameObject.AddComponent<AudioLowPassFilter>();
            rainAudio.Play();
        }

        void Update()
        {
            if (player == null || rainAudio == null) return;
            Vector3 p = player.transform.position;
            if (useShelterVolumes)
            {
                IsSheltered = false;
                if (shelters != null) foreach (var shelter in shelters)
                    if (shelter != null && shelter.gameObject.activeInHierarchy &&
                        (shelter.ClosestPoint(p + Vector3.up) - (p + Vector3.up)).sqrMagnitude < 0.0001f)
                    { IsSheltered = true; break; }
            }
            else IsSheltered = (Mathf.Abs(p.x) < 9 && p.z > -5 && p.z < 13) ||
                (Mathf.Abs(p.x) < 2.7f && p.z > -8.8f && p.z < -5);
            rainAudio.volume = Mathf.MoveTowards(rainAudio.volume, (IsSheltered ? ShelteredRainVolume : OutdoorRainVolume)*RainFade, Time.unscaledDeltaTime * 0.6f);
            filter.cutoffFrequency = Mathf.Lerp(filter.cutoffFrequency, IsSheltered ? 1400 : 5000, Time.unscaledDeltaTime * 4);
        }

        void OnDestroy()
        {
            if (ownsRainfall && rainfall != null) Destroy(rainfall);
        }
    }
}
