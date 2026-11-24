using System.Collections;
using Hakaton.Demo;
using UnityEngine;

/// <summary>
/// Последствия логических выводов на доске (ТЗ «Система связей улик и сюжетных триггеров»):
/// внутренние монологи Алексея, сюжетные события, диалоги по сценарию.
/// Все события срабатывают один раз и сохраняются между сценами.
/// </summary>
public static class StoryConsequences
{
    // Сцена 26 «Настоящая дата» — звонок напарнику после E04 + E10.
    static readonly string[] CallDateLines =
    {
        "· короткие гудки ·",
        "Н: Лёх?",
        "А: Когда умер мой отец?",
        "Н: Что?",
        "А: Просто скажи дату.",
        "Н: Ты сейчас серьёзно?",
        "А: Да.",
        "Н: Он погиб в этом году.",
        "А: Нет. В девяносто седьмом.",
        "Н: Лёх…",
        "А: Я после его смерти приезжал. Я дом осматривал.",
        "Н: Осмотр делали другие сотрудники.",
        "А: Я помню, как ходил по комнатам.",
        "Н: Где ты сейчас?",
        "А: В кабинете.",
        "Н: Ничего больше не трогай. Я еду.",
    };

    // Сцены 27–30: восстановление воспоминаний. Порядок сцен не менять.
    static readonly string[] MemoryLines =
    {
        "А, читает: «Лёша, приезжай. Нужно кое-что обсудить. Лучше сегодня».",
        "А: И позвонить нельзя было?",
        "А: Надо ехать.",
        "Ф: Кто там?",
        "Ф: Лёша? Ты чего так поздно?",
        "А: Ты просил приехать.",
        "Ф: Когда?",
        "А: В письме.",
        "Ф: В каком письме? Я тебе ничего не писал.",
        "Ф: Ты выпил, что ли?",
        "Ф: Тогда сядь. Давай спокойно.",
        "Ф: Лёша. Убери.",
        "Ф: Ты меня слышишь? Опусти пистолет.",
        "· выстрел ·",
        "А: Это я.",
        "А: Я его убил.",
    };

    public static void OnDeductionFormed(DemoGame game, EvidenceCatalog.Deduction def)
    {
        if (game == null || def == null) return;
        switch (def.id)
        {
            case EvidenceCatalog.CaliberMismatch:
                Monolog.Then(null,
                    "Странно. Пистолет один, а калибр пули другой. Как это вообще прошло мимо следствия?");
                game.hud.Notify("Новый вывод на доске. Напарнику в участке есть что показать.", 6f);
                break;
            case EvidenceCatalog.SimilarPattern:
                Monolog.Then(null,
                    "В одном деле человек ничего не помнит. В другом даже оружие с пулей не сходится. И оба дела закрыты. Совпадение?");
                break;
            case EvidenceCatalog.AlexGun:
                Monolog.Then(null, "Мой калибр.", "У него другой…");
                break;
            case EvidenceCatalog.DateContradiction:
                Monolog.Then(() => game.hud.ShowLineSequence(
                        "ТЕЛЕФОН / ЗВОНОК НАПАРНИКУ", "Настоящая дата", CallDateLines, true, null),
                    "Двухтысячный?..", "Нет. Здесь ошибка.");
                break;
            case EvidenceCatalog.LetterOperation:
                Monolog.Then(null,
                    "Письмо звало меня к отцу. Эти материалы связывают меня с операцией. Всё идёт к одному вечеру.");
                break;
            case EvidenceCatalog.UnknownVisitor:
                Monolog.Then(null,
                    "Следы чужих ботинок и камеры в стенах. Отец следил за собственным домом. И сюда кто-то приходил. Кто — пока неясно.");
                break;
        }
        CheckFinale(game);
    }

    /// <summary>
    /// Флешбек разрешён только когда сформированы все три вывода: D03, D04 и D05.
    /// Вывод сохраняется даже если событие ещё ждёт остальных условий (ТЗ).
    /// </summary>
    public static void CheckFinale(DemoGame game)
    {
        if (game == null || game.board == null) return;
        if (InvestigationTravel.GetEventState(EvidenceCatalog.EvMemoryRestoration) == StoryEventState.Triggered) return;
        if (!game.board.HasDeduction(EvidenceCatalog.AlexGun) ||
            !game.board.HasDeduction(EvidenceCatalog.DateContradiction) ||
            !game.board.HasDeduction(EvidenceCatalog.LetterOperation)) return;
        InvestigationTravel.SetEventState(EvidenceCatalog.EvMemoryRestoration, StoryEventState.Ready);
        game.board.EnsureDeductionCard(EvidenceCatalog.MemoriesRestored);
        new GameObject("Воспоминания · сцены 27–30").AddComponent<StoryCoroutineHost>()
            .Run(FinaleRoutine(game));
    }

    static IEnumerator FinaleRoutine(DemoGame game)
    {
        // Не говорить поверх меню: ждём, пока игрок вернётся в игру.
        while (game.hud != null && game.hud.Mode != DemoHud.Screen.Play) yield return null;
        InvestigationTravel.SetEventState(EvidenceCatalog.EvMemoryRestoration, StoryEventState.Triggered);
        var manager = Object.FindAnyObjectByType<SubtitleManager>();
        if (manager == null) manager = new GameObject("Subtitles").AddComponent<SubtitleManager>();
        foreach (string text in MemoryLines)
        {
            float duration = Mathf.Clamp(1.6f + text.Length * 0.06f, 3f, 8f);
            manager.ShowSubtitle(text, duration);
            yield return new WaitForSecondsRealtime(duration + 0.2f);
        }
        if (game.hud != null)
            game.hud.Notify("Воспоминания восстановлены. Дело изменилось необратимо.", 8f);
    }
}

/// <summary>Короткий хост для корутин последствий, живёт в активной сцене.</summary>
public sealed class StoryCoroutineHost : MonoBehaviour
{
    public void Run(IEnumerator routine) => StartCoroutine(routine);
}

/// <summary>
/// Внутренний монолог Алексея: строки субтитрами друг за другом, опционально — продолжение.
/// </summary>
public static class Monolog
{
    public static void Then(System.Action onDone, params string[] lines)
    {
        if (lines == null || lines.Length == 0) { onDone?.Invoke(); return; }
        new GameObject("Монолог Алексея").AddComponent<StoryCoroutineHost>().Run(Routine(onDone, lines));
    }

    static IEnumerator Routine(System.Action onDone, string[] lines)
    {
        var manager = Object.FindAnyObjectByType<SubtitleManager>();
        if (manager == null) manager = new GameObject("Subtitles").AddComponent<SubtitleManager>();
        foreach (string text in lines)
        {
            float duration = Mathf.Clamp(1.6f + text.Length * 0.06f, 3f, 8f);
            manager.ShowSubtitle(text, duration);
            yield return new WaitForSecondsRealtime(duration + 0.2f);
        }
        onDone?.Invoke();
    }
}
