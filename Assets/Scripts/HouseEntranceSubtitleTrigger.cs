using UnityEngine;

/// <summary>
/// Entrance remarks when Alexey first steps inside the father's house.
/// Defaults to a player-zone trigger; place and radius are set from code or inspector.
/// </summary>
public class HouseEntranceSubtitleTrigger : SubtitleSequence
{
    void Awake()
    {
        if (lines != null && lines.Length > 0) return;
        lines = new[]
        {
            new Line { time = 0f, text = "А: А, ну хоть тут без фокусов." },
            new Line { time = 3f, text = "А: Так. С чего начнём…" },
        };
    }
}
