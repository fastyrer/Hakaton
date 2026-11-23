using UnityEngine;

namespace Hakaton.Demo.FatherHouse
{
    /// <summary>
    /// Hallway telephone. Opens the contact panel; only Ivan answers, only once.
    /// The call lines live here next to the feature; they come from
    /// «Диалоги_Дом_помнит», scenes 4 and 5.
    /// </summary>
    public sealed class HousePhone : DemoInteractable
    {
        public const string CallFlag = "phone_ivan_called";

        public static readonly string[] CallIvan =
        {
            "· гудки ·",
            "И: Привет.",
            "А: Привет. Я у отца.",
            "И: В доме?",
            "А: Да. Кабинет закрыт. От него ключа у тебя нет?",
            "И: Нет. У меня вообще ключей от его дома не было.",
            "А: Я помню. Может, среди вещей попадался?",
            "И: Нет. А что за ключ?",
            "А: На замке бубны нарисованы.",
            "И: Господи. Он всё ещё этим занимался?",
            "А: Похоже на то.",
            "И: Посмотри в гостиной. В ящиках, под часами. Где-нибудь там.",
            "А: Почему под часами?",
            "И: Он в них постоянно лазил.",
            "А: Чинил?",
            "И: Они не ходили. Я хотел их снять, он на меня наорал.",
            "А: А ты спрашивал, зачем они ему?",
            "И: Спрашивал. Сказал, чтобы я своими вещами занимался.",
            "А: Очень на него похоже.",
            "И: Лёш, ты только аккуратно там. Он мог и провод какой-нибудь под напряжением оставить.",
            "А: Да я пока только дверь пытаюсь открыть.",
            "И: У него с этого всё и начиналось.",
        };

        // Дополнительная тема, если фотография уже найдена.
        public static readonly string[] CallPhoto =
        {
            "А: Нашёл фотографию. Где мы у крыльца стоим.",
            "И: Дмитрий там ещё щурится?",
            "А: Да. Ему солнце прямо в лицо.",
            "И: Отец нас тогда раз пять переставлял.",
            "А: Зато нормально получилось.",
            "И: Мы там минут сорок простояли.",
            "А: Я не помню, чтобы так долго.",
            "И: А я помню. Дмитрий уже ушёл, его обратно привели.",
            "А: Ему на месте вообще не сиделось.",
            "И: Ну так ребёнок был.",
            "А: Тебе оставить фотографию?",
            "И: Да. Только вместе с рамкой. Она там ещё целая?",
            "А: Вроде да.",
            "И: Тогда отложи. Заберу потом.",
        };

        DemoGame game;

        public override string Prompt => "Позвонить · " + label;

        void Start()
        {
            if (game == null) game = FindAnyObjectByType<DemoGame>();
        }

        public bool IvanCalled => game != null && game.GetPuzzleState(CallFlag) == PuzzleState.Solved;
        public bool PhotoFound => game != null && game.Found.Contains(0);

        public void MarkIvanCalled()
        {
            if (game != null) game.SetPuzzleState(CallFlag, PuzzleState.Solved);
        }

        public override void Interact(DemoGame owner)
        {
            game = owner;
            game.hud.ShowPhoneMenu(this);
        }
    }
}
