using UnityEngine;

namespace Hakaton.Demo
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class ArrivalHeadphones : MonoBehaviour
    {
        public AudioClip track;
        public string trackTitle = "Commodo — Living Bones";
        [Range(0, 1)] public float volume = 0.55f;
        public float fadeInSeconds = 3;
        public DemoGame game;
        public bool IsPlaying => source != null && source.isPlaying;
        AudioSource source;
        float elapsed;
        bool started;
        void Awake()
        {
            source = GetComponent<AudioSource>(); source.playOnAwake = false;
            source.spatialBlend = 0; source.loop = false; source.volume = 0;
        }
        void Update()
        {
            if (game == null || game.hud == null || game.hud.Mode != DemoHud.Screen.Play) return;
            if (!started && track != null)
            {
                started = true; source.clip = track; source.Play();
                game.hud.Notify("В НАУШНИКАХ · " + trackTitle, 6);
            }
            if (started)
            {
                elapsed += Time.deltaTime;
                source.volume = volume * Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeInSeconds));
            }
        }
    }
}
