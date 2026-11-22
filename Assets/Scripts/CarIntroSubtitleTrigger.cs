using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CarIntroSubtitleTrigger : MonoBehaviour
{
    [Header("Subtitle Settings")]
    [Tooltip("Reference to the SubtitleManager component")]
    [SerializeField] private SubtitleManager subtitleManager;

    [Tooltip("Audio clip for text blip (optional if SubtitleManager has one assigned)")]
    [SerializeField] private AudioClip blipSound;

    private HashSet<string> subtitlesDisplayed = new HashSet<string>();

    private void OnEnable()
    {
        InvokeRepeating("CheckSubtitleTime", 0f, 0.1f);
    }

    private void OnDisable()
    {
        CancelInvoke("CheckSubtitleTime");
    }

    private void CheckSubtitleTime()
    {
        if (subtitleManager == null) return;

        float timeSinceLevelLoad = Time.timeSinceLevelLoad;

        // Start subtitle at 0s
        if (timeSinceLevelLoad > 0.1f && timeSinceLevelLoad < 1.2f && !Contains(subtitlesDisplayed, "start"))
        {
            subtitleManager.ShowSubtitle("Эх… Как всё так сложилось.");
            AddIfNotPresent(subtitlesDisplayed, "start");
        }

        // First line around 5s
        if (timeSinceLevelLoad >= 2.5f && timeSinceLevelLoad < 5.0f && !Contains(subtitlesDisplayed, "first"))
        {
            subtitleManager.ShowSubtitle("Три года как отца не стало. И всё, что написали в деле, — несчастный случай.");
            AddIfNotPresent(subtitlesDisplayed, "first");
        }

        // Second line around 10s
        if (timeSinceLevelLoad >= 6.5f && timeSinceLevelLoad < 10.5f && !Contains(subtitlesDisplayed, "second"))
        {
            subtitleManager.ShowSubtitle("Почему я тогда сразу за это не взялся? Почему вещи его не разобрал?");
            AddIfNotPresent(subtitlesDisplayed, "second");
        }

        // Third line around 15s
        if (timeSinceLevelLoad >= 12.5f && timeSinceLevelLoad < 15.5f && !Contains(subtitlesDisplayed, "third"))
        {
            subtitleManager.ShowSubtitle("Приезжал же сюда. Походил, посмотрел… И уехал.");
            AddIfNotPresent(subtitlesDisplayed, "third");
        }

        // Fourth line around 20s
        if (timeSinceLevelLoad >= 16.5f && timeSinceLevelLoad < 19.0f && !Contains(subtitlesDisplayed, "fourth"))
        {
            subtitleManager.ShowSubtitle("Иван говорит, что надо было давно всё вывезти. Дмитрий вообще слышать об этом не хочет.");
            AddIfNotPresent(subtitlesDisplayed, "fourth");
        }

        // Fifth line around 25s
        if (timeSinceLevelLoad >= 20.5f && timeSinceLevelLoad < 21.5f && !Contains(subtitlesDisplayed, "fifth"))
        {
            subtitleManager.ShowSubtitle("Так, я на месте.");
            AddIfNotPresent(subtitlesDisplayed, "fifth");
        }

        // House appears around 22.7s
        if (timeSinceLevelLoad >= 22.2f && timeSinceLevelLoad < 24.2f && !Contains(subtitlesDisplayed, "house"))
        {
            subtitleManager.ShowSubtitle("Отец тот ещё параноик был. Сейчас ещё с замками намучаюсь.");
            AddIfNotPresent(subtitlesDisplayed, "house");
        }

        // "А: Так, я на месте." around 23.5s
        if (timeSinceLevelLoad >= 25.0f && timeSinceLevelLoad < 27.0f && !Contains(subtitlesDisplayed, "eighth"))
        {
            subtitleManager.ShowSubtitle("Старый любил такую дребедень.");
            AddIfNotPresent(subtitlesDisplayed, "eighth");
        }
    }

    private bool Contains(HashSet<string> set, string key) => set.Contains(key);
    private void AddIfNotPresent(HashSet<string> set, string key) { if (!set.Contains(key)) set.Add(key); }
}