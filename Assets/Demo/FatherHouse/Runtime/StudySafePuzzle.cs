using System.Collections.Generic;
using Hakaton.Demo;
using UnityEngine;

namespace Hakaton.Demo.FatherHouse
{
    // Study safe with the four-figurine mechanism. First look pins the scheme to the
    // detective board, afterwards interacting opens the figurine placement menu.
    public sealed class StudySafePuzzle : DemoInteractable
    {
        public const string SchemeFlag = "safe_scheme_pinned";
        public const string SolvedFlag = "safe_figurines_solved";
        public const string SchemeMonologEvent = "study_safe_scheme";
        public const int SchemeEvidenceId = 8;
        public const string BasementKeyItem = "basement_key";
        public static readonly string[] FigurineItems = { "figurine_ivan", "figurine_dmitry", "figurine_alexey", "figurine_father" };
        public static readonly string[] FigurineNames = { "Иван", "Дмитрий", "Алексей", "Отец" };

        public GameObject figurinePrefab;
        public Transform[] slots = new Transform[4];

        Renderer safeRenderer;
        MaterialPropertyBlock propBlock;
        Color originalColor;
        GameObject keySpawned;

        public int PlacedCount => InvestigationTravel.PlacedFigurines.Count;
        public bool Solved => PlacedCount >= FigurineItems.Length;

        void Awake()
        {
            kind = Interaction.Custom;
            label = "Сейф · осмотреть";
            safeRenderer = GetComponentInChildren<Renderer>();
            if (safeRenderer != null)
            {
                propBlock = new MaterialPropertyBlock();
                safeRenderer.GetPropertyBlock(propBlock);
                originalColor = propBlock.GetColor("_BaseColor");
                if (originalColor == Color.black) originalColor = safeRenderer.sharedMaterial != null ? safeRenderer.sharedMaterial.color : Color.white;
            }
            EnsureSlots();
        }

        void Start()
        {
            for (int i = 0; i < FigurineItems.Length; i++)
                if (InvestigationTravel.PlacedFigurines.Contains(FigurineItems[i]))
                    SpawnFigurine(i);
            if (Solved) label = "Сейф · механизм сработал";
            // The house may reload after the mechanism was solved but before the key was
            // picked up; wait until DemoGame.Start has restored the inventory, then check.
            StartCoroutine(RestoreKeyNextFrames());
        }

        System.Collections.IEnumerator RestoreKeyNextFrames()
        {
            yield return null;
            yield return null;
            var game = FindAnyObjectByType<DemoGame>();
            if (game != null) EnsureKeySpawned(game);
        }

        void EnsureSlots()
        {
            if (slots != null && slots.Length == FigurineItems.Length && slots[0] != null) return;
            var made = new List<Transform>(FigurineItems.Length);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.09f, 0.1f, 0.09f) };
            for (int i = 0; i < FigurineItems.Length; i++)
            {
                var slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slot.name = "Место для фигурки " + (i + 1);
                var collider = slot.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                slot.transform.SetParent(transform, false);
                slot.transform.localPosition = new Vector3(-0.27f + i * 0.18f, 1.115f, 0f);
                slot.transform.localScale = new Vector3(0.13f, 0.015f, 0.13f);
                slot.GetComponent<Renderer>().sharedMaterial = material;
                made.Add(slot.transform);
            }
            slots = made.ToArray();
        }

        public override void Highlight(bool on)
        {
            if (safeRenderer == null || propBlock == null) return;
            propBlock.SetColor("_BaseColor", on ? Color.yellow : originalColor);
            safeRenderer.SetPropertyBlock(propBlock);
        }

        public override void Interact(DemoGame game)
        {
            InvestigationTravel.SafeDiscovered = true;
            if (game.GetPuzzleState(SchemeFlag) != PuzzleState.Solved)
            {
                game.SetPuzzleState(SchemeFlag, PuzzleState.Solved);
                game.Found.Add(SchemeEvidenceId);
                SubtitleSequence.Fire(SchemeMonologEvent);
                if (game.board.AddEvidence(SchemeEvidenceId, out var entry))
                    game.hud.Notify("Схема механизма с фигурками прикреплена к доске детектива.", 5f);
                label = "Сейф · фигурки";
                game.hud.ShowBoard();
                return;
            }
            game.hud.ShowSafeFigurines(this);
        }

        public List<int> AvailableFigurines(DemoGame game)
        {
            var result = new List<int>();
            for (int i = 0; i < FigurineItems.Length; i++)
                if (game.HasItem(FigurineItems[i]) && !InvestigationTravel.PlacedFigurines.Contains(FigurineItems[i]))
                    result.Add(i);
            return result;
        }

        public void Place(DemoGame game, int index)
        {
            if (game == null || index < 0 || index >= FigurineItems.Length) return;
            string item = FigurineItems[index];
            if (InvestigationTravel.PlacedFigurines.Contains(item) || !game.HasItem(item)) return;
            InvestigationTravel.PlacedFigurines.Add(item);
            SpawnFigurine(index);
            game.sound.Creak(slots[index].position);
            if (Solved)
            {
                game.SetPuzzleState(SolvedFlag, PuzzleState.Solved);
                label = "Сейф · механизм сработал";
                game.hud.Notify("Механизм сработал.", 5f);
                EnsureKeySpawned(game);
            }
            else game.hud.Notify("Фигурка встала на место.", 3.5f);
        }

        // The solved mechanism drops the basement key like the cuckoo clock does.
        void EnsureKeySpawned(DemoGame game)
        {
            if (keySpawned != null || game.HasItem(BasementKeyItem)) return;
            keySpawned = CreateBasementKey();
        }

        GameObject CreateBasementKey()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.7f, 0.5f, 0.3f) };
            var key = new GameObject("Ключ от подвала", typeof(BoxCollider), typeof(QuestPickup));
            key.transform.SetParent(transform, false);
            key.transform.position = transform.position + new Vector3(-0.75f, 0.16f, 0f);
            Box("Стержень ключа", key.transform, new Vector3(0.05f, 0, 0), new Vector3(0.26f, 0.03f, 0.03f), material);
            Box("Бородка ключа", key.transform, new Vector3(0.16f, 0, 0.045f), new Vector3(0.05f, 0.03f, 0.1f), material);
            Box("Головка ключа", key.transform, new Vector3(-0.09f, 0, 0), new Vector3(0.07f, 0.03f, 0.11f), material);
            var collider = key.GetComponent<BoxCollider>();
            collider.size = new Vector3(0.42f, 0.12f, 0.28f);
            var pickup = key.GetComponent<QuestPickup>();
            pickup.kind = Interaction.Custom;
            pickup.evidenceId = -1;
            pickup.label = "Ключ от подвала";
            pickup.itemId = BasementKeyItem;
            pickup.successMessage = "Получен ключ от подвала.";
            return key;
        }

        static void Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            var collider = result.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }

        void SpawnFigurine(int index)
        {
            if (figurinePrefab == null || slots[index] == null) return;
            var figurine = Instantiate(figurinePrefab, slots[index]);
            figurine.transform.localPosition = Vector3.zero;
            figurine.transform.localRotation = Quaternion.identity;
        }
    }
}
