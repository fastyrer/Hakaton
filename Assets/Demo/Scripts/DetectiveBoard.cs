using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hakaton.Demo
{
    // Session state lives independently of the board's disposable UI.
    public sealed class DetectiveBoard : MonoBehaviour
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public bool isEvidence;
            public int sourceId = -1;
            public string title;
            public string text;
            public Vector2 position;
            public bool isDeduction;
            public string deductionId;

            public Entry Copy() => (Entry)MemberwiseClone();
        }

        [Serializable]
        public sealed class Connection
        {
            public string a;
            public string b;
            public bool confirmed;
        }

        public const float Width = 704, Height = 520, CardWidth = 200, CardHeight = 196;
        public IReadOnlyList<Entry> Entries => entries;
        public IReadOnlyList<Connection> Connections => connections;
        public bool CanUndo => deleted.Count > 0;
        readonly List<Entry> entries = new List<Entry>();
        readonly Stack<Entry> deleted = new Stack<Entry>();
        readonly List<Connection> connections = new List<Connection>();
        DemoGame game;
        int sequence;
        bool initialized;

        public void Initialize(DemoGame owner)
        {
            game = owner;
            if (initialized) return;
            initialized = true;
            AddNote(string.IsNullOrWhiteSpace(game.locationTitle) ? "Семейный дом" : game.locationTitle,
                string.IsNullOrWhiteSpace(game.locationObjective) ? "Нью-Джерси. Октябрь 2000.\n\nРазобрать вещи отца. Проверить записи и даты в его кабинете." : game.locationObjective, -1, out _);
        }

        public void RestoreEntries(IEnumerable<Entry> saved)
        {
            if (saved == null) return;
            entries.Clear(); deleted.Clear(); sequence = 0;
            foreach (var item in saved)
            {
                var copy = item.Copy();
                copy.id = string.IsNullOrEmpty(item.id) ? "card-" + (++sequence) : item.id;
                entries.Add(copy);
                int suffix = IdSuffix(copy.id);
                if (suffix > sequence) sequence = suffix;
            }
        }

        static int IdSuffix(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            int dash = id.LastIndexOf('-');
            if (dash < 0 || dash + 1 >= id.Length) return 0;
            return int.TryParse(id.Substring(dash + 1), out var value) ? value : 0;
        }

        public void RestoreConnections(IEnumerable<Connection> saved)
        {
            if (saved == null) return;
            connections.Clear();
            foreach (var item in saved)
                if (item != null && Find(item.a) != null && Find(item.b) != null)
                    connections.Add(new Connection { a = item.a, b = item.b, confirmed = item.confirmed });
        }

        public void PruneConnections()
        {
            connections.RemoveAll(c => Find(c.a) == null || Find(c.b) == null);
        }

        public Entry Find(string id) => entries.Find(e => e.id == id);
        public bool HasEvidence(int sourceId) => entries.Exists(e => e.isEvidence && e.sourceId == sourceId);
        public Entry FindDeduction(string deductionId) =>
            entries.Find(e => e.isDeduction && e.deductionId == deductionId);
        public bool HasDeduction(string deductionId) => FindDeduction(deductionId) != null;
        public bool Owns(int sourceId) => game != null && sourceId >= 0 &&
            sourceId < DemoGame.EvidenceTitles.Length && game.Found.Contains(sourceId);

        // --- Связи и логические выводы (проверка по сюжетным ID, порядок не важен) ---

        public bool HasConnection(string aId, string bId) =>
            connections.Exists(c => (c.a == aId && c.b == bId) || (c.a == bId && c.b == aId));

        /// <summary>
        /// Соединяет две карточки нитью. Правильная пара фиксирует вывод (красная нить
        /// + виртуальная карточка D0X + монолог + сюжетное событие один раз).
        /// Неправильная пары не наказывают — нить просто остаётся серой и снимается кликом.
        /// </summary>
        public string Connect(string aId, string bId, out string status)
        {
            status = null;
            var a = Find(aId);
            var b = Find(bId);
            if (a == null || b == null || aId == bId) return null;
            if (HasConnection(aId, bId)) { status = "Такая связь уже есть на доске"; return null; }
            var def = EvidenceCatalog.FindCombo(StoryIdOf(a), StoryIdOf(b));
            connections.Add(new Connection { a = aId, b = bId, confirmed = def != null });
            if (def != null && !HasDeduction(def.id))
            {
                AddDeductionCard(def);
                InvestigationTravel.SetEventState(def.eventId, StoryEventState.Triggered);
                StoryConsequences.OnDeductionFormed(game, def);
                status = "Связь подтверждена · на доске новый вывод: " + def.title;
                return def.id;
            }
            status = def != null
                ? "Связь подтверждена."
                : "Связь отмечена. Убедительной закономерности пока нет.";
            return null;
        }

        static string StoryIdOf(Entry entry) => entry.isDeduction
            ? entry.deductionId
            : EvidenceCatalog.StoryIdForSource(entry.sourceId);

        public bool RemoveConnection(int index)
        {
            if (index < 0 || index >= connections.Count) return false;
            connections.RemoveAt(index);
            return true;
        }

        Entry AddDeductionCard(EvidenceCatalog.Deduction def)
        {
            var entry = Create(false, -1, def.title, def.text);
            entry.isDeduction = true;
            entry.deductionId = def.id;
            return entry;
        }

        /// <summary>Карточка вывода без повторного события: для восстановления и финала.</summary>
        public Entry EnsureDeductionCard(string deductionId)
        {
            var existing = FindDeduction(deductionId);
            if (existing != null) return existing;
            if (!EvidenceCatalog.Deductions.TryGetValue(deductionId, out var def)) return null;
            return AddDeductionCard(def);
        }

        public bool AddEvidence(int sourceId, out Entry entry)
        {
            entry = null;
            if (!Owns(sourceId) || HasEvidence(sourceId)) return false;
            entry = Create(true, sourceId, DemoGame.EvidenceTitles[sourceId], DemoGame.EvidenceText[sourceId]);
            return true;
        }

        public bool AddNote(string title, string text, int sourceId, out Entry entry)
        {
            entry = null;
            if (!Valid(title, text, sourceId)) return false;
            entry = Create(false, sourceId, CleanTitle(title), text.Trim());
            return true;
        }

        public bool UpdateNote(string id, string title, string text, int sourceId)
        {
            var entry = Find(id);
            if (entry == null || entry.isEvidence || !Valid(title, text, sourceId)) return false;
            entry.title = CleanTitle(title);
            entry.text = text.Trim();
            entry.sourceId = sourceId;
            return true;
        }

        bool Valid(string title, string text, int sourceId) => !string.IsNullOrWhiteSpace(title) &&
            title.Trim().Length <= 72 && !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= 1200 &&
            (sourceId == -1 || Owns(sourceId));

        static string CleanTitle(string title) => title.Trim().Replace('\n', ' ').Replace('\r', ' ');

        Entry Create(bool evidence, int sourceId, string title, string text)
        {
            int slot = entries.Count;
            int layer = slot / 6;
            var entry = new Entry
            {
                id = "card-" + (++sequence), isEvidence = evidence, sourceId = sourceId,
                title = title, text = text,
                position = new Vector2(18 + (slot % 3) * 224 + layer * 16, 20 + ((slot / 3) % 2) * 250 + layer * 12)
            };
            entries.Add(entry);
            Move(entry.id, entry.position);
            return entry;
        }

        public bool Move(string id, Vector2 position)
        {
            var entry = Find(id);
            if (entry == null || float.IsNaN(position.x) || float.IsNaN(position.y) ||
                float.IsInfinity(position.x) || float.IsInfinity(position.y)) return false;
            entry.position = new Vector2(Mathf.Clamp(position.x, 8, Width - CardWidth - 8),
                Mathf.Clamp(position.y, 8, Height - CardHeight - 8));
            return true;
        }

        public void BringToFront(string id)
        {
            var entry = Find(id);
            if (entry == null) return;
            entries.Remove(entry);
            entries.Add(entry);
        }

        public bool Remove(string id)
        {
            var entry = Find(id);
            if (entry == null || entry.isDeduction) return false;
            deleted.Push(entry.Copy());
            entries.Remove(entry);
            return true;
        }

        public bool UndoRemove(out Entry entry)
        {
            entry = null;
            if (deleted.Count == 0) return false;
            var previous = deleted.Peek();
            if (previous.sourceId >= 0 && !Owns(previous.sourceId)) return false;
            if (previous.isEvidence && HasEvidence(previous.sourceId)) return false;
            entry = deleted.Pop();
            entries.Add(entry);
            return true;
        }
    }
}
