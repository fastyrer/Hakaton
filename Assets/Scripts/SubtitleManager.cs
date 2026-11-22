using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple subtitle manager for displaying dialogue text at the bottom of the screen.
/// Fades in/out smoothly and plays a text blip sound.
/// </summary>
public class SubtitleManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The UI Text component to use for subtitles")]
    [SerializeField] private Text subtitleText;

    [Tooltip("Time in seconds for subtitle to fade in")]
    [SerializeField] private float fadeInTime = 0.5f;

    [Tooltip("Time in seconds subtitle stays fully visible")]
    [SerializeField] private float stayTime = 2.0f;

    [Tooltip("Time in seconds for subtitle to fade out")]
    [SerializeField] private float fadeOutTime = 0.5f;

    [Tooltip("Volume of the text blip sound (0-1)")]
    [SerializeField] private float blipVolume = 0.7f;

    [Tooltip("Audio clip for text blip sound")]
    [SerializeField] private AudioClip blipSound;

    [Tooltip("Whether to show subtitles (default true)")]
    [SerializeField] private bool enableSubtitles = true;

    // Internal state
    private CanvasGroup canvasGroup;
    private AudioSource audioSource;
    private Coroutine currentCoroutine;

    private void Awake()
    {
        // Ensure we have a CanvasGroup for fading
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Ensure we have an AudioSource for blip sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Hide subtitle initially
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (subtitleText != null)
            subtitleText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Display a subtitle line with fade in/out effect and blip sound.
    /// </summary>
    /// <param text="text">The subtitle text to display (without parentheses markers)</param>
    /// <param duration">Total display time in seconds (defaults to stayTime + fadeInTime + fadeOutTime)</param>
    public void ShowSubtitle(string text, float duration = 0f)
    {
        if (!enableSubtitles || subtitleText == null)
            return;

        // Don't display text in parentheses - strip them out
        string cleanText = RemoveParentheses(text);

        // Set the text and activate
        subtitleText.text = cleanText;
        subtitleText.gameObject.SetActive(true);

        // Reset coroutine if one is running
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        // Start the fade coroutine
        currentCoroutine = StartCoroutine(FadeRoutine(duration));
    }

    /// <summary>
    /// Remove text within parentheses and the parentheses themselves.
    /// </summary>
    private string RemoveParentheses(string text)
    {
        string result = text;
        int startIdx = result.IndexOf('(');
        while (startIdx >= 0)
        {
            int endIdx = result.IndexOf(')', startIdx);
            if (endIdx >= 0)
            {
                result = result.Remove(startIdx, endIdx - startIdx + 1);
            }
            else
            {
                result = result.Remove(startIdx);
            }
            startIdx = result.IndexOf('(');
        }
        return result;
    }

    /// <summary>
    /// Coroutine that handles the fade in/out sequence.
    /// </summary>
    private System.Collections.IEnumerator FadeRoutine(float totalDuration)
    {
        // Calculate effective duration
        float effectiveStay = stayTime;
        float effectiveTotal = fadeInTime + effectiveStay + fadeOutTime;

        if (totalDuration > 0f)
        {
            effectiveTotal = totalDuration;
            // Distribute time proportionally
            float ratio = effectiveStay / (effectiveTotal - fadeInTime - fadeOutTime);
            effectiveStay = ratio * (effectiveTotal - fadeInTime - fadeOutTime);
        }

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInTime);
            if (canvasGroup != null)
                canvasGroup.alpha = t;
            if (subtitleText != null)
                subtitleText.color = new Color(
                    subtitleText.color.r,
                    subtitleText.color.g,
                    subtitleText.color.b,
                    t);
            yield return null;
        }
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        if (subtitleText != null)
            subtitleText.color = new Color(
                subtitleText.color.r,
                subtitleText.color.g,
                subtitleText.color.b,
                1f);

        // Stay visible
        elapsed = 0f;
        while (elapsed < effectiveStay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutTime);
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;
            if (subtitleText != null)
                subtitleText.color = new Color(
                    subtitleText.color.r,
                    subtitleText.color.g,
                    subtitleText.color.b,
                    1f - t);
            yield return null;
        }
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (subtitleText != null)
            subtitleText.color = new Color(
                subtitleText.color.r,
                subtitleText.color.g,
                subtitleText.color.b,
                0f);

        // Deactivate the text game object
        if (subtitleText != null)
            subtitleText.gameObject.SetActive(false);

        currentCoroutine = null;
    }

    /// <summary>
    /// Quick method to show a subtitle with default timing.
    /// </summary>
    public void ShowQuickSubtitle(string text)
    {
        if (!enableSubtitles || subtitleText == null)
            return;

        // Don't display text in parentheses - strip them out
        string cleanText = RemoveParentheses(text);

        // Set the text and activate
        subtitleText.text = cleanText;
        subtitleText.gameObject.SetActive(true);

        // Reset coroutine if one is running
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        // Start the fade coroutine with default duration
        currentCoroutine = StartCoroutine(FadeRoutine(0f));
    }
}