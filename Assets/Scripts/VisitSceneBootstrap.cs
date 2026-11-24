using System.Collections;
using Hakaton.Demo;
using Hakaton.Demo.FatherHouse;
using UnityEngine;
using UnityEngine.SceneManagement;

// Visit scenes (Ivan / Dmitry): no DemoGame lives in these scenes, so the player cannot
// walk. This bootstrap wires a minimal DemoGame, plays the visit dialog as subtitles
// and hands out the safe figurine.
public static class VisitSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "IvanHouse" && scene.name != "DmitryHouse" && scene.name != "AlexeyHouse") return;
        var root = new GameObject("Визит · сценарий");
        SceneManager.MoveGameObjectToScene(root, scene);
        VisitScene.NextScene = scene.name;
        root.AddComponent<VisitScene>();
    }
}

public sealed class VisitScene : MonoBehaviour
{
    // Set by the bootstrap before AddComponent: Awake runs before the bootstrap can
    // assign an instance field.
    public static string NextScene;
    string sceneName;

    static readonly string[] IvanLines =
    {
        "И: Проходи. Разулся бы только, я недавно помыл.",
        "А: Я ненадолго. Ключ нашёл? — В часах. — Серьёзно? — Да. Смотри, что ещё нашёл.",
        "И: Это у него в кабинете было? — Угу. У тебя такая фигурка есть? — Есть. На полке стоит.",
        "А: Можно посмотреть? — Бери. Только не урони.",
        "А: Снизу такая же отметка. Похоже, часть замка. Их четыре нужно.",
        "И: Он и из этого замок сделал? Мне он её просто отдал. Я думал, у вас такие же.",
        "А: У меня была. У Дмитрия, наверное, тоже есть. — Мою забирай.",
        "А: Так просто? — А что, я должен загадку тебе придумать?",
        "А: Ты ведь не считаешь, что там всё нормально было? В его смерти.",
        "И: Я не знаю, Лёш. Я не видел, что произошло. Мне показали заключение. Что я должен был сделать?",
        "И: Ты думаешь, я пришёл, кивнул и забыл? Я спрашивал. Мне ответили то же самое.",
        "А: У него дома запасы еды, камеры, проходы за стенами. Это тоже нормально?",
        "И: Он боялся людей. Потом начал бояться, что они знают, где он прячет вещи.",
        "А: Может, было чего бояться. — Возможно. Но он и нас проверял. Ты забыл?",
    };

    static readonly string[] DmitryLines =
    {
        "Д: О. Пропавший брат. — Привет и тебе.",
        "Д: Заходи. Только быстро, я собирался уходить. — Мне про отца спросить надо.",
        "Д: Ну конечно. А просто так ты зайти не мог. — Дим, давай без этого.",
        "А: У тебя фигурка отсюда есть? — Есть. — Мне нужна. У отца замок, этими штуками открывается.",
        "Д: А сразу дверь выбить нельзя? — Я не хочу там всё разнести. — Он бы твою выбил.",
        "Д: Так отдашь? — Сейчас найду. Вот. Держи. — Спасибо. — Только верни потом.",
        "А: Вы сильно ругались перед его смертью? — Мы всегда ругались.",
        "Д: Он мог позвонить и полчаса рассказывать, какой я идиот. Потом просил приехать помочь.",
        "А: Почему приезжал? — Потому что отец. Сам не знаешь?",
    };

    void Awake()
    {
        sceneName = NextScene;
        bool ivan = sceneName == "IvanHouse";
        bool alexey = sceneName == "AlexeyHouse";
        var player = FindAnyObjectByType<DemoPlayer>();
        var hud = FindAnyObjectByType<DemoHud>();
        var rig = new GameObject("Игра");
        rig.transform.SetParent(transform, false);
        var game = rig.AddComponent<DemoGame>();
        game.player = player;
        game.hud = hud;
        game.sound = rig.AddComponent<DemoSound>();
        game.locationTitle = ivan ? "ДОМ ИВАНА" : alexey ? "КВАРТИРА АЛЕКСЕЯ" : "ДОМ ДМИТРИЯ";
        game.locationIntroduction = ivan
            ? "Иван живёт в тихом районе. Он старше и всегда держался особняком.\n\nНа полке у него должна стоять фигурка из отцовского набора."
            : alexey
            ? "Своя квартира. Здесь точно валяется вторая фигурка из детского набора.\n\nСтоит осмотреть стол."
            : "Дмитрий вечно куда-то спешит и почти не звонит.\n\nЕсли фигурка и есть, то где-то в его шкафу.";
        game.locationObjective = ivan ? "Поговорить с Иваном" : alexey ? "Найти свою фигурку" : "Поговорить с Дмитрием";
        if (player != null) player.game = game;
    }

    void Start()
    {
        SetupExitDoor();
        if (sceneName == "AlexeyHouse") return;
        var lines = sceneName == "IvanHouse" ? IvanLines : DmitryLines;
        var sequence = gameObject.AddComponent<SubtitleSequence>();
        sequence.WithLines(BuildLines(lines));
        sequence.Play();
        StartCoroutine(GrantFigurine(lines));
    }

    static SubtitleSequence.Line[] BuildLines(string[] texts)
    {
        var lines = new SubtitleSequence.Line[texts.Length];
        float time = 0f;
        for (int i = 0; i < texts.Length; i++)
        {
            lines[i] = new SubtitleSequence.Line { time = time, text = texts[i] };
            time += Mathf.Clamp(1.2f + texts[i].Length * 0.055f, 2.4f, 9f);
        }
        return lines;
    }

    IEnumerator GrantFigurine(string[] lines)
    {
        float total = 0f;
        foreach (string text in lines) total += Mathf.Clamp(1.2f + text.Length * 0.055f, 2.4f, 9f);
        yield return new WaitForSecondsRealtime(total + 1f);
        var game = GetComponentInChildren<DemoGame>();
        if (game == null) yield break;
        string item = sceneName == "IvanHouse" ? StudySafePuzzle.FigurineItems[0] : StudySafePuzzle.FigurineItems[1];
        string name = sceneName == "IvanHouse" ? StudySafePuzzle.FigurineNames[0] : StudySafePuzzle.FigurineNames[1];
        if (game.GrantItem(item)) game.hud.Notify("Получена фигурка · " + name, 5f);
    }

    void SetupExitDoor()
    {
        var door = GameObject.Find("Front door");
        if (door == null) return;
        foreach (var existing in door.GetComponents<DemoInteractable>()) Destroy(existing);
        if (door.GetComponentInChildren<Collider>() == null)
        {
            var box = door.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 1.1f, 0);
            box.size = new Vector3(1.2f, 2.2f, 0.2f);
        }
        var exit = door.AddComponent<VisitExitDoor>();
        exit.label = "Вернуться к машине";
    }
}

public sealed class VisitExitDoor : DemoInteractable
{
    public override void Interact(DemoGame game)
    {
        InvestigationTravel.Capture(game);
        Time.timeScale = 1;
        SceneManager.LoadScene(DemoGame.HomeScene);
    }
}
