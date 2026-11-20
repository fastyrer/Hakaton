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

            public Entry Copy() => (Entry)MemberwiseClone();
        }

        public const float Width = 704, Height = 520, CardWidth = 200, CardHeight = 196;
        public IReadOnlyList<Entry> Entries => entries;
        public bool CanUndo => deleted.Count > 0;
        readonly List<Entry> entries = new List<Entry>();
        readonly Stack<Entry> deleted = new Stack<Entry>();
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

        public Entry Find(string id) => entries.Find(e => e.id == id);
        public bool HasEvidence(int sourceId) => entries.Exists(e => e.isEvidence && e.sourceId == sourceId);
        public bool Owns(int sourceId) => game != null && sourceId >= 0 &&
            sourceId < DemoGame.EvidenceTitles.Length && game.Found.Contains(sourceId);

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
            var entry = new Entry
            {
                id = "card-" + (++sequence), isEvidence = evidence, sourceId = sourceId,
                title = title, text = text,
                position = new Vector2(18 + (slot % 3) * 224 + (slot / 6) * 5, 20 + ((slot / 3) % 2) * 250)
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
            if (entry == null) return false;
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
