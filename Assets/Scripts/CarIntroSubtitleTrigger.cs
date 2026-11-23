using UnityEngine;

/// <summary>
/// Alexey's opening monologue during the car intro. Timings follow the drive;
/// edit lines in the inspector instead of touching code.
/// </summary>
public class CarIntroSubtitleTrigger : SubtitleSequence
{
    void Awake()
    {
        if (lines != null && lines.Length > 0) return;
        lines = new[]
        {
            new Line { time = 0.1f,  text = "Эх… Как всё так сложилось." },
            new Line { time = 2.5f,  text = "Три года как отца не стало. И всё, что написали в деле, — несчастный случай." },
            new Line { time = 6.5f,  text = "Почему я тогда сразу за это не взялся? Почему вещи его не разобрал?" },
            new Line { time = 12.5f, text = "Приезжал же сюда. Походил, посмотрел… И уехал." },
            new Line { time = 16.5f, text = "Иван говорит, что надо было давно всё вывезти. Дмитрий вообще слышать об этом не хочет." },
            new Line { time = 20.5f, text = "Так, я на месте." },
            new Line { time = 22.2f, text = "Отец тот ещё параноик был. Сейчас ещё с замками намучаюсь." },
            new Line { time = 25.0f, text = "Старый любил такую дребедень." },
        };
    }
}
