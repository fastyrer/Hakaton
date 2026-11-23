using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Data-driven subtitle sequence. Replaces hand-written per-line timers:
/// lines are plain data, the trigger decides when the sequence starts.
/// Trigger modes: OnStart (after scene load), PlayerZone (player enters an area
/// once), Event (started by code via SubtitleSequence.Fire), Manual (call Play()).
/// </summary>
public class SubtitleSequence : MonoBehaviour
{
    public enum Trigger { OnStart, PlayerZone, Event, Manual }

    [Serializable]
    public class Line
    {
        [Tooltip("Seconds from sequence start")]
        public float time;
        [TextArea] public string text;
    }

    [Header("Source")]
    [SerializeField] protected SubtitleManager subtitleManager;
    [SerializeField] protected AudioClip blipSound;

    [Header("Sequence")]
    [SerializeField] protected Trigger trigger = Trigger.OnStart;
    [Tooltip("Wait until the HUD is back in gameplay mode before starting the clock")]
    [SerializeField] protected bool waitForGameplay = true;
    [SerializeField] protected float startDelay;
    [SerializeField] protected Line[] lines = new Line[0];
    [Tooltip("Seconds per character when stretching short slots to readable durations")]
    [SerializeField] protected float timePerCharacter = 0.055f;
    [SerializeField] protected float minLineTime = 2.4f;
    [SerializeField] protected float maxLineTime = 9f;

    [Header("Player zone trigger")]
    [SerializeField] protected Vector3 zoneCenter;
    [SerializeField] protected float zoneRadius = 2f;

    [Header("Event trigger")]
    [SerializeField] protected string eventId;

    [Header("Persistence")]
    [Tooltip("Optional puzzle flag: the sequence plays only while this state is not Solved and marks it Solved when played")]
    [SerializeField] protected string guardFlag;

    bool played;
    Hakaton.Demo.DemoGame game;

    /// <summary>Chainable runtime configuration, used when sequences are spawned by code.</summary>
    public SubtitleSequence AsZone(Vector3 center, float radius)
    { trigger = Trigger.PlayerZone; zoneCenter = center; zoneRadius = radius; return this; }
    public SubtitleSequence AsEvent(string id)
    { trigger = Trigger.Event; eventId = id; return this; }
    public SubtitleSequence WithLines(params Line[] value)
    { lines = value; return this; }
    public SubtitleSequence WithDelay(float delay)
    { startDelay = delay; return this; }
    public SubtitleSequence WithGuard(string flag)
    { guardFlag = flag; return this; }

    /// <summary>Start every Event sequence with the given id. Safe to call for ids nobody listens to.</summary>
    public static void Fire(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        foreach (var sequence in FindObjectsByType<SubtitleSequence>(FindObjectsInactive.Include))
            if (sequence.trigger == Trigger.Event && sequence.eventId == id) sequence.Play();
    }

    protected virtual void Start()
    {
        if (trigger == Trigger.OnStart) Play();
    }

    protected virtual void Update()
    {
        if (played || trigger != Trigger.PlayerZone) return;
        if (game == null) game = FindAnyObjectByType<Hakaton.Demo.DemoGame>();
        if (game == null || game.player == null) return;
        Vector3 p = game.player.transform.position;
        if (Mathf.Abs(p.y - zoneCenter.y) < 2f &&
            Vector2.Distance(new Vector2(p.x, p.z), new Vector2(zoneCenter.x, zoneCenter.z)) <= zoneRadius)
            Play();
    }

    public void Play()
    {
        if (played || lines == null || lines.Length == 0 || !isActiveAndEnabled) return;
        if (!string.IsNullOrEmpty(guardFlag))
        {
            if (game == null) game = FindAnyObjectByType<Hakaton.Demo.DemoGame>();
            if (game != null && game.GetPuzzleState(guardFlag) == Hakaton.Demo.PuzzleState.Solved) { played = true; return; }
            if (game != null) game.SetPuzzleState(guardFlag, Hakaton.Demo.PuzzleState.Solved);
        }
        played = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        if (waitForGameplay)
        {
            // Do not talk over intro cards and menus; start once the player is in control.
            while (true)
            {
                if (game == null) game = FindAnyObjectByType<Hakaton.Demo.DemoGame>();
                if (game == null || game.hud == null || game.hud.Mode == Hakaton.Demo.DemoHud.Screen.Play) break;
                yield return null;
            }
        }
        if (startDelay > 0f) yield return new WaitForSecondsRealtime(startDelay);
        var manager = subtitleManager != null ? subtitleManager : FindAnyObjectByType<SubtitleManager>();
        if (manager == null)
        {
            manager = new GameObject("Subtitles").AddComponent<SubtitleManager>();
        }
        float t0 = Time.unscaledTime;
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;
            while (Time.unscaledTime - t0 < line.time) yield return null;
            float readable = Mathf.Clamp(1.2f + line.text.Length * timePerCharacter, minLineTime, maxLineTime);
            float duration = i + 1 < lines.Length ? Mathf.Max(lines[i + 1].time - line.time, 1f) : readable;
            manager.ShowSubtitle(line.text, duration);
        }
    }
}
