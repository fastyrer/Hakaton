using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hakaton.Demo.FatherHouse
{
    /// <summary>
    /// Close-up clock face for the cuckoo clock puzzle. The player drags both
    /// hands with the mouse; when both point down at 6 the back panel clicks open.
    /// Runs on unscaled time because the HUD pauses the world while it is open.
    /// </summary>
    public sealed class ClockFaceView : MonoBehaviour
    {
        const float FaceSize = 420f;
        const float Target = 180f; // 6 o'clock, straight down
        const float Tolerance = 8f;
        const float Snap = 5f;

        static readonly Color Ink = new Color(0.89f, 0.85f, 0.74f);
        static readonly Color Amber = new Color(0.76f, 0.55f, 0.29f);
        static readonly Color Solved = new Color(0.55f, 0.8f, 0.45f);

        DemoGame game;
        Action onSolved;
        RectTransform face;
        RectTransform hourHand, minuteHand;
        Image hourImage, minuteImage;
        float hourAngle = 55f, minuteAngle = 305f; // hands start away from the answer
        RectTransform dragged;
        bool solved;
        float solvedDelay;
        static Texture2D faceTexture;

        public void Initialize(DemoGame owner, Font font, Action solvedCallback)
        {
            game = owner;
            onSolved = solvedCallback;
            var rect = GetComponent<RectTransform>();

            face = new GameObject("Циферблат", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            face.SetParent(rect, false);
            face.anchorMin = face.anchorMax = new Vector2(0.5f, 0.5f);
            face.pivot = new Vector2(0.5f, 0.5f);
            face.anchoredPosition = new Vector2(0f, -10f);
            face.sizeDelta = new Vector2(FaceSize, FaceSize);
            var faceImage = face.GetComponent<Image>();
            faceImage.sprite = FaceSprite();
            faceImage.raycastTarget = false;

            AddNumeral(font, "12", 0f, 1f);
            AddNumeral(font, "3", 1f, 0f);
            AddNumeral(font, "6", 0f, -1f);
            AddNumeral(font, "9", -1f, 0f);

            hourHand = MakeHand("Часовая стрелка", 12f, 118f);
            minuteHand = MakeHand("Минутная стрелка", 7f, 172f);
            hourImage = hourHand.GetComponent<Image>();
            minuteImage = minuteHand.GetComponent<Image>();
            hourHand.localEulerAngles = new Vector3(0f, 0f, -hourAngle);
            minuteHand.localEulerAngles = new Vector3(0f, 0f, -minuteAngle);

            var cap = new GameObject("Ось", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            cap.SetParent(face, false);
            cap.anchorMin = cap.anchorMax = new Vector2(0.5f, 0.5f);
            cap.sizeDelta = new Vector2(18f, 18f);
            cap.GetComponent<Image>().color = Amber;
        }

        void AddNumeral(Font font, string text, float x, float y)
        {
            var label = new GameObject(text, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(face, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x * (FaceSize * 0.5f - 34f), y * (FaceSize * 0.5f - 34f));
            rect.sizeDelta = new Vector2(60f, 44f);
            label.font = font;
            label.text = text;
            label.fontSize = 30;
            label.color = Ink;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
        }

        RectTransform MakeHand(string name, float width, float length)
        {
            var hand = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            hand.SetParent(face, false);
            hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
            hand.pivot = new Vector2(0.5f, 0.08f); // a short tail below the axle
            hand.anchoredPosition = Vector2.zero;
            hand.sizeDelta = new Vector2(width, length);
            var image = hand.GetComponent<Image>();
            image.color = Ink;
            image.raycastTarget = false;
            return hand;
        }

        void Update()
        {
            if (solved)
            {
                solvedDelay -= Time.unscaledDeltaTime;
                if (solvedDelay <= 0f)
                {
                    solved = false;
                    var callback = onSolved;
                    onSolved = null;
                    callback?.Invoke();
                }
                return;
            }
            var mouse = Mouse.current;
            if (mouse == null || face == null) return;
            Vector2 position = mouse.position.ReadValue();
            // The HUD canvas is ScreenSpaceOverlay, so no camera is needed.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(face, position, null, out Vector2 local);
            float radius = FaceSize * 0.5f;
            if (mouse.leftButton.wasPressedThisFrame && local.magnitude <= radius)
            {
                float click = Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg;
                dragged = AngleDistance(click, hourAngle) < AngleDistance(click, minuteAngle) ? hourHand : minuteHand;
            }
            if (dragged != null && mouse.leftButton.isPressed && local.magnitude > 24f)
            {
                float angle = Mathf.Round(Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg / Snap) * Snap;
                if (dragged == hourHand) hourAngle = angle; else minuteAngle = angle;
                dragged.localEulerAngles = new Vector3(0f, 0f, -angle);
            }
            if (mouse.leftButton.wasReleasedThisFrame && dragged != null)
            {
                dragged = null;
                if (game != null && game.sound != null && game.player != null)
                    game.sound.Knock(game.player.transform.position);
            }
            if (AngleDistance(hourAngle, Target) <= Tolerance && AngleDistance(minuteAngle, Target) <= Tolerance)
            {
                solved = true;
                solvedDelay = 0.7f;
                dragged = null;
                if (hourImage != null) hourImage.color = Solved;
                if (minuteImage != null) minuteImage.color = Solved;
                if (game != null && game.sound != null && game.player != null)
                    game.sound.Creak(game.player.transform.position);
            }
        }

        static float AngleDistance(float a, float b)
        {
            float d = Mathf.Abs(Mathf.DeltaAngle(a, b));
            return d;
        }

        static Sprite FaceSprite()
        {
            if (faceTexture != null) return Sprite.Create(faceTexture, new Rect(0, 0, faceTexture.width, faceTexture.height), new Vector2(0.5f, 0.5f), 1f);
            const int size = 512;
            const float center = size / 2f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Color32 disk = new Color32(16, 20, 17, 242);
            Color32 rim = new Color32(194, 140, 74, 255);
            Color32 tick = new Color32(227, 217, 191, 255);
            Color32 clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center, dy = y - center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    Color32 c = clear;
                    if (r <= center - 1f)
                    {
                        c = disk;
                        if (r > center - 12f) c = rim;
                        else if (r > center - 62f && r < center - 34f)
                        {
                            float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                            if (angle < 0f) angle += 360f;
                            float nearest = Mathf.Round(angle / 30f) * 30f % 360f;
                            if (Mathf.Abs(Mathf.DeltaAngle(angle, nearest)) < 1.6f) c = tick;
                        }
                    }
                    pixels[y * size + x] = c;
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            faceTexture = texture;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
