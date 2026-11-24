using Hakaton.Demo;
using UnityEngine;
using UnityEngine.SceneManagement;

// Полицейский участок: стол напарника и похожее дело (ТЗ, сцены 14–17).
// Порядок: E05+E06 на доске → D01 → показать документы напарнику (сцена 16) →
// в архиве появляется похожее дело E07 → после изучения играет сцена 17.
public static class PoliceStationStory
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "PoliceStation") return;
        var root = new GameObject("Сюжет участка · напарник");
        SceneManager.MoveGameObjectToScene(root, scene);

        // Стол напарника в обжитых рабочих местах: стопка материалов (-7.5, 0.9, 22.1).
        var desk = Prop(root.transform, "Стол напарника", new Vector3(-7.6f, 0.92f, 22.4f),
            new Vector3(0.3f, 0.06f, 0.22f), new Color(0.85f, 0.83f, 0.7f));
        var partner = desk.AddComponent<PartnerDesk>();
        partner.label = "Стол напарника · показать документы";

        // Похожее дело лежит в архиве (стол в глубине архива, 16.2, 0.9, 27.4).
        var similar = Prop(root.transform, "Папка · похожее дело К-93/008",
            new Vector3(16.2f, 1.02f, 27.4f), new Vector3(0.34f, 0.05f, 0.26f), new Color(0.6f, 0.55f, 0.4f));
        var clue = similar.AddComponent<ClueEvidence>();
        clue.evidenceId = 9;
        clue.label = "Папка с грифом К-93/008 · осмотреть";
        clue.gateEvent = EvidenceCatalog.EvPartnerCaseShown;
        clue.gateMessage = "Папка подшита в общий массив. Сначала стоит поговорить с напарником.";
        clue.monolog = PartnerDesk.Scene17;
    }

    static GameObject Prop(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prop.name = name;
        prop.transform.SetParent(parent, false);
        prop.transform.position = position;
        prop.transform.localScale = scale;
        prop.transform.localRotation = Quaternion.Euler(0, -8, 0);
        prop.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        return prop;
    }
}

/// <summary>
/// Стол напарника: принимает вывод D01 (несовпадение калибров), играет сцену 16,
/// после неё напарник выписывает похожее дело (E07) в архив.
/// </summary>
public sealed class PartnerDesk : DemoInteractable
{
    // Сцена 16 «Напарник. Разные калибры» (Диалоги_Дом_помнит.docx).
    public static readonly string[] Scene16 =
    {
        "А: Посмотри.",
        "Н: Что?",
        "А: Пистолет рядом с телом — сорок пятый. Пуля — девятка.",
        "Н: Где ты это взял?",
        "А: В архиве. В одной папке лежит.",
        "Н: Вижу.",
        "А: И?",
        "Н: Надо оригинал заключения поднять.",
        "А: Ты думаешь, перепутали?",
        "Н: Я думаю, сначала надо проверить.",
        "А: А тогда почему не проверили?",
        "Н: Хороший вопрос.",
        "Н: Слушай, я похожее уже видел.",
        "А: Что именно?",
        "Н: Человек погиб, оружие нашли, всё быстро закрыли. А потом в бумагах началась каша.",
        "А: Какое дело?",
        "Н: Подожди здесь. Найду номер.",
    };

    // Сцена 17 «После изучения похожего дела» — запускается после подбора E07
    // независимо от того, успел ли игрок соединить материалы на доске (ТЗ).
    public static readonly string[] Scene17 =
    {
        "А: Здесь тоже пишут, что человек ничего не помнил.",
        "Н: Так он сказал.",
        "А: У него причина убивать была?",
        "Н: Мы не нашли.",
        "А: А дело закрыли.",
        "Н: Да.",
        "А: Тебя это не смущает?",
        "Н: Смущает. Поэтому я тебе его и показал.",
        "А: Кто их закрывал?",
        "Н: Я проверю.",
        "А: И мне потом расскажешь?",
        "Н: Если ты перестанешь вытягивать из меня обещания каждые полминуты.",
        "Н: Лёх, только сам ни к кому не ломись. Договорились?",
        "А: Я пока домой.",
        "Н: К себе или к отцу?",
        "А: Сначала к себе.",
    };

    void Awake()
    {
        kind = Interaction.Custom;
    }

    public override string Prompt => label;

    public override void Interact(DemoGame game)
    {
        if (InvestigationTravel.GetEventState(EvidenceCatalog.EvCaliberMismatch) != StoryEventState.Triggered)
        {
            game.hud.Notify("Напарник занят. Пока нечего ему показать — сначала дела в архиве.", 5f);
            return;
        }
        if (InvestigationTravel.GetEventState(EvidenceCatalog.EvPartnerCaseShown) != StoryEventState.Triggered)
        {
            // Прогресс фиксируем до открытия окна: ранний Esc не должен блокировать E07.
            InvestigationTravel.SetEventState(EvidenceCatalog.EvPartnerCaseShown, StoryEventState.Triggered);
            game.hud.ShowLineSequence("УЧАСТОК / НАПАРНИК", "Разные калибры", Scene16, false,
                () => game.hud.Notify("Напарник выписал похожее дело. Оно лежит в архиве.", 6f));
            return;
        }
        game.hud.Notify("Н: Я проверю, кто закрывал эти дела. Без обещаний, Лёх.", 5f);
    }
}
