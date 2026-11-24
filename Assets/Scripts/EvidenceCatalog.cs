using System.Collections.Generic;

namespace Hakaton.Demo
{
    public enum StoryEventState { NotReady = 0, Ready = 1, Triggered = 2 }

    /// <summary>
    /// Реестр улик и выводов доски по сюжетным ID (ТЗ: проверка комбинаций по ID,
    /// а не по названию предмета). Фигурки — отдельная головоломка, здесь не участвуют.
    /// </summary>
    public static class EvidenceCatalog
    {
        // Улики E01–E13 → sourceId на доске (индекс в DemoGame.EvidenceTitles).
        public const string Scheme = "E01";          // Схема механизма с фигурками · кабинет
        public const string FloorMarks = "E02";      // Следы ботинок на ковре · кабинет
        public const string Bottle = "E03";          // Пустая бутылка «Берёзка» · кабинет
        public const string CaseFile = "E04";        // Дело отца с датой 1997 года · участок
        public const string GunInventory = "E05";    // Опись пистолета отца, .45 · участок
        public const string Expertise = "E06";       // Заключение эксперта: пуля 9 мм · участок
        public const string SimilarCase = "E07";     // Материалы похожего дела · участок
        public const string Shell = "E08";           // Гильза из старой куртки Алексея · его дом
        public const string Letter = "E09";          // Письмо от имени отца · дом Алексея
        public const string Year2000Doc = "E10";     // Документ о событиях 2000 года · хранилище
        public const string ProgramDocs = "E11";     // Записи и материалы программы · хранилище
        public const string Tapes = "E12";           // Записи с камер наблюдения · тайная комната
        public const string Photo = "E13";           // Семейная фотография · дом отца

        // Выводы.
        public const string CaliberMismatch = "D01";
        public const string SimilarPattern = "D02";
        public const string AlexGun = "D03";
        public const string DateContradiction = "D04";
        public const string LetterOperation = "D05";
        public const string UnknownVisitor = "D06";
        public const string MemoriesRestored = "D07";

        // События (not_ready → ready → triggered, запускаются один раз).
        public const string EvCaliberMismatch = "EV_CALIBER_MISMATCH";
        public const string EvPartnerCaseShown = "EV_PARTNER_CASE_SHOWN";
        public const string EvSimilarCase = "EV_SIMILAR_CASE";
        public const string EvAlexGun = "EV_ALEX_GUN_RECOGNITION";
        public const string EvDateContradiction = "EV_DATE_CONTRADICTION";
        public const string EvLetterOperation = "EV_LETTER_OPERATION";
        public const string EvUnknownVisitor = "EV_UNKNOWN_VISITOR";
        public const string EvMemoryRestoration = "EV_MEMORY_RESTORATION";

        // Финал: записка прочитана, выбор сделан, концовка (ТЗ «Финал и две концовки»).
        public const string EvFinaleRead = "EV_FINALE_READ";
        public const string EvEndingChosen = "EV_ENDING_CHOSEN";
        public const string EvEndingTruth = "EV_ENDING_TRUTH";
        public const string EvEndingIllusion = "EV_ENDING_ILLUSION";

        static readonly Dictionary<string, int> storyToSource = new Dictionary<string, int>
        {
            { Scheme, 8 }, { FloorMarks, 7 }, { Bottle, 6 }, { CaseFile, 3 }, { GunInventory, 4 },
            { Expertise, 5 }, { SimilarCase, 9 }, { Shell, 10 }, { Letter, 1 }, { Year2000Doc, 11 },
            { ProgramDocs, 12 }, { Tapes, 13 }, { Photo, 0 },
        };

        static readonly Dictionary<int, string> sourceToStory = BuildReverse();

        static Dictionary<int, string> BuildReverse()
        {
            var reverse = new Dictionary<int, string>();
            foreach (var pair in storyToSource) reverse[pair.Value] = pair.Key;
            return reverse;
        }

