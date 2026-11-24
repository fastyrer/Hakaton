using Hakaton.Demo;
using UnityEngine;
using UnityEngine.SceneManagement;

// Сюжетные улики дедукции (ТЗ, реестр E01–E13): гильза в доме Алексея (E08),
// документ 2000 года и материалы программы в скрытом подвальном хранилище (E10, E11),
// записи камер в тайной комнате наблюдения (E12). Спавнятся при загрузке сцены,
// файл сцен не трогаем. Доступ опирается на уже существующие двери и тайники.
public static class StoryEvidenceClues
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "AlexeyHouse") SpawnAlexeyHouse(scene);
        else if (scene.name == "FatherHouse") SpawnFatherHouse(scene);
    }

    static void SpawnAlexeyHouse(Scene scene)
    {
        var root = new GameObject("Сюжетные улики · гильза");
        SceneManager.MoveGameObjectToScene(root, scene);
        // Старая куртка на полу гостиной, рядом со столом, где лежат фотография и фигурка.
        var jacket = Prop(root.transform, "Старая куртка", new Vector3(-4.6f, 0.14f, -1.8f),
            new Vector3(0.55f, 0.12f, 0.42f), new Color(0.16f, 0.17f, 0.2f));
        var clue = jacket.AddComponent<ClueEvidence>();
        clue.evidenceId = 10;
        clue.label = "Старая куртка · осмотреть";
        clue.monolog = new[]
        {
            "Гильза…",
            "Опять после стрельбища карманы не вытряхнул.",
        };
    }

    static void SpawnFatherHouse(Scene scene)
    {
        var root = new GameObject("Сюжетные улики · хранилище и наблюдение");
        SceneManager.MoveGameObjectToScene(root, scene);

        // Скрытое подвальное хранилище (за стеллажом в подвале): стол (10.3, -2.6, 26.5).
        var year2000 = Prop(root.transform, "Папка · выписка 2000 года",
            new Vector3(9.8f, -2.32f, 26.1f), new Vector3(0.34f, 0.05f, 0.26f), new Color(0.78f, 0.72f, 0.55f));
        var yearClue = year2000.AddComponent<ClueEvidence>();
        yearClue.evidenceId = 11;
        yearClue.label = "Папка с выпиской · осмотреть";
        yearClue.monolog = new[] { "Двухтысячный?.." };

        var program = Prop(root.transform, "Папка · материалы программы",
            new Vector3(10.8f, -2.32f, 26.9f), new Vector3(0.36f, 0.06f, 0.28f), new Color(0.55f, 0.5f, 0.38f));
        var programClue = program.AddComponent<ClueEvidence>();
        programClue.evidenceId = 12;
        programClue.label = "Подшивка с грифом · осмотреть";

        // Тайная комната наблюдения: стол у мониторов (-10.8, 0.8, 12.1).
        var tapes = Prop(root.transform, "Видеокассеты · записи камер",
            new Vector3(-10.6f, 0.95f, 12.7f), new Vector3(0.3f, 0.09f, 0.2f), new Color(0.12f, 0.12f, 0.13f));
        var tapesClue = tapes.AddComponent<ClueEvidence>();
        tapesClue.evidenceId = 13;
        tapesClue.label = "Видеокассеты · осмотреть";
        tapesClue.monolog = new[]
        {
            "Ты и за нами смотрел?",
            "Нет. Подожди. Я этого не помню.",
        };
    }

    static GameObject Prop(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prop.name = name;
        prop.transform.SetParent(parent, false);
        prop.transform.position = position;
        prop.transform.localScale = scale;
        prop.transform.localRotation = Quaternion.Euler(0, 12, 0);
        prop.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        return prop;
    }
}

/// <summary>
/// Подбираемая сюжетная улика: проверка по ID, опциональный сюжетный «замок» —
/// событие InvestigationTravel, которое должно уже произойти, и монолог после подбора.
/// </summary>
public sealed class ClueEvidence : DemoInteractable
{
    public string gateEvent;
    [TextArea] public string gateMessage = "Пока рано.";
    [TextArea] public string[] monolog;

    Renderer propRenderer;
    MaterialPropertyBlock propBlock;
    Color originalColor;

    void Awake()
    {
        kind = Interaction.Custom;
        propRenderer = GetComponentInChildren<Renderer>();
        if (propRenderer != null)
        {
            propBlock = new MaterialPropertyBlock();
            propRenderer.GetPropertyBlock(propBlock);
            originalColor = propBlock.GetColor("_BaseColor");
            if (originalColor == Color.black) originalColor = propRenderer.sharedMaterial != null ? propRenderer.sharedMaterial.color : Color.white;
        }
    }

    public override string Prompt => label;

    public override void Highlight(bool on)
    {
        if (propRenderer == null || propBlock == null) return;
        propBlock.SetColor("_BaseColor", on ? Color.yellow : originalColor);
        propRenderer.SetPropertyBlock(propBlock);
    }

    public override void Interact(DemoGame game)
    {
        if (!string.IsNullOrEmpty(gateEvent) &&
            InvestigationTravel.GetEventState(gateEvent) != StoryEventState.Triggered)
        {
            game.hud.Notify(gateMessage, 4f);
            return;
        }
        bool fresh = game.Found.Add(evidenceId);
        if (fresh)
        {
            game.sound.Creak(transform.position);
            SubtitleSequence.Fire("evidence_" + evidenceId);
        }
        game.hud.ShowEvidence(evidenceId, DemoHud.Screen.Play);
        if (fresh && monolog != null && monolog.Length > 0)
            Monolog.Then(null, monolog);
    }
}
