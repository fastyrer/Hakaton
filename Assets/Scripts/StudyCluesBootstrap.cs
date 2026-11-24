using Hakaton.Demo;
using Hakaton.Demo.FatherHouse;
using UnityEngine;
using UnityEngine.SceneManagement;

// Spawns the study clue props (bottle, floor marks, entry monolog, safe monolog)
// when the father's house loads, so the scene file itself stays untouched.
public static class StudyCluesBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "FatherHouse") return;
        var root = new GameObject("Улики кабинета");
        SceneManager.MoveGameObjectToScene(root, scene);
        var setup = root.AddComponent<StudyCluesSetup>();
        setup.Build();
    }
}

public sealed class StudyCluesSetup : MonoBehaviour
{
    static readonly Color markDark = new Color(0.045f, 0.04f, 0.035f);

    public void Build()
    {
        CreateFloorMarks();
        CreateStudyEntryMonolog();
        CreateSafeMonolog();
    }

    Material PropMaterial(Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        material.name = "StudyClue_" + color;
        return material;
    }

    void CreateFloorMarks()
    {
        var marks = new GameObject("Следы на полу");
        marks.transform.SetParent(transform, false);
        marks.transform.SetPositionAndRotation(new Vector3(-4.4f, 0.012f, 14.6f), Quaternion.Euler(0, 12, 0));
        var collider = marks.AddComponent<BoxCollider>();
        collider.center = new Vector3(0, 0.02f, 0);
        collider.size = new Vector3(1.15f, 0.05f, 0.8f);
        var material = PropMaterial(markDark);
        for (int i = 0; i < 2; i++)
        {
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "Полоса следа " + (i + 1);
            strip.transform.SetParent(marks.transform, false);
            strip.transform.localPosition = new Vector3(0, 0.005f, -0.22f + i * 0.44f);
            strip.transform.localScale = new Vector3(0.85f, 0.008f, 0.07f);
            strip.transform.localRotation = Quaternion.Euler(0, i == 0 ? 4 : -5, 0);
            strip.GetComponent<Renderer>().sharedMaterial = material;
            var stripCollider = strip.GetComponent<Collider>();
            if (stripCollider != null) Destroy(stripCollider);
        }
        var clue = marks.AddComponent<HighlightableEvidence>();
        clue.evidenceId = 7;
        clue.label = "Следы на полу · осмотреть";
    }

    void CreateStudyEntryMonolog()
    {
        var trigger = new GameObject("Вход в кабинет · монолог");
        trigger.transform.SetParent(transform, false);
        trigger.AddComponent<SubtitleSequence>()
            .AsZone(new Vector3(-5.2f, 0f, 14.2f), 2.2f)
            .WithGuard("study_first_enter")
            .WithLines(
                new SubtitleSequence.Line { time = 0f, text = "Так вот что ты здесь чинил." },
                new SubtitleSequence.Line { time = 3f, text = "Ну наконец-то." },
                new SubtitleSequence.Line { time = 5.5f, text = "Что за запах? Отец явно борщил с духами." },
                new SubtitleSequence.Line { time = 9.5f, text = "Ладно. Посмотрим, что ты тут насобирал." });
    }

    void CreateSafeMonolog()
    {
        var trigger = new GameObject("Сейф · монолог");
        trigger.transform.SetParent(transform, false);
        trigger.AddComponent<SubtitleSequence>()
            .AsEvent(StudySafePuzzle.SchemeMonologEvent)
            .WithLines(
                new SubtitleSequence.Line { time = 0f, text = "Четыре штуки…" },
                new SubtitleSequence.Line { time = 3f, text = "У меня такая была. Где-то дома валяется." });
    }
}
