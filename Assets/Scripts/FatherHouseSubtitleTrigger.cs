using UnityEngine;

/// <summary>
/// The partner's phone call while Alexey walks from the car to the house.
/// Defaults to a player-zone trigger near the porch; lines stay editable.
/// </summary>
public class FatherHouseSubtitleTrigger : SubtitleSequence
{
    void Awake()
    {
        if (lines != null && lines.Length > 0) return;
        lines = new[]
        {
            new Line { time = 0f,  text = "· Звонок. Напарник ·" },
            new Line { time = 1.2f, text = "Н: Привет, Лёх. Слышал, тебе наконец отпуск согласовали. Уже выбрал, куда смотаешься?" },
            new Line { time = 6f,  text = "А: Привет. Да вот к отцу заехал наконец. Надо разобрать хлам." },
            new Line { time = 10f, text = "Н: В смысле? Ты опять поехал к нему?" },
            new Line { time = 13f, text = "А: В дом. Не на кладбище." },
            new Line { time = 15.5f, text = "Н: Да я понял." },
            new Line { time = 17.5f, text = "А: Ну а чего ты так?" },
            new Line { time = 19.5f, text = "Н: Ничего. Ладно, не моё дело. Как настроение будет, набери. Может, в бар сходим." },
            new Line { time = 24.5f, text = "А: Посмотрим. Я пока не знаю, сколько здесь провожусь." },
            new Line { time = 28f, text = "Н: Только телефон не выключай." },
            new Line { time = 30.5f, text = "А: Да куда я денусь." },
            new Line { time = 33f, text = "· Конец разговора ·" },
        };
    }
}
