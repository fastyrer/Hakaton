using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HouseEntranceSubtitleTrigger : MonoBehaviour
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

        // "Вход в дом" when entering the house (around 0-1s)
        if (timeSinceLevelLoad >= 0f && timeSinceLevelLoad < 2f && !Contains(subtitlesDisplayed, "entrance"))
        {
            subtitleManager.ShowSubtitle("Вход в дом");
            AddIfNotPresent(subtitlesDisplayed, "entrance");
            PlayBlip();
        }

        // "А: А, ну хоть тут без фокусов." after entering
        if (timeSinceLevelLoad >= 1f && timeSinceLevelLoad < 3f && !Contains(subtitlesDisplayed, "e1"))
        {
            subtitleManager.ShowSubtitle("А: А, ну хоть тут без фокусов.");
            AddIfNotPresent(subtitlesDisplayed, "e1");
            PlayBlip();
        }

        // "А: Так. С чего начнём…" after entering house
        if (timeSinceLevelLoad >= 3f && timeSinceLevelLoad < 5f && !Contains(subtitlesDisplayed, "e2"))
        {
            subtitleManager.ShowSubtitle("А: Так. С чего начнём…");
            AddIfNotPresent(subtitlesDisplayed, "e2");
            PlayBlip();
        }

        // "А: Даже это заколотил. Ну и зачем тебе тогда окна вообще?" looking at window
        if (timeSinceLevelLoad >= 5f && timeSinceLevelLoad < 7f && !Contains(subtitlesDisplayed, "e3"))
        {
            subtitleManager.ShowSubtitle("А: Даже это заколотил. Ну и зачем тебе тогда окна вообще?");
            AddIfNotPresent(subtitlesDisplayed, "e3");
            PlayBlip();
        }

        // "А: Ты тут войну собирался переживать?" looking at kitchen supplies
        if (timeSinceLevelLoad >= 7f && timeSinceLevelLoad < 9f && !Contains(subtitlesDisplayed, "e4"))
        {
            subtitleManager.ShowSubtitle("А: Ты тут войну собирался переживать?");
            AddIfNotPresent(subtitlesDisplayed, "e4");
            PlayBlip();
        }

        // "А: О, мы все здесь. Надо забрать." looking at family photo
        if (timeSinceLevelLoad >= 9f && timeSinceLevelLoad < 11f && !Contains(subtitlesDisplayed, "e5"))
        {
            subtitleManager.ShowSubtitle("А: О, мы все здесь. Надо забрать.");
            AddIfNotPresent(subtitlesDisplayed, "e5");
            PlayBlip();
        }
    }

    private void PlayBlip()
    {
        if (blipSound != null)
            AudioSource.PlayClipAtPoint(blipSound, Camera.main.transform.position, 0.7f);
    }

    private bool Contains(HashSet<string> set, string key) => set.Contains(key);
    private void AddIfNotPresent(HashSet<string> set, string key) { if (!set.Contains(key)) set.Add(key); }
}