using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FatherHouseSubtitleTrigger : MonoBehaviour
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

        // Trigger when arriving at father's house
        // "Звонок напарника" when scene loads (around 0-1s)
        if (timeSinceLevelLoad >= 0f && timeSinceLevelLoad < 2f && !Contains(subtitlesDisplayed, "call"))
        {
            subtitleManager.ShowSubtitle("(Звонок напарника)");
            // Note: The parentheses are part of the context, but ShowSubtitle strips them.
            // We need to handle this differently - just show the content without parens
            AddIfNotPresent(subtitlesDisplayed, "call");
            PlayBlip();
        }

        // "Н: Привет, Лёх..." - first dialogue after call
        if (timeSinceLevelLoad >= 1f && timeSinceLevelLoad < 3f && !Contains(subtitlesDisplayed, "n1"))
        {
            subtitleManager.ShowSubtitle("Н: Привет, Лёх. Слышал, тебе наконец отпуск согласовали. Уже выбрал, куда смотаешься?");
            AddIfNotPresent(subtitlesDisplayed, "n1");
            PlayBlip();
        }

        // "А: Привет." response
        if (timeSinceLevelLoad >= 3f && timeSinceLevelLoad < 5f && !Contains(subtitlesDisplayed, "n2"))
        {
            subtitleManager.ShowSubtitle("А: Привет. Да вот к отцу заехал наконец. Надо разобрать хлам.");
            AddIfNotPresent(subtitlesDisplayed, "n2");
            PlayBlip();
        }

        // "Н: В смысле?" question
        if (timeSinceLevelLoad >= 5f && timeSinceLevelLoad < 7f && !Contains(subtitlesDisplayed, "n3"))
        {
            subtitleManager.ShowSubtitle("Н: В смысле? Ты опять поехал к нему?");
            AddIfNotPresent(subtitlesDisplayed, "n3");
            PlayBlip();
        }

        // "А: В дом. Не на кладбице." answer
        if (timeSinceLevelLoad >= 7f && timeSinceLevelLoad < 9f && !Contains(subtitlesDisplayed, "n4"))
        {
            subtitleManager.ShowSubtitle("А: В дом. Не на кладбище.");
            AddIfNotPresent(subtitlesDisplayed, "n4");
            PlayBlip();
        }

        // "Н: Да я понял." acknowledgement
        if (timeSinceLevelLoad >= 9f && timeSinceLevelLoad < 11f && !Contains(subtitlesDisplayed, "n5"))
        {
            subtitleManager.ShowSubtitle("Н: Да я понял.");
            AddIfNotPresent(subtitlesDisplayed, "n5");
            PlayBlip();
        }

        // "А: Ну а чего ты так?" question
        if (timeSinceLevelLoad >= 11f && timeSinceLevelLoad < 13f && !Contains(subtitlesDisplayed, "n6"))
        {
            subtitleManager.ShowSubtitle("А: Ну а чего ты так?");
            AddIfNotPresent(subtitlesDisplayed, "n6");
            PlayBlip();
        }

        // "Н: Ничего. Ладно, не моё дела." answer
        if (timeSinceLevelLoad >= 13f && timeSinceLevelLoad < 15f && !Contains(subtitlesDisplayed, "n7"))
        {
            subtitleManager.ShowSubtitle("Н: Ничего. Ладно, не моё дела. Как настроение будет, набери. Может, в бар сходим.");
            AddIfNotPresent(subtitlesDisplayed, "n7");
            PlayBlip();
        }

        // "А: Посмотрим." response
        if (timeSinceLevelLoad >= 15f && timeSinceLevelLoad < 17f && !Contains(subtitlesDisplayed, "n8"))
        {
            subtitleManager.ShowSubtitle("А: Посмотрим.");
            AddIfNotPresent(subtitlesDisplayed, "n8");
            PlayBlip();
        }

        // "Н: Только телефон не выключай." warning
        if (timeSinceLevelLoad >= 17f && timeSinceLevelLoad < 19f && !Contains(subtitlesDisplayed, "n9"))
        {
            subtitleManager.ShowSubtitle("Н: Только телефон не выключай.");
            AddIfNotPresent(subtitlesDisplayed, "n9");
            PlayBlip();
        }

        // "А: Да куда я денусь." response
        if (timeSinceLevelLoad >= 19f && timeSinceLevelLoad < 21f && !Contains(subtitlesDisplayed, "n10"))
        {
            subtitleManager.ShowSubtitle("А: Да куда я денусь.");
            AddIfNotPresent(subtitlesDisplayed, "n10");
            PlayBlip();
        }

        // "Конец разговора." - end of call
        if (timeSinceLevelLoad >= 21f && timeSinceLevelLoad < 23f && !Contains(subtitlesDisplayed, "endcall"))
        {
            subtitleManager.ShowSubtitle("Конец разговора.");
            AddIfNotPresent(subtitlesDisplayed, "endcall");
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