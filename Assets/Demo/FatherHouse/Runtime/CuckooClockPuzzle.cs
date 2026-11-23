using System.Collections;
using UnityEngine;

namespace Hakaton.Demo.FatherHouse
{
    [DefaultExecutionOrder(100)]
    public sealed class CuckooClockPuzzle : DemoInteractable
    {
        public const string PuzzleId = "clock_key";
        public const string StudyKeyItem = "father_study_key";
        public QuestPickup key;
        public Transform keyDropPoint;
        public float fallDuration = 0.55f;
        DemoGame game;
        Vector3 keyStart;
        bool releasing;

        public override string Prompt
        {
            get
            {
                if (game == null) return "Осмотреть · " + label;
                switch (game.GetPuzzleState(PuzzleId))
                {
                    case PuzzleState.Started: return "Механизм часов запущен";
                    case PuzzleState.Solved: return "Задний ящик часов открыт";
                    default: return "Покрутить стрелки · " + label;
                }
            }
        }

        Renderer clockRenderer;
        MaterialPropertyBlock propBlock;
        Color originalColor;

        public override void Highlight(bool on)
        {
            if (clockRenderer == null || propBlock == null) return;
            propBlock.SetColor("_BaseColor", on ? Color.yellow : originalColor);
            clockRenderer.SetPropertyBlock(propBlock);
        }

        void Start()
        {
            if (game == null) game = FindAnyObjectByType<DemoGame>();
            if (key != null) keyStart = key.transform.position;
            clockRenderer = GetComponentInChildren<Renderer>();
            if (clockRenderer != null)
            {
                propBlock = new MaterialPropertyBlock();
                clockRenderer.GetPropertyBlock(propBlock);
                originalColor = propBlock.GetColor("_BaseColor");
                if (originalColor == Color.black) originalColor = clockRenderer.material.color;
            }
            RestoreState();
        }

        void Update()
        {
            if (game == null || game.player == null) return;
            if (game.GetPuzzleState(PuzzleId) == PuzzleState.Undiscovered && game.player.Focused == this)
                game.SetPuzzleState(PuzzleId, PuzzleState.Available);
        }

        public override void Interact(DemoGame owner)
        {
            game = owner;
            PuzzleState state = game.GetPuzzleState(PuzzleId);
            if (state == PuzzleState.Started || releasing)
            {
                game.hud.Notify("Задний ящик уже открывается.");
                return;
            }
            if (state == PuzzleState.Solved)
            {
                game.hud.Notify(game.HasItem(StudyKeyItem) ? "Задний ящик пуст. Ключ уже у вас." : "Задний ящик открыт. Ключ лежит у часов.");
                return;
            }
            if (key == null || keyDropPoint == null)
            {
                game.hud.Notify("Механизм часов заело. Здесь пока ничего не получить.");
                return;
            }
            // Close-up of the face: the back panel opens only when both hands point at 6.
            game.hud.ShowClockPuzzle(OnFaceSolved);
        }

        void OnFaceSolved()
        {
            game.SetPuzzleState(PuzzleId, PuzzleState.Started);
            StartCoroutine(ReleaseKey());
        }

        IEnumerator ReleaseKey()
        {
            if (key == null || keyDropPoint == null)
            {
                game.hud.Notify("Механизм часов заело. Здесь пока ничего не получить.");
                yield break;
            }
            releasing = true;
            game.sound.Creak(transform.position);
            game.hud.Notify("Сзади часов открылся небольшой ящик.");
            keyStart = key.transform.position;
            key.gameObject.SetActive(true);
            float elapsed = 0;
            while (elapsed < fallDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fallDuration);
                key.transform.position = Vector3.Lerp(keyStart, keyDropPoint.position, t * t);
                key.transform.Rotate(0, 120 * Time.unscaledDeltaTime, 0, Space.World);
                yield return null;
            }
            key.transform.position = keyDropPoint.position;
            releasing = false;
            game.SetPuzzleState(PuzzleId, PuzzleState.Solved);
            game.sound.Knock(keyDropPoint.position);
            game.hud.Notify("Из часов выпал ключ. Его можно подобрать.", 4);
        }

        void RestoreState()
        {
            if (key == null) return;
            PuzzleState state = game.GetPuzzleState(PuzzleId);
            if (state == PuzzleState.Started) game.SetPuzzleState(PuzzleId, PuzzleState.Solved);
            bool released = state == PuzzleState.Started || state == PuzzleState.Solved;
            key.gameObject.SetActive(released && !game.HasItem(StudyKeyItem));
            if (released && keyDropPoint != null) key.transform.position = keyDropPoint.position;
        }
    }
}
