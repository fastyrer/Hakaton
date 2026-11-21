using UnityEngine;

namespace Hakaton.Demo
{
    // Small original synthesized placeholders; no downloaded audio or licensed music.
    public sealed class DemoSound : MonoBehaviour
    {
        public float Volume { get => AudioListener.volume; set => AudioListener.volume = value; }
        AudioSource local;
        AudioClip step, creak, knock, impact, hum;
        System.Random random;

        void Awake()
        {
            if(PlayerPrefs.HasKey(DemoPreferences.VolumeKey))AudioListener.volume=DemoPreferences.Volume;
            random = new System.Random(2000);
            local = gameObject.AddComponent<AudioSource>();
            local.spatialBlend = 0;
            step = Make("Floorboard", 0.15f, 74, 0.32f);
            creak = Make("Door hinge", 0.55f, 145, 0.13f);
            knock = Make("Behind the wall", 0.27f, 58, 0.38f);
            impact = Make("Power trip", 0.65f, 39, 0.8f);
            hum = Make("House tone", 4, 53, 0.013f, true);
            var ambient = gameObject.AddComponent<AudioSource>();
            ambient.clip = hum;
            ambient.loop = true;
            ambient.volume = 0.5f;
            ambient.Play();
        }

        AudioClip Make(string title, float seconds, float frequency, float noise, bool loop = false)
        {
            const int rate = 22050;
            var data = new float[(int)(seconds * rate)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = loop ? 1 : Mathf.Exp(-t * 9) * Mathf.Min(t * 180, 1);
                data[i] = (Mathf.Sin(t * frequency * Mathf.PI * 2) * 0.22f +
                    ((float)random.NextDouble() * 2 - 1) * noise) * envelope;
            }
            var clip = AudioClip.Create(title, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void Footstep(bool running)
        {
            local.pitch = Random.Range(0.84f, 1.06f);
            local.PlayOneShot(step, running ? 0.35f : 0.2f);
        }
        public void Creak(Vector3 position) => AudioSource.PlayClipAtPoint(creak, position, 0.32f);
        public void Knock(Vector3 position) => AudioSource.PlayClipAtPoint(knock, position, 0.65f);
        public void Stinger() { local.pitch = 0.76f; local.PlayOneShot(impact, 0.65f); }

        void OnDestroy()
        {
            foreach (var clip in new[] { step, creak, knock, impact, hum })
                if (clip != null) Destroy(clip);
        }
    }
}
