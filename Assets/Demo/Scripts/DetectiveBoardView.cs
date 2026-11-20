using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Hakaton.Demo
{
    public sealed class DetectiveBoardView : MonoBehaviour
    {
        public RectTransform Surface { get; private set; }
        public string SelectedId { get; private set; }
        public InputField TitleInput { get; private set; }
        public InputField BodyInput { get; private set; }
        public Dropdown SourceInput { get; private set; }
        DemoGame game;
        DetectiveBoard board;
        Font font;
        RectTransform window, inventory, inspector;
        Text status;
        Button undo;
        Texture2D cork;
        readonly Dictionary<string, BoardCardDrag> cards = new Dictionary<string, BoardCardDrag>();
        readonly List<int> sourceIds = new List<int>();
        bool newNote, binding;
        readonly Color ink = new Color(0.88f, 0.85f, 0.75f);
        readonly Color dim = new Color(0.53f, 0.58f, 0.53f);
        readonly Color paperInk = new Color(0.14f, 0.16f, 0.13f);
        readonly Color paper = new Color(0.84f, 0.80f, 0.65f);
        readonly Color panel = new Color(0.055f, 0.073f, 0.063f);

        public void Initialize(DemoGame owner, Font typeface)
        {
            game = owner; board = game.board; font = typeface;
            var shade = Img(transform, "Board shade", new Color(0.012f, 0.018f, 0.016f, 0.97f));
            Stretch(shade.rectTransform);
            window = Rect(transform, "Detective board window");
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(0.5f, 0.5f);
            window.sizeDelta = new Vector2(1280, 720);
            Fit();
            Label(window, "ДОСКА ДЕТЕКТИВА", 24, 17, 900, 38, 30, ink);
            Label(window, "ДЕЛО 01 / СЕМЕЙНЫЙ ДОМ / ОКТЯБРЬ 2000", 26, 62, 980, 22, 13, dim);
            Btn(window, "Close board", "ЗАКРЫТЬ [ TAB ]", 1064, 22, 192, 42, () => game.hud.Play());
            inventory = Panel("Inventory", 24, 100, 228, 565);
            inspector = Panel("Card inspector", 988, 100, 268, 565);
            var frame = Img(window, "Wooden frame", new Color(0.12f, 0.085f, 0.052f));
            Place(frame.rectTransform, 262, 139, 716, 532);
            Surface = Rect(window, "Board surface");
            Place(Surface, 268, 145, DetectiveBoard.Width, DetectiveBoard.Height);
            var backing = Surface.gameObject.AddComponent<RawImage>();
            cork = MakeCork(); backing.texture = cork;
            backing.uvRect = new Rect(0, 0, DetectiveBoard.Width / 64, DetectiveBoard.Height / 64);
            Surface.gameObject.AddComponent<RectMask2D>();
            Btn(window, "New note", "+ НОВАЯ ЗАПИСЬ", 268, 98, 174, 34, BeginNewNote, 13);
            undo = Btn(window, "Undo removal", "ВЕРНУТЬ УДАЛЁННУЮ", 800, 98, 172, 34, UndoRemoval, 12);
            status = Label(window, "", 454, 103, 334, 28, 13, dim);
            Label(window, "ЛКМ — выбрать и перетащить карточку · Tab / Esc — закрыть · изменения сохраняются до нового начала игры", 24, 688, 1232, 22, 13, dim);
            SyncCards();
            RefreshInventory();
            if (board.Entries.Count > 0) SelectedId = board.Entries[0].id;
            RefreshSelection();
            BindInspector();
            Status("Перетаскивайте карточки мышью");
        }

        void LateUpdate() => Fit();
        void Fit()
        {
            if (window == null) return;
            var bounds = ((RectTransform)transform).rect;
            float scale = Mathf.Min(bounds.width / 1280, bounds.height / 720);
            window.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

        public void Status(string message)
        {
            if (status != null) status.text = message;
            if (undo != null) undo.interactable = board.CanUndo;
        }

        public void Select(string id)
        {
            if (board.Find(id) == null) return;
            CommitPending();
            SelectedId = id;
            newNote = false;
            board.BringToFront(id);
            SyncCards();
            RefreshSelection();
            BindInspector();
        }

        public void Move(string id, Vector2 position)
        {
            if (!board.Move(id, position)) return;
            if (cards.TryGetValue(id, out var handle)) handle.Refresh(board.Find(id), SelectedId == id);
        }

        public void BeginNewNote()
        {
            CommitPending();
            newNote = true;
            BindInspector();
            TitleInput.ActivateInputField();
            Status("Укажите заголовок, текст и основание");
        }

        // Called before closing/replacing the board, so typing is never lost on Tab/Esc.
        public void CommitPending()
        {
            if (binding || TitleInput == null || BodyInput == null) return;
            string text = BodyInput.text;
            if (string.IsNullOrWhiteSpace(text)) return;
            string title = string.IsNullOrWhiteSpace(TitleInput.text) ? "Заметка по делу" : TitleInput.text;
            int source = SourceInput != null && SourceInput.value < sourceIds.Count ? sourceIds[SourceInput.value] : -1;
            if (newNote)
            {
                if (board.AddNote(title, text, source, out var created))
                { SelectedId = created.id; newNote = false; SyncCards(); }
            }
            else if (SelectedId != null) board.UpdateNote(SelectedId, title, text, source);
            SyncCards();
        }

        void SaveNote()
        {
            if (string.IsNullOrWhiteSpace(TitleInput.text) || string.IsNullOrWhiteSpace(BodyInput.text))
            { Status("Введите заголовок и текст записи"); return; }
            int source = sourceIds[SourceInput.value];
            bool saved;
            if (newNote)
            {
                saved = board.AddNote(TitleInput.text, BodyInput.text, source, out var created);
                if (saved) SelectedId = created.id;
            }
            else saved = board.UpdateNote(SelectedId, TitleInput.text, BodyInput.text, source);
            if (!saved) { Status("Запись не добавлена: проверьте текст и основание"); return; }
            newNote = false;
            SyncCards(); RefreshSelection(); BindInspector();
            Status("Запись сохранена");
        }

        void CancelEditing()
        {
            newNote = false;
            BindInspector();
            Status("Несохранённые изменения отменены");
        }

        void RemoveSelected()
        {
            CommitPending();
            if (SelectedId == null || !board.Remove(SelectedId)) return;
            SelectedId = null; newNote = false;
            TitleInput = BodyInput = null; SourceInput = null;
            SyncCards(); RefreshInventory(); BindInspector();
            Status("Карточка убрана · предмет остался в инвентаре");
        }

        void UndoRemoval()
        {
            CommitPending();
            if (!board.UndoRemove(out var restored))
            { Status("Сначала уберите дубликат этой улики с доски"); return; }
            SelectedId = restored.id; newNote = false;
            SyncCards(); RefreshInventory(); RefreshSelection(); BindInspector();
            Status("Карточка возвращена на прежнее место");
        }

        void AddEvidence(int id)
        {
            CommitPending();
            if (!board.AddEvidence(id, out var entry)) { Status("Эта улика уже на доске"); return; }
            SelectedId = entry.id; newNote = false;
            SyncCards(); RefreshInventory(); RefreshSelection(); BindInspector();
            Status("Улика прикреплена из инвентаря");
        }

        void RefreshInventory()
        {
            ClearChildren(inventory);
            Label(inventory, "ИНВЕНТАРЬ", 16, 16, 200, 30, 20, ink);
            Label(inventory, "НАЙДЕНО: " + game.Found.Count + " / 3", 16, 49, 200, 22, 13, dim);
            int row = 0;
            for (int id = 0; id < DemoGame.EvidenceTitles.Length; id++)
            {
                if (!board.Owns(id)) continue;
                int source = id;
                var item = Img(inventory, "Inventory item " + id, new Color(0.092f, 0.115f, 0.095f));
                Place(item.rectTransform, 6, 83 + row * 144, 216, 136);
                Label(item.transform, "ПРЕДМЕТ 0" + (id + 1), 10, 8, 198, 18, 11, dim);
                var photo = Preview(item.transform, game.EvidenceIllustration(id), 10, 33, 42, 42);
                Label(item.transform, DemoGame.EvidenceTitles[id], 61, 30, 145, 52, 16, ink);
                var add = Btn(item.transform, "Add evidence " + id, board.HasEvidence(id) ? "ПРИКРЕПЛЕНО" : "НА ДОСКУ", 10, 92, 118, 32, () => AddEvidence(source), 12);
                add.interactable = !board.HasEvidence(id);
                Btn(item.transform, "Read inventory " + id, "ОСМОТР", 136, 92, 70, 32, () => game.hud.ShowEvidence(source, DemoHud.Screen.Board), 12);
                row++;
            }
            if (row == 0)
                Label(inventory, "Улик пока нет.\n\nОсмотрите фотографию в гостиной и бумаги на столе кабинета.\n\nВзаимодействие — E.", 16, 91, 196, 300, 18, dim);
            else Label(inventory, "Удаление карточек не расходует найденные предметы.", 16, 521, 196, 38, 12, dim);
        }

        void SyncCards()
        {
            var gone = new List<string>();
            foreach (var pair in cards)
                if (board.Find(pair.Key) == null) { pair.Value.gameObject.SetActive(false); Destroy(pair.Value.gameObject); gone.Add(pair.Key); }
            foreach (var id in gone) cards.Remove(id);
            foreach (var entry in board.Entries)
            {
                if (!cards.TryGetValue(entry.id, out var handle))
                {
                    handle = CreateCard(entry);
                    cards.Add(entry.id, handle);
                }
                handle.Refresh(entry, entry.id == SelectedId);
                handle.transform.SetAsLastSibling();
            }
            if (undo != null) undo.interactable = board.CanUndo;
        }

        BoardCardDrag CreateCard(DetectiveBoard.Entry entry)
        {
            var image = Img(Surface, "Card " + entry.id, entry.isEvidence ? paper : new Color(0.80f, 0.82f, 0.66f));
            Place(image.rectTransform, entry.position.x, entry.position.y, DetectiveBoard.CardWidth, DetectiveBoard.CardHeight);
            var outline = image.gameObject.AddComponent<Outline>();
            var pin = Img(image.transform, "Red pin", new Color(0.46f, 0.12f, 0.07f));
            Place(pin.rectTransform, 96, 5, 8, 8); pin.raycastTarget = false;
            var type = Label(image.transform, "", 12, 19, 176, 18, 10, new Color(0.34f, 0.31f, 0.22f));
            var title = Label(image.transform, "", 12, 43, 176, 43, 17, paperInk);
            Text excerpt;
            if (entry.isEvidence)
            {
                Preview(image.transform, game.EvidenceIllustration(entry.sourceId), 12, 91, 64, 64);
                excerpt = Label(image.transform, "", 85, 91, 103, 72, 12, paperInk);
            }
            else excerpt = Label(image.transform, "", 12, 91, 176, 74, 14, paperInk);
            var source = Label(image.transform, "", 12, 175, 176, 16, 9, new Color(0.34f, 0.31f, 0.22f));
            var handle = image.gameObject.AddComponent<BoardCardDrag>();
            handle.Initialize(this, entry, title, excerpt, type, source, outline);
            return handle;
        }

        void RefreshSelection()
        {
            foreach (var pair in cards) pair.Value.Refresh(board.Find(pair.Key), pair.Key == SelectedId);
        }

        void BindInspector()
        {
            binding = true;
            TitleInput = BodyInput = null; SourceInput = null;
            ClearChildren(inspector);
            var selected = SelectedId == null ? null : board.Find(SelectedId);
            if (!newNote && selected == null)
            {
                Label(inspector, "КАРТОЧКА", 16, 16, 236, 30, 20, ink);
                Label(inspector, "Выберите карточку на доске.\n\nУлику можно прикрепить из инвентаря. Свою версию — записать кнопкой «Новая запись».\n\nКарточки перетаскиваются левой кнопкой мыши.", 16, 76, 236, 340, 19, dim);
                binding = false; return;
            }
            if (!newNote && selected.isEvidence)
            {
                Label(inspector, "УЛИКА / 0" + (selected.sourceId + 1), 16, 16, 236, 30, 20, ink);
                Label(inspector, selected.title, 16, 61, 236, 60, 22, ink);
                Preview(inspector, game.EvidenceIllustration(selected.sourceId), 62, 130, 144, 144);
                string preview = selected.text.Length > 210 ? selected.text.Substring(0, 207) + "…" : selected.text;
                Label(inspector, preview, 16, 291, 236, 154, 17, ink);
                Btn(inspector, "Read selected evidence", "ЧИТАТЬ УЛИКУ", 16, 464, 236, 40, () => game.hud.ShowEvidence(selected.sourceId, DemoHud.Screen.Board), 15);
                Btn(inspector, "Remove card", "УБРАТЬ С ДОСКИ", 16, 514, 236, 34, RemoveSelected, 13);
                binding = false; return;
            }
            Label(inspector, newNote ? "НОВАЯ ЗАПИСЬ" : "ЗАПИСЬ ПО ДЕЛУ", 16, 16, 236, 30, 20, ink);
            Label(inspector, "Ваши наблюдения и версии", 16, 52, 236, 22, 13, dim);
            Label(inspector, "ЗАГОЛОВОК", 16, 87, 236, 18, 12, dim);
            TitleInput = Field(inspector, "Note title", newNote ? "" : selected.title, "Краткий заголовок", 16, 110, 236, 56, 72, true);
            Label(inspector, "ТЕКСТ ЗАПИСИ", 16, 177, 236, 18, 12, dim);
            BodyInput = Field(inspector, "Note body", newNote ? "" : selected.text, "Что это может значить?", 16, 199, 236, 166, 1200, false);
            Label(inspector, "ОСНОВАНИЕ", 16, 381, 236, 18, 12, dim);
            int sourceId = selected != null ? selected.sourceId : -1;
            SourceInput = Sources(inspector, sourceId);
            Btn(inspector, "Save note", "СОХРАНИТЬ ЗАПИСЬ", 16, 464, 236, 40, SaveNote, 14);
            if (!newNote) Btn(inspector, "Remove card", "УДАЛИТЬ", 16, 514, 112, 34, RemoveSelected, 13);
            Btn(inspector, "Cancel note edit", "ОТМЕНА", newNote ? 16 : 140, 514, newNote ? 236 : 112, 34, CancelEditing, 13);
            binding = false;
        }

        InputField Field(Transform parent, string name, string value, string placeholder, float x, float y, float w, float h, int limit, bool title)
        {
            var obj = DefaultControls.CreateInputField(new DefaultControls.Resources());
            obj.name = name; obj.transform.SetParent(parent, false);
            Place((RectTransform)obj.transform, x, y, w, h);
            obj.GetComponent<Image>().color = paper;
            foreach (var text in obj.GetComponentsInChildren<Text>(true))
            { text.font = font; text.fontSize = 16; text.color = paperInk; text.supportRichText = false; text.alignment = TextAnchor.UpperLeft; }
            var field = obj.GetComponent<InputField>();
            field.lineType = title ? InputField.LineType.MultiLineSubmit : InputField.LineType.MultiLineNewline;
            field.characterLimit = limit;
            field.caretColor = paperInk;
            field.selectionColor = new Color(0.63f, 0.43f, 0.2f, 0.45f);
            field.text = value;
            var hint = field.placeholder as Text;
            if (hint != null) { hint.text = placeholder; hint.color = new Color(0.4f, 0.42f, 0.34f); }
            return field;
        }

        Dropdown Sources(Transform parent, int chosen)
        {
            var obj = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            obj.name = "Note source"; obj.transform.SetParent(parent, false);
            Place((RectTransform)obj.transform, 16, 403, 236, 42);
            obj.GetComponent<Image>().color = paper;
            foreach (var text in obj.GetComponentsInChildren<Text>(true))
            { text.font = font; text.fontSize = 13; text.color = paperInk; text.supportRichText = false; }
            var arrow = obj.transform.Find("Arrow");
            if (arrow != null) arrow.gameObject.SetActive(false);
            var dropdown = obj.GetComponent<Dropdown>();
            sourceIds.Clear(); sourceIds.Add(-1);
            var options = new List<string> { "По делу / без предмета" };
            for (int i = 0; i < DemoGame.EvidenceTitles.Length; i++) if (board.Owns(i))
            { sourceIds.Add(i); options.Add(DemoGame.EvidenceTitles[i]); }
            dropdown.ClearOptions(); dropdown.AddOptions(options);
            dropdown.value = Mathf.Max(0, sourceIds.IndexOf(chosen));
            dropdown.RefreshShownValue();
            return dropdown;
        }

        RectTransform Panel(string name, float x, float y, float w, float h)
        {
            var image = Img(window, name, panel);
            Place(image.rectTransform, x, y, w, h);
            return image.rectTransform;
        }

        RawImage Preview(Transform parent, Texture2D texture, float x, float y, float w, float h)
        {
            var rect = Rect(parent, "Item illustration");
            Place(rect, x, y, w, h);
            var raw = rect.gameObject.AddComponent<RawImage>();
            raw.texture = texture; raw.raycastTarget = false;
            raw.color = texture != null ? Color.white : new Color(0.3f, 0.33f, 0.27f);
            return raw;
        }

        static RectTransform Rect(Transform parent, string name)
        {
            var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            result.SetParent(parent, false);
            result.anchorMin = result.anchorMax = result.pivot = new Vector2(0, 1);
            return result;
        }
        static void Place(RectTransform r, float x, float y, float w, float h)
        { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); }
        static void Stretch(RectTransform r)
        { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static Image Img(Transform parent, string name, Color color)
        { var image = Rect(parent, name).gameObject.AddComponent<Image>(); image.color = color; return image; }
        Text Label(Transform parent, string text, float x, float y, float w, float h, int size, Color color)
        {
            var r = Rect(parent, "Text"); Place(r, x, y, w, h);
            var label = r.gameObject.AddComponent<Text>(); label.font = font; label.text = text;
            label.fontSize = size; label.color = color; label.raycastTarget = false; label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }
        Button Btn(Transform parent, string name, string text, float x, float y, float w, float h, Action action, int size = 14)
        {
            var image = Img(parent, name, new Color(0.14f, 0.19f, 0.145f));
            Place(image.rectTransform, x, y, w, h);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1.5f, 1.35f, 1.1f);
            colors.selectedColor = colors.normalColor; colors.disabledColor = new Color(0.45f, 0.45f, 0.45f); button.colors = colors;
            button.onClick.AddListener(() => action());
            var label = Label(image.transform, text, 7, 0, w - 14, h, size, ink);
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }
        static void ClearChildren(Transform root)
        { foreach (Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); } }
        static Texture2D MakeCork()
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGB24, false) { name = "Board cork", filterMode = FilterMode.Point };
            var random = new System.Random(2000);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            { float v = 0.84f + (float)random.NextDouble() * 0.22f; texture.SetPixel(x, y, new Color(0.26f * v, 0.20f * v, 0.12f * v)); }
            texture.Apply(); return texture;
        }
        void OnDestroy() { if (cork != null) Destroy(cork); }
    }
}
