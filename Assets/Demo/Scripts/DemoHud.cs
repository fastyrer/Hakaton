using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Hakaton.Demo
{
    public sealed class DemoHud : MonoBehaviour
    {
        public enum Screen { Intro, Play, Evidence, Journal, Dialogue, Pause, Death, Ending, Travel, Board, Document }
        public Screen Mode { get; private set; }
        public Material retroMaterial;
        [Range(180,720)] public int worldVerticalResolution=240;
        public string locationCaption="НЬЮ-ДЖЕРСИ / ОКТЯБРЬ 2000";
        public Font font;
        public string dialogueText;
        DemoGame game;
        Canvas canvas;
        RectTransform overlay, modal, hud;
        Text objective, prompt, toast;
        float toastUntil;
        Screen evidenceBack;
        bool destinationStub;
        public DetectiveBoardView CurrentBoard { get; private set; }
        public Investigation.InvestigationBoardView CaseBoard { get; private set; }
        readonly Color ink = new Color(0.89f, 0.85f, 0.74f);
        readonly Color amber = new Color(0.76f, 0.55f, 0.29f);
        readonly Color muted = new Color(0.53f, 0.56f, 0.51f);

        public void Initialize(DemoGame owner)
        {
            game = owner;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            var events = new GameObject("Input events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            var raw = new GameObject("240p world", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            raw.transform.SetParent(transform, false);
            Stretch(raw.rectTransform);
            raw.raycastTarget = false;
            var retro = gameObject.AddComponent<RetroView>();
            retro.worldCamera = game.player.view;
            retro.verticalResolution=worldVerticalResolution;
            retro.display = raw;
            retro.displayMaterial = retroMaterial;
            // OnEnable runs before references are assigned; LateUpdate creates the target.

            hud = Rect("HUD", transform);
            Stretch(hud);
            Label(hud, locationCaption, 42, 26, 700, 24, 14, muted);
            objective = Label(hud, "", 42, 55, 950, 36, 20, ink);
            prompt = Label(hud, "", 300, 582, 680, 40, 19, ink, TextAnchor.MiddleCenter);
            Label(hud, "WASD  ДВИЖЕНИЕ     SHIFT  БЕГ     E  ОСМОТР     F  ФОНАРЬ     TAB  ДОСКА", 100, 673, 1080, 23, 14, muted, TextAnchor.MiddleCenter);
            Label(hud, "+", 624, 344, 32, 32, 18, new Color(0.8f, 0.8f, 0.68f, 0.65f), TextAnchor.MiddleCenter);
            toast = Label(hud, "", 200, 115, 880, 65, 20, ink, TextAnchor.MiddleCenter);
            overlay = Rect("Windows", transform);
            Stretch(overlay);
        }

        void Update()
        {
            if (game == null) return;
            objective.text = game.Objective;
            prompt.text = game.player.Focused != null && Mode == Screen.Play
                ? (game.player.Focused.kind == DemoInteractable.Interaction.Car ? "[ E / ЛКМ ]  " : "[ E ]  ") + game.player.Focused.Prompt : "";
            if (Time.unscaledTime > toastUntil) toast.text = "";
            if (Mode == Screen.Play && Cursor.lockState != CursorLockMode.Locked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                LockCursor(true);
        }

        public void Notify(string text, float seconds = 3.5f) { toast.text = text; toastUntil = Time.unscaledTime + seconds; }
        void LockCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }

        void Clear(Screen screen)
        {
            if(CaseBoard!=null){CaseBoard.Commit();CaseBoard=null;}
            if (CurrentBoard != null) { CurrentBoard.CommitPending(); CurrentBoard = null; }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Mode = screen;
            foreach (Transform child in overlay) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            hud.gameObject.SetActive(screen == Screen.Play);
            LockCursor(screen == Screen.Play);
            Time.timeScale = screen == Screen.Play ? 1 : 0;
        }

        public void Play() => Clear(Screen.Play);

        public void ShowBoard()
        {
            Clear(Screen.Board);
            if(game.investigation!=null)
            {
                var caseRoot=Rect("Доска расследования",overlay);Stretch(caseRoot);
                CaseBoard=caseRoot.gameObject.AddComponent<Investigation.InvestigationBoardView>();CaseBoard.Initialize(game.investigation);return;
            }
            var root = Rect("Detective board", overlay);
            Stretch(root);
            CurrentBoard = root.gameObject.AddComponent<DetectiveBoardView>();
            CurrentBoard.Initialize(game, font);
        }

        RectTransform Panel(Screen screen, string category, string title)
        {
            Clear(screen);
            var shade = Image(overlay, "Shade", new Color(0.015f, 0.02f, 0.018f, 0.86f));
            Stretch(shade.rectTransform);
            modal = Rect("Card", overlay);
            modal.anchorMin = modal.anchorMax = new Vector2(0.5f, 0.5f);
            modal.pivot = new Vector2(0.5f, 0.5f);
            modal.sizeDelta = new Vector2(1000, 580);
            Image(modal, "Paper", new Color(0.052f, 0.064f, 0.057f, 0.98f)).rectTransform.sizeDelta = new Vector2(1000, 580);
            Label(modal, category, 42, 28, 900, 25, 14, amber);
            Label(modal, title, 42, 68, 900, 65, 35, ink);
            var line = Image(modal, "Rule", muted);
            Place(line.rectTransform, 42, 143, 916, 1);
            return modal;
        }

        public RectTransform CasePanel(string category,string title)=>Panel(Screen.Document,category,title);
        public RectTransform CaseConversationPanel(string who)
        {
            Clear(Screen.Dialogue);
            var p=Rect("Разговор",overlay);p.anchorMin=p.anchorMax=new Vector2(.5f,0);p.pivot=new Vector2(.5f,1);p.anchoredPosition=new Vector2(0,254);p.sizeDelta=new Vector2(1040,232);
            Investigation.CaseUI.Box(p,"Фон",0,0,1040,232,new Color(.025f,.035f,.028f,.96f));
            Investigation.CaseUI.Text(p,who,24,14,990,30,20,amber);return p;
        }
        public void ShowIntro()
        {
            if (!string.IsNullOrWhiteSpace(game.locationTitle))
            {
                var location = Panel(Screen.Intro, "МАТЕРИАЛЫ ДЕЛА / ПРИБЫТИЕ", game.locationTitle);
                Label(location, "США · 2000 ГОД", 42, 172, 880, 30, 18, amber);
                Label(location, game.locationIntroduction, 42, 220, 890, 185, 23, ink);
                Button(location, "НАЧАТЬ", 42, 436, 380, Play);
                Label(location, "WASD / мышь · Shift — бег · E — взаимодействие\nF — фонарь · Tab — доска детектива · Esc — пауза", 42, 502, 900, 54, 16, muted);
                return;
            }
            if (game.arrivalScene)
            {
                var arrival = Panel(Screen.Intro, "ПЕРВАЯ СЦЕНА / ПРИБЫТИЕ", "ДОМ ПОМНИТ");
                Label(arrival, "НЬЮ-ДЖЕРСИ, США · ОСЕНЬ 2000 ГОДА", 42, 172, 880, 30, 18, amber);
                Label(arrival, "Дождь идёт с самого вечера. Семейный дом едва виден в тумане.\n\nВходная дверь на крыльце. Патрульная машина осталась во дворе.\nПодойди к ней, чтобы выбрать следующую локацию.", 42, 220, 890, 185, 23, ink);
                Button(arrival, "НАЧАТЬ", 42, 436, 380, Play);
                Label(arrival, "WASD / мышь · Shift — бег · E — дверь / предмет\nЛКМ по машине — локации · F — фонарь · Tab — доска · Esc — пауза", 42, 502, 900, 54, 16, muted);
                return;
            }
            var card = Panel(Screen.Intro, "ПЕРВЫЙ ИГРОВОЙ ПРОТОТИП / 01", "ДОМ ПОМНИТ");
            Label(card, "НЬЮ-ДЖЕРСИ, США · 2000 ГОД", 42, 172, 880, 30, 18, amber);
            Label(card, "Ты вернулся в семейный дом разобрать вещи отца.\nАнтон ждёт в гостиной. Начни с того, что осталось на виду.\n\nОсматривай предметы. Сопоставляй записи. Задавай вопросы.\nЗапомни дорогу на кухню — там есть свет.", 42, 220, 890, 185, 23, ink);
            Button(card, "ВОЙТИ В ДОМ", 42, 436, 380, () => { Play(); Notify("Антон в гостиной слева. Кабинет — следующая дверь по коридору.", 8); });
            Label(card, "WASD / мышь · Shift — бег · E — действие · F — фонарь\nTab — доска детектива · Esc — пауза", 42, 502, 900, 54, 16, muted);
        }

        public void ShowDocument(string title,string text)
        {
            var card=Panel(Screen.Document,"ЛИЧНЫЕ ЗАПИСИ / ДОМ ОТЦА",title);
            Label(card,text,42,172,900,290,23,ink);
            Button(card,"ПОЛОЖИТЬ НА МЕСТО [ ESC ]",42,502,916,Play);
        }

        public void ShowEvidence(int id, Screen back)
        {
            evidenceBack = back;
            var card = Panel(Screen.Evidence, "ИНВЕНТАРЬ / УЛИКА 0" + (id + 1), DemoGame.EvidenceTitles[id]);
            var preview = new GameObject("Document", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(card, false);
            Place(preview.rectTransform, 42, 174, 248, 248);
            preview.texture = game.EvidenceIllustration(id);
            Label(card, DemoGame.EvidenceText[id], 330, 170, 620, 298, 22, ink);
            Label(card, "ПРЕДМЕТ В ИНВЕНТАРЕ", 42, 450, 680, 30, 14, amber);
            Button(card, "ВЕРНУТЬСЯ [ ESC ]", 42, 502, 360, () => { if (back == Screen.Board || back == Screen.Journal) ShowBoard(); else Play(); });
            var pin = Button(card, game.board.HasEvidence(id) ? "УЛИКА УЖЕ НА ДОСКЕ" : "ПРИКРЕПИТЬ НА ДОСКУ", 510, 502, 448, () =>
            {
                if (game.board.AddEvidence(id, out var entry)) { ShowBoard(); CurrentBoard.Select(entry.id); }
            });
            pin.interactable = game.board.Owns(id) && !game.board.HasEvidence(id);
        }

        public void ShowJournal(bool presenting = false)
        {
            if (!presenting) { ShowBoard(); return; }
            var card = Panel(presenting ? Screen.Dialogue : Screen.Journal, presenting ? "РАЗГОВОР / ПРЕДЪЯВЛЕНИЕ" : "ПОЛЕВЫЕ ЗАПИСИ / " + game.Found.Count + " ИЗ 3", presenting ? "Что показать Антону?" : "Улики");
            for (int i = 0; i < 3; i++)
            {
                int id = i;
                bool found = game.Found.Contains(i);
                var button = Button(card, found ? "0" + (i + 1) + "    " + DemoGame.EvidenceTitles[i] : "0" + (i + 1) + "    — пока не найдено —", 42, 180 + i * 83, 916,
                    () => { if (presenting) game.Present(id); else ShowEvidence(id, Screen.Journal); });
                button.interactable = found;
            }
            Button(card, "НАЗАД [ ESC ]", 42, 502, 360, () => { if (presenting) game.Talk(); else Play(); });
        }

        public void ShowDialogue(string text)
        {
            dialogueText = text;
            var card = Panel(Screen.Dialogue, "ГОСТИНАЯ / РАЗГОВОР", "Антон");
            Label(card, text, 42, 170, 900, 245, 23, ink);
            Button(card, "ПРЕДЪЯВИТЬ УЛИКУ", 42, 436, 430, () => ShowJournal(true));
            Button(card, "ЗАКОНЧИТЬ РАЗГОВОР", 510, 436, 448, Play);
        }

        public void ShowRevelation(string text, Action next)
        {
            var card = Panel(Screen.Dialogue, "НОВАЯ ЗАЦЕПКА / КЕЛЛЕР", "Шум у боковой двери");
            Label(card, text, 42, 170, 900, 270, 23, ink);
            Button(card, "ПРОДОЛЖИТЬ", 42, 502, 400, next);
        }

        public void ShowPause()
        {
            game.investigation?.Save();
            var card = Panel(Screen.Pause, "ПАУЗА", "Дом подождёт");
            Label(card, "Чувствительность мыши", 42, 178, 800, 30, 21, ink);
            Slider(card, 42, 229, game.player.sensitivity, 0.03f, 0.3f, v => game.player.sensitivity = v);
            Label(card, "Громкость", 42, 294, 800, 30, 21, ink);
            Slider(card, 42, 346, game.sound.Volume, 0, 1, v => game.sound.Volume = v);
            Button(card, "ПРОДОЛЖИТЬ", 42, 436, 430, Play);
            Button(card, "НАЧАТЬ ЗАНОВО", 510, 436, 448, game.Restart);
            var mainMenu = Button(card, "ГЛАВНОЕ МЕНЮ", 42, 502, 916, () =>
            {
                Time.timeScale=1;
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            });
            mainMenu.interactable=Application.CanStreamedLevelBeLoaded("MainMenu");
        }

        public void ShowDeath()
        {
            var card = Panel(Screen.Death, "ПОГОНЯ / ПОРАЖЕНИЕ", "Он оказался ближе");
            Label(card, "Беги обратно по коридору. Кухня будет слева.\nВойди внутрь и закрой за собой дверь.\n\nУлики и разговор сохранены. Повтор начнётся у щитка.", 42, 180, 900, 220, 25, ink);
            Button(card, "ПОВТОРИТЬ ПОГОНЮ", 42, 436, 460, game.StartChase);
        }

        public void ShowEnding()
        {
            var card = Panel(Screen.Ending, "КОНЕЦ ПЕРВОГО ПРОТОТИПА", "За дверью стало тихо");
            Label(card, "На кухонном столе лежит папка. Внутри — ещё один адрес.\n\nНо шаги за дверью остановились совсем недавно.\n\nПродолжение следует.", 42, 180, 900, 230, 25, ink);
            Button(card, "ПРОЙТИ ЕЩЁ РАЗ", 42, 436, 460, game.Restart);
        }

        public void ShowTravelMenu()
        {
            if(game.investigation!=null){game.investigation.TravelMenu();return;}
            destinationStub = false;
            var card = Panel(Screen.Travel, "ПАТРУЛЬНАЯ МАШИНА / МАРШРУТ", "Куда поедем?");
            bool station = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PoliceStation";
            Button(card, station ? "ДОМ ВОЛКОВЫХ" : "ДОМ ВОЛКОВЫХ · ВЫ ЗДЕСЬ", 42, 177, 916,
                () => game.SelectDestination("Дом Волковых")).interactable = station;
            Button(card, station ? "ПОЛИЦЕЙСКИЙ УЧАСТОК · ВЫ ЗДЕСЬ" : "ПОЛИЦЕЙСКИЙ УЧАСТОК", 42, 253, 916,
                () => game.SelectDestination("Полицейский участок")).interactable = !station;
            Button(card, "ПРИДОРОЖНОЕ КАФЕ", 42, 329, 916, () => game.SelectDestination("Придорожное кафе"));
            Label(card, "Материалы дела и записи на доске останутся с вами.", 42, 422, 900, 36, 18, muted);
            Button(card, "ОСТАТЬСЯ ЗДЕСЬ [ ESC ]", 42, 502, 550, Play);
        }

        public void ShowDestinationStub(string destination)
        {
            destinationStub = true;
            var card = Panel(Screen.Travel, "МАРШРУТ / " + destination.ToUpperInvariant(), "Локация пока недоступна");
            string current = string.IsNullOrWhiteSpace(game.locationTitle) ? "семейный дом и его двор" : "локация «" + game.locationTitle + "»";
            Label(card, "Выбран пункт: «" + destination + "».\n\nСейчас можно посетить только " + current + ".\nЭта поездка появится в следующей версии.", 42, 180, 900, 250, 24, ink);
            Button(card, "К СПИСКУ ЛОКАЦИЙ", 42, 436, 430, ShowTravelMenu);
            Button(card, "ОСТАТЬСЯ У ДОМА", 510, 436, 448, Play);
        }

        public void Back()
        {
            if(game.investigation!=null&&game.investigation.ConversationOpen){game.investigation.ResumeConversation();return;}
            switch (Mode)
            {
                case Screen.Play: ShowPause(); break;
                case Screen.Pause: case Screen.Journal: case Screen.Dialogue: case Screen.Board: case Screen.Document: Play(); break;
                case Screen.Evidence: if (evidenceBack == Screen.Board || evidenceBack == Screen.Journal) ShowBoard(); else Play(); break;
                case Screen.Travel: if (destinationStub) ShowTravelMenu(); else Play(); break;
            }
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            return rect;
        }
        static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void Place(RectTransform rect, float x, float y, float w, float h)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); }
        static Image Image(Transform parent, string name, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }
        Text Label(Transform parent, string text, float x, float y, float w, float h, int size, Color color, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var rect = Rect(text.Length < 30 ? text : "Text", parent);
            Place(rect, x, y, w, h);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = size; label.color = color;
            label.alignment = alignment; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }
        Button Button(Transform parent, string text, float x, float y, float width, Action action)
        {
            var image = Image(parent, text, new Color(0.12f, 0.15f, 0.125f));
            Place(image.rectTransform, x, y, width, 54);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.5f, 1.35f, 1.05f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f);
            button.colors = colors;
            button.onClick.AddListener(() => action());
            Label(image.transform, text, 18, 0, width - 36, 54, 18, ink, TextAnchor.MiddleLeft);
            return button;
        }
        void Slider(Transform parent, float x, float y, float value, float min, float max, Action<float> changed)
        {
            var track = Image(parent, "Slider", new Color(0.2f, 0.23f, 0.2f));
            Place(track.rectTransform, x, y, 650, 18);
            var handle = Image(track.transform, "Handle", amber);
            handle.rectTransform.sizeDelta = new Vector2(24, 32);
            var slider = track.gameObject.AddComponent<Slider>();
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = min; slider.maxValue = max; slider.value = value;
            slider.onValueChanged.AddListener(v => changed(v));
        }
    }
}
