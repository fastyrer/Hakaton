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
        public enum Screen { Intro, Play, Evidence, Journal, Dialogue, Pause, Death, Ending, Travel, Board, Document, Clock, Phone }
        public Screen Mode { get; private set; }
        public Material retroMaterial;
        [Range(180,720)] public int worldVerticalResolution=240;
        public string locationCaption="НЬЮ-ДЖЕРСИ / ОКТЯБРЬ 2000";
        public Font font;
        public Font consoleFont;
        public string dialogueText;
        DemoGame game;
        Canvas canvas;
        RectTransform overlay, modal, hud;
        Text objective, prompt, toast;
        float toastUntil;
        Screen evidenceBack;
        bool destinationStub;
        int callSession;
        public DetectiveBoardView CurrentBoard { get; private set; }
        readonly Color ink = new Color(0.89f, 0.85f, 0.74f);
        readonly Color amber = new Color(0.76f, 0.55f, 0.29f);
        readonly Color muted = new Color(0.53f, 0.56f, 0.51f);

        public void Initialize(DemoGame owner)
        {
            game = owner;
            var loaded = Resources.Load<Font>("Fonts/clacon2");
            if (loaded != null) font = loaded;
            else if (consoleFont != null) font = consoleFont;
            else font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            if (game == null || objective == null) return;
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
            callSession++;
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

        public void ShowPhoneMenu(FatherHouse.HousePhone phone)
        {
            var card = Panel(Screen.Phone, "ПРИХОЖАЯ / ТЕЛЕФОН", "Кому позвонить?");
            bool called = phone.IvanCalled;
            var partner = Button(card, "НАПАРНИК · НЕ НАБИРАТЬ", 42, 177, 916, () => { });
            partner.interactable = false;
            Button(card, called ? "ИВАН · УЖЕ ГОВОРИЛИ" : "ИВАН", 42, 253, 916, () => StartPhoneCall(phone)).interactable = !called;
            var dmitry = Button(card, "ДМИТРИЙ · НЕ НАБИРАТЬ", 42, 329, 916, () => { });
            dmitry.interactable = false;
            Label(card, "Аппарат рабочий. Трубку снимают не везде.", 42, 410, 900, 36, 16, muted);
            Button(card, "ПОЛОЖИТЬ ТРУБКУ [ ESC ]", 42, 502, 550, Play);
        }

        void StartPhoneCall(FatherHouse.HousePhone phone)
        {
            var card = Panel(Screen.Phone, "ТЕЛЕФОН / РАЗГОВОР", "Иван");
            var line = Label(card, "", 60, 240, 880, 180, 24, ink, TextAnchor.MiddleCenter);
            Label(card, "Разговор идёт сам. Esc — положить трубку.", 42, 502, 900, 30, 15, muted);
            StartCoroutine(PhoneCallRoutine(phone, card, line));
        }

        System.Collections.IEnumerator PhoneCallRoutine(FatherHouse.HousePhone phone, RectTransform card, Text line)
        {
            int session = callSession;
            var lines = phone.PhotoFound
                ? Concat(FatherHouse.HousePhone.CallIvan, FatherHouse.HousePhone.CallPhoto)
                : FatherHouse.HousePhone.CallIvan;
            foreach (string text in lines)
            {
                if (session != callSession || card == null) yield break;
                line.text = text;
                float wait = Mathf.Clamp(1.1f + text.Length * 0.05f, 1.6f, 6f);
                float until = Time.unscaledTime + wait;
                while (Time.unscaledTime < until && session == callSession) yield return null;
            }
            if (session != callSession || card == null) yield break;
            line.text = "· короткие гудки ·";
            phone.MarkIvanCalled();
            Button(card, "ПОЛОЖИТЬ ТРУБКУ [ ESC ]", 42, 436, 550, Play);
        }

        static string[] Concat(string[] first, string[] second)
        {
            var result = new string[first.Length + second.Length];
            first.CopyTo(result, 0);
            second.CopyTo(result, first.Length);
            return result;
        }

        // Последовательность реплик по сценарию (диалоги с напарником, телефонные звонки).
        // Состояние прогресса фиксируется ДО открытия окна, чтобы ранний Esc ничего не блокировал.
        public void ShowLineSequence(string category, string title, string[] lines, bool autoAdvance, Action onDone)
        {
            var card = Panel(Screen.Dialogue, category, title);
            var line = Label(card, "", 60, 200, 880, 220, 24, ink, TextAnchor.MiddleCenter);
            Label(card, autoAdvance ? "Разговор идёт сам. Esc — прервать." : "Нажимайте «Далее» или Esc.", 42, 502, 900, 30, 15, muted);
            var next = Button(card, "ДАЛЕЕ", 510, 436, 448, () => { });
            next.gameObject.SetActive(false);
            StartCoroutine(LineSequenceRoutine(card, line, next, lines, autoAdvance, onDone));
        }

        System.Collections.IEnumerator LineSequenceRoutine(RectTransform card, Text line, Button next, string[] lines, bool autoAdvance, Action onDone)
        {
            int session = callSession;
            foreach (string text in lines)
            {
                if (session != callSession || card == null) yield break;
                line.text = text;
                if (!autoAdvance)
                {
                    next.gameObject.SetActive(true);
                    bool clicked = false;
                    next.onClick.AddListener(() => clicked = true);
                    while (!clicked && session == callSession && card != null) yield return null;
                    next.onClick.RemoveAllListeners();
                    next.gameObject.SetActive(false);
                }
                else
                {
                    float wait = Mathf.Clamp(1.1f + text.Length * 0.05f, 1.6f, 6f);
                    float until = Time.unscaledTime + wait;
                    while (Time.unscaledTime < until && session == callSession) yield return null;
                }
            }
            if (session != callSession || card == null) yield break;
            if (onDone != null) { onDone(); onDone = null; }
            var done = Button(card, "ПРОДОЛЖИТЬ [ ESC ]", 42, 436, 430, Play);
            done.gameObject.SetActive(true);
        }

        public void ShowClockPuzzle(Action solved)
        {
            var card = Panel(Screen.Clock, "ГОСТИНАЯ / ЧАСЫ С КУКУШКОЙ", "Циферблат");
            var view = card.gameObject.AddComponent<FatherHouse.ClockFaceView>();
            view.Initialize(game, font, () => { Play(); solved?.Invoke(); });
            Label(card, "Стрелки можно тянуть мышью.\nМеханизм отзовётся, когда они встанут правильно.", 42, 170, 250, 120, 16, muted);
            Button(card, "ОТОЙТИ [ ESC ]", 42, 502, 360, Play);
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
            var card = Panel(Screen.Evidence, "ИНВЕНТАРЬ / УЛИКА " + DemoGame.EvidenceNumber(id), DemoGame.EvidenceTitles[id]);
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
                var button = Button(card, found ? DemoGame.EvidenceNumber(id) + "    " + DemoGame.EvidenceTitles[id] : DemoGame.EvidenceNumber(id) + "    — пока не найдено —", 42, 180 + i * 83, 916,
                    () => { if (presenting) game.Present(id); else ShowEvidence(id, Screen.Journal); });
                button.interactable = found;
            }
            Button(card, "НАЗАД [ ESC ]", 42, 502, 360, () => { if (presenting) game.Talk(); else Play(); });
        }

        public void ShowDialogue(string speaker, string text)
        {
            dialogueText = text;
            var card = Panel(Screen.Dialogue, "РАЗГОВОР", speaker.ToUpperInvariant());
            Label(card, speaker, 42, 28, 900, 30, 16, amber);
            Label(card, text, 42, 170, 900, 245, 23, ink);
            Button(card, "ПРЕДЪЯВИТЬ УЛИКУ", 42, 436, 430, () => ShowJournal(true));
            Button(card, "ЗАКОНЧИТЬ РАЗГОВОР", 510, 436, 448, Play);
        }

        public void ShowDialogue(string text)
        {
            ShowDialogue("Антон", text);
        }

        public void ShowRevelation(string text, Action next)
        {
            var card = Panel(Screen.Dialogue, "НОВАЯ ЗАЦЕПКА / КЕЛЛЕР", "Шум у боковой двери");
            Label(card, text, 42, 170, 900, 270, 23, ink);
            Button(card, "ПРОДОЛЖИТЬ", 42, 502, 400, next);
        }

        public void ShowPause()
        {
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

        // --- Финал и две концовки (ТЗ). Все экраны — Screen.Ending: Esc их не закрывает. ---

        public void ShowFinalNote(string heading, string text, Action onRead)
        {
            var card = Panel(Screen.Document, "СКРЫТОЕ ХРАНИЛИЩЕ / ЗАПИСКА", heading);
            Label(card, text, 42, 168, 900, 306, 19, ink);
            Button(card, "ДАЛЕЕ", 42, 502, 916, () => { Play(); onRead?.Invoke(); });
        }

        public void ShowFinaleChoice(Action<int> onChoose)
        {
            var card = Panel(Screen.Ending, "ФИНАЛ / ВЫБОР", "Что делать с этой правдой?");
            Label(card, "Расследование закончено. Решение необратимо.", 42, 158, 900, 32, 18, muted);
            Button(card, "ПРИНЯТЬ ПРАВДУ", 42, 232, 916, () => onChoose(1));
            Label(card, "Признать произошедшее и рассказать миру правду.", 58, 292, 860, 30, 17, muted);
            Button(card, "ОТКАЗАТЬСЯ ОТ ПРАВДЫ", 42, 352, 916, () => onChoose(2));
            Label(card, "Забыть произошедшее и остаться в мире иллюзий.", 58, 412, 860, 30, 17, muted);
        }

        public void ShowNarration(string category, string title, string text, Action onDone)
        {
            var card = Panel(Screen.Ending, category, title);
            Label(card, text, 60, 176, 880, 300, 23, ink);
            Button(card, "ДАЛЕЕ", 42, 502, 916, () => onDone?.Invoke());
        }

        public void ShowNewsReport(string headline, string text, Action onDone)
        {
            var card = Panel(Screen.Ending, "НОВОСТИ / ЭФИР", "Срочный выпуск");
            Label(card, headline, 42, 150, 916, 88, 24, amber);
            var rule = Image(card, "Rule", muted);
            Place(rule.rectTransform, 42, 244, 916, 1);
            Label(card, text, 42, 262, 916, 228, 20, ink);
            Button(card, "ДАЛЕЕ", 42, 502, 916, () => onDone?.Invoke());
        }

        public void ShowEndingArt(string endingName, string resourceName)
        {
            var card = Panel(Screen.Ending, "КОНЦОВКА", endingName);
            var rect = Rect("Ending art", card);
            Place(rect, 42, 148, 916, 330);
            var raw = rect.gameObject.AddComponent<RawImage>();
            var texture = Resources.Load<Texture2D>(resourceName);
            if (texture != null) { raw.texture = texture; raw.color = Color.white; }
            else raw.color = new Color(0.05f, 0.075f, 0.062f);
            if (texture == null)
                Label(card, "Место для иллюстрации · Resources/" + resourceName, 42, 484, 916, 24, 14, muted);
            Button(card, "В ГЛАВНОЕ МЕНЮ", 42, 514, 916, () =>
            {
                Time.timeScale = 1;
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            });
        }

        public void FadeThrough(Action midAction) => StartCoroutine(FadeThroughRoutine(midAction));

        System.Collections.IEnumerator FadeThroughRoutine(Action midAction)
        {
            int session = callSession;
            var shadeRect = Rect("Finale fade", transform);
            Stretch(shadeRect);
            var shade = shadeRect.gameObject.AddComponent<Image>();
            shade.raycastTarget = true;
            float elapsed = 0f;
            while (elapsed < 2.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                shade.color = new Color(0.004f, 0.006f, 0.005f, Mathf.Clamp01(elapsed / 2.2f));
                yield return null;
            }
            shade.color = new Color(0.004f, 0.006f, 0.005f, 1f);
            if (session == callSession) midAction?.Invoke();
            elapsed = 0f;
            while (elapsed < 1.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                shade.color = new Color(0.004f, 0.006f, 0.005f, 1f - Mathf.Clamp01(elapsed / 1.2f));
                yield return null;
            }
            Destroy(shadeRect.gameObject);
        }

        public void ShowTravelMenu()
        {
            destinationStub = false;
            var card = Panel(Screen.Travel, "ПАТРУЛЬНАЯ МАШИНА / МАРШРУТ", "Куда поедем?");
            bool station = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PoliceStation";
            if (InvestigationTravel.SafeDiscovered)
            {
                Button(card, station ? "ДОМ ВОЛКОВЫХ" : "ДОМ ВОЛКОВЫХ · ВЫ ЗДЕСЬ", 42, 177, 916,
                    () => game.SelectDestination("Дом Волковых")).interactable = station;
                Button(card, station ? "ПОЛИЦЕЙСКИЙ УЧАСТОК · ВЫ ЗДЕСЬ" : "ПОЛИЦЕЙСКИЙ УЧАСТОК", 42, 231, 916,
                    () => game.SelectDestination("Полицейский участок")).interactable = !station;
                Button(card, "ДОМ ИВАНА", 42, 285, 916, () => game.SelectDestination("Дом Ивана"));
                Button(card, "ДОМ ДМИТРИЯ", 42, 339, 916, () => game.SelectDestination("Дом Дмитрия"));
                Button(card, "ДОМ АЛЕКСЕЯ", 42, 393, 916, () => game.SelectDestination("Дом Алексея"));
                Button(card, "ПРИДОРОЖНОЕ КАФЕ", 42, 447, 916, () => game.SelectDestination("Придорожное кафе"));
                Button(card, "ОСТАТЬСЯ ЗДЕСЬ [ ESC ]", 42, 514, 550, Play);
                return;
            }
            Button(card, station ? "ДОМ ВОЛКОВЫХ" : "ДОМ ВОЛКОВЫХ · ВЫ ЗДЕСЬ", 42, 177, 916,
                () => game.SelectDestination("Дом Волковых")).interactable = station;
            Button(card, station ? "ПОЛИЦЕЙСКИЙ УЧАСТОК · ВЫ ЗДЕСЬ" : "ПОЛИЦЕЙСКИЙ УЧАСТОК", 42, 253, 916,
                () => game.SelectDestination("Полицейский участок")).interactable = !station;
            Button(card, "ПРИДОРОЖНОЕ КАФЕ", 42, 329, 916, () => game.SelectDestination("Придорожное кафе"));
            Label(card, "Материалы дела и записи на доске останутся с вами.", 42, 422, 900, 36, 18, muted);
            Button(card, "ОСТАТЬСЯ ЗДЕСЬ [ ESC ]", 42, 502, 550, Play);
        }

        public void ShowSafeFigurines(FatherHouse.StudySafePuzzle safe)
        {
            var card = Panel(Screen.Dialogue, "КАБИНЕТ ОТЦА / СЕЙФ", "Четыре места для фигурок");
            Label(card, "На крышке сейфа четыре углубления. К ним нужны одинаковые фигурки.", 42, 170, 900, 60, 20, ink);
            var available = safe.AvailableFigurines(game);
            if (available.Count == 0)
            {
                Label(card, safe.PlacedCount >= FatherHouse.StudySafePuzzle.FigurineItems.Length
                    ? "Все фигурки стоят на своих местах."
                    : "Подходящих фигурок в инвентаре пока нет. Осталось мест: " + (FatherHouse.StudySafePuzzle.FigurineItems.Length - safe.PlacedCount), 42, 260, 900, 60, 20, muted);
            }
            else
            {
                for (int i = 0; i < available.Count; i++)
                {
                    int index = available[i];
                    Button(card, "ПОСТАВИТЬ НА МЕСТО · ФИГУРКА (" + FatherHouse.StudySafePuzzle.FigurineNames[index] + ")",
                        42, 250 + i * 66, 916, () => { Play(); safe.Place(game, index); });
                }
            }
            Button(card, "ОТОЙТИ [ ESC ]", 42, 502, 360, Play);
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
            switch (Mode)
            {
                case Screen.Play: ShowPause(); break;
                case Screen.Pause: case Screen.Journal: case Screen.Dialogue: case Screen.Board: case Screen.Document: case Screen.Clock: case Screen.Phone: Play(); break;
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
