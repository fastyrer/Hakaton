using Hakaton.Demo;
using UnityEngine;
using UnityEngine.SceneManagement;

// Финал игры (ТЗ «Финал и две концовки»): существующая записка «Последний конверт»
// в комнате за подвальным стеллажом запускает финальный монолог и выбор концовки.
// Без новых локаций, персонажей и механик — только существующие окна HUD.
public static class FinaleStory
{
    const string NoteHeading = "Последний конверт";

    // Исходный текст записки из сцены (приоритет) + текст-раскрытие из ТЗ.
    public const string NoteBody =
        "Здесь нет оружия. Здесь то, ради чего за мной следят.\n\n" +
        "Я оставил слишком много версий одного события. Сверьте дату, оружие и заключение эксперта. " +
        "Бумага не умеет помнить, но сохраняет чужую ошибку.\n\n" +
        "Не верьте первой версии.\n\n· · ·\n\n" +
        "Если ты читаешь это, значит, всё наконец-то закончилось.\n\n" +
        "Я знал, что однажды ты вернёшься сюда. Возможно, ты захочешь найти виноватого, понять, " +
        "почему всё произошло именно так. Но ответ гораздо хуже, чем ты можешь себе представить.\n\n" +
        "Программа, над которой мы работали, никогда не предназначалась для лечения. Вещество способно " +
        "искажать восприятие человека, разрушать его способность отличать восприятие от реальности " +
        "и заставлять его совершать поступки, которые он никогда бы не совершил в здравом уме.\n\n" +
        "Я слишком поздно понял, насколько далеко всё зашло.\n\n" +
        "Всё, что произошло в тот день, было результатом эксперимента. И ты оказался его частью.\n\n" +
        "Теперь решение за тобой. Ты можешь рассказать правду или продолжить жить с ней так, как тебе будет проще.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "FatherHouse") return;
        foreach (var document in Object.FindObjectsByType<Hakaton.Demo.FatherHouse.HouseDocument>(FindObjectsInactive.Include))
        {
            if (document.heading != NoteHeading) continue;
            var target = document.gameObject;
            string label = document.label;
            Object.Destroy(document);
            var note = target.AddComponent<FinalNote>();
            note.heading = NoteHeading;
            note.body = NoteBody;
            note.label = string.IsNullOrEmpty(label) ? "Прочитать: Последний конверт" : label;
        }
    }
}

/// <summary>Записка-финал: чтение → монолог → экран выбора. После выбора — только чтение.</summary>
public sealed class FinalNote : DemoInteractable
{
    public string heading;
    [TextArea(6, 20)] public string body;

    Renderer noteRenderer;
    MaterialPropertyBlock propBlock;
    Color originalColor;

    void Awake()
    {
        kind = Interaction.Custom;
        noteRenderer = GetComponentInChildren<Renderer>();
        if (noteRenderer != null)
        {
            propBlock = new MaterialPropertyBlock();
            noteRenderer.GetPropertyBlock(propBlock);
            originalColor = propBlock.GetColor("_BaseColor");
            if (originalColor == Color.black) originalColor = noteRenderer.material.color;
        }
    }

    public override void Highlight(bool on)
    {
        if (noteRenderer == null || propBlock == null) return;
        propBlock.SetColor("_BaseColor", on ? Color.yellow : originalColor);
        noteRenderer.SetPropertyBlock(propBlock);
    }

    public override void Interact(DemoGame game)
    {
        if (InvestigationTravel.GetEventState(EvidenceCatalog.EvEndingChosen) == StoryEventState.Triggered)
        {
            game.hud.ShowDocument(heading, body);
            return;
        }
        // Прогресс фиксируем до показа: ранний Esc не должен терять финал.
        InvestigationTravel.SetEventState(EvidenceCatalog.EvFinaleRead, StoryEventState.Triggered);
        game.hud.ShowFinalNote(heading, body, () => FinaleFlow.AfterReading(game));
    }
}

public static class FinaleFlow
{
    static readonly string[] FinalMonolog =
    {
        "Значит, всё это время я искал ответы не там.",
        "Я думал, что пытаюсь восстановить события прошлого. Но что, если всё это время я просто пытался вспомнить то, что мой собственный разум отказывался принимать?",
        "Отец мёртв. И я знаю, кто его убил.",
        "Теперь осталось решить, что делать с этой правдой.",
    };

    const string TruthSceneText =
        "Я больше не могу делать вид, что ничего не произошло.\n\n" +
        "Все эти годы я пытался найти виновного, не желая признавать, что ответ находился передо мной.\n\n" +
        "Я отправлю всё, что нашёл. Пусть теперь правду узнают остальные.\n\n" +
        "Мне придётся ответить за то, что я сделал.";

    const string TruthHeadline = "Правительство США принесло публичные извинения за разработку экспериментального вещества";
    const string TruthNews =
        "Сегодня правительство США официально признало факт разработки экспериментального вещества, " +
        "способного оказывать разрушительное воздействие на психику человека.\n\n" +
        "После публикации материалов расследования и передачи доказательств независимым СМИ программа была " +
        "подвергнута общественному расследованию.\n\n" +
        "Представители правительства принесли публичные извинения пострадавшим и их семьям.\n\n" +
        "Обстоятельства проведения экспериментов продолжают выясняться.";

    const string IllusionSceneText =
        "Я слишком долго пытался вспомнить.\n\n" +
        "Слишком долго искал ответы, которые, возможно, никогда не хотел находить.\n\n" +
        "Если существует возможность всё забыть, почему я должен от неё отказываться?\n\n" +
        "Может быть, правда не всегда стоит того, чтобы её принимать.";

    const string IllusionHeadline = "Неизвестное вещество могло быть применено в ходе военной операции США";
    const string IllusionNews =
        "По сообщениям зарубежных источников, в ходе недавней военной операции американские силы могли " +
        "применить боеприпасы неизвестного типа.\n\n" +
        "После их использования на позициях противника начались необъяснимые столкновения между собственными военнослужащими.\n\n" +
        "Очевидцы сообщают, что после воздействия неизвестного вещества солдаты начали проявлять агрессию друг к другу.\n\n" +
        "Официальные представители США не комментируют сообщения.\n\n" +
        "Эксперты предполагают, что речь может идти о ранее неизвестной разработке.";

    public static void AfterReading(DemoGame game)
    {
        Monolog.Then(() => game.hud.ShowFinaleChoice(choice => StartEnding(game, choice)), FinalMonolog);
    }

    static void StartEnding(DemoGame game, int choice)
    {
        InvestigationTravel.SetEventState(EvidenceCatalog.EvEndingChosen, StoryEventState.Triggered);
        bool truth = choice == 1;
        InvestigationTravel.SetEventState(truth ? EvidenceCatalog.EvEndingTruth : EvidenceCatalog.EvEndingIllusion,
            StoryEventState.Triggered);
        if (truth)
            game.hud.ShowNarration("ФИНАЛ / ПРИНЯТИЕ ПРАВДЫ", "Отправить доказательства",
                TruthSceneText,
                () => game.hud.FadeThrough(
                    () => game.hud.ShowNewsReport(TruthHeadline, TruthNews,
                        () => game.hud.ShowEndingArt("Принятие правды", "ending_truth"))));
        else
            game.hud.ShowNarration("ФИНАЛ / ИЛЛЮЗИЯ", "Принять вещество",
                IllusionSceneText,
                () => game.hud.FadeThrough(
                    () => game.hud.ShowNewsReport(IllusionHeadline, IllusionNews,
                        () => game.hud.ShowEndingArt("Иллюзия", "ending_illusion"))));
    }
}