        public static string StoryIdForSource(int sourceId) =>
            sourceToStory.TryGetValue(sourceId, out var id) ? id : null;

        public sealed class Deduction
        {
            public string id;
            public string title;
            public string text;
            public string eventId;
        }

        public static readonly Dictionary<string, Deduction> Deductions = new Dictionary<string, Deduction>
        {
            { CaliberMismatch, new Deduction { id = CaliberMismatch, eventId = EvCaliberMismatch,
                title = "Несовпадение калибров",
                text = "Оружие, найденное рядом с телом отца, не соответствует калибру извлечённой пули. Пистолет из описи не мог выстрелить этой пулей. В материалах дела — явное противоречие." } },
            { SimilarPattern, new Deduction { id = SimilarPattern, eventId = EvSimilarCase,
                title = "Похожее дело",
                text = "В другом деле человек ничего не помнил, мотива не нашли — и дело закрыли. В деле отца — противоречие с калибрами. Связаны ли расследования? Пока это только гипотеза." } },
            { AlexGun, new Deduction { id = AlexGun, eventId = EvAlexGun,
                title = "Гильза Алексея",
                text = "Гильза из куртки Алексея — его калибр. Пуля в деле не подходит ни к пистолету отца, ни к пистолету Алексея. Стреляло третье оружие." } },
            { DateContradiction, new Deduction { id = DateContradiction, eventId = EvDateContradiction,
                title = "Настоящая дата",
                text = "В документах из хранилища встречается 2000 год. Напарник подтвердил: отец погиб в этом году. Почему Алексей до сих пор помнит 1997-й?" } },
            { LetterOperation, new Deduction { id = LetterOperation, eventId = EvLetterOperation,
                title = "Письмо и операция",
                text = "Письмо позвало Алексея к отцу. Материалы из хранилища связывают Алексея с операцией. Обе нити ведут к одному вечеру. Кто отправил письмо — пока неизвестно." } },
            { UnknownVisitor, new Deduction { id = UnknownVisitor, eventId = EvUnknownVisitor,
                title = "Посторонний в доме",
                text = "Следы ботинок не совпадают с обувью членов семьи. В стенах — камеры наблюдения. В доме бывал чужой. Кто именно и когда — пока неясно." } },
            { MemoriesRestored, new Deduction { id = MemoriesRestored, eventId = EvMemoryRestoration,
                title = "Воспоминания восстановлены",
                text = "Гильза. Дата. Письмо. Три нити сходятся: в день убийства Алексей был у отца. Теперь он это помнит." } },
        };

        static readonly Dictionary<string, string> combos = new Dictionary<string, string>();

        static EvidenceCatalog()
        {
            AddCombo(GunInventory, Expertise, CaliberMismatch);      // E05 + E06 → D01
            AddCombo(SimilarCase, CaliberMismatch, SimilarPattern);  // E07 + D01 → D02
            AddCombo(CaliberMismatch, Shell, AlexGun);               // D01 + E08 → D03
            AddCombo(CaseFile, Year2000Doc, DateContradiction);      // E04 + E10 → D04
            AddCombo(Letter, ProgramDocs, LetterOperation);          // E09 + E11 → D05
            AddCombo(FloorMarks, Tapes, UnknownVisitor);             // E02 + E12 → D06
        }

        static void AddCombo(string first, string second, string deductionId) =>
            combos[Key(first, second)] = deductionId;

        static string Key(string a, string b) => string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;

        /// <summary>Вывод для пары сюжетных ID (порядок не важен) или null.</summary>
        public static Deduction FindCombo(string first, string second)
        {
            if (first == null || second == null) return null;
            return combos.TryGetValue(Key(first, second), out var id) ? Deductions[id] : null;
        }

        public static bool IsFinaleInput(string deductionId) =>
            deductionId == AlexGun || deductionId == DateContradiction || deductionId == LetterOperation;
    }
}
