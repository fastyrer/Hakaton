using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hakaton.Demo;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public static class VerifyBoard
{
    static DemoGame game;
    static Keyboard keyboard;
    static Mouse mouse;
    static readonly List<InputDevice> suspended = new List<InputDevice>();
    static readonly List<string> passed = new List<string>();
    static DetectiveBoardView View => game.hud.CurrentBoard;

    public static async Task<string[]> Main()
    {
        passed.Clear();
        game = UnityEngine.Object.FindAnyObjectByType<DemoGame>();
        if (!Application.isPlaying || game == null) throw new Exception("Open a game scene in Play Mode first.");
        keyboard = InputSystem.AddDevice<Keyboard>("Board verification keyboard");
        mouse = InputSystem.AddDevice<Mouse>("Board verification mouse");
        var background = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        suspended.Clear();
        foreach (var device in InputSystem.devices)
            if (device != keyboard && device != mouse && device.enabled && (device is Keyboard || device is Mouse))
            { suspended.Add(device); InputSystem.DisableDevice(device); }
        try
        {
            Check(game.Found.Count == 0 && game.board.Entries.Count == 1, "Fresh game has case note and empty inventory");
            Check(game.evidence.Length >= 3 && game.evidence.Take(3).All(e => e != null && e.illustration != null), "All three world clues have inventory data and illustrations");
            ClickText(game.arrivalScene ? "НАЧАТЬ" : "ВОЙТИ В ДОМ"); await Frames(3);
            await Tap(Key.Tab);
            Check(game.hud.Mode == DemoHud.Screen.Board && View != null && Time.timeScale == 0, "Tab opens detective board and pauses world");
            Keys(Key.Tab); await Frames(5);
            Check(game.hud.Mode == DemoHud.Screen.Play, "Held Tab toggles only once");
            Keys(); await Frames(2); await Tap(Key.Tab);
            var frozen = game.player.transform.position;
            Keys(Key.W); await Seconds(0.3f); Keys(); await Frames(2);
            Check(game.player.transform.position == frozen, "Player cannot move while editing board");
            Check(!game.board.AddEvidence(0, out _) && !game.board.AddNote("Версия", "Не найдено", 0, out _), "Uncollected inventory items cannot create evidence or sourced notes");
            Check(!HasButton("Add evidence 0"), "Uncollected evidence is absent from inventory UI");
            ClickName("New note"); await Frames(2);
            ClickName("Save note"); await Frames(2);
            Check(game.board.Entries.Count == 1 && game.hud.Mode == DemoHud.Screen.Board, "Empty note is rejected without leaving board");
            ClickName("Cancel note edit"); await Frames(2);
            await Tap(Key.Tab);

            await Collect(0, new Vector3(-5.5f, 0.05f, -1.8f));
            Check(game.Found.SetEquals(new[] { 0 }) && game.hud.Mode == DemoHud.Screen.Evidence, "World interaction puts first clue in inventory");
            ClickText("ПРИКРЕПИТЬ НА ДОСКУ"); await Frames(3);
            var photo = game.board.Entries.Single(e => e.isEvidence && e.sourceId == 0);
            Check(game.hud.Mode == DemoHud.Screen.Board && game.board.Entries.Count == 2, "Inspection screen can pin owned evidence to board");
            Check(!FindButton("Add evidence 0").interactable && !game.board.AddEvidence(0, out _), "Duplicate evidence cards are blocked in UI and model");
            View.Select(photo.id);
            var destination = new Vector2(105, 266);
            Drag(photo.id, destination);
            Check(Vector2.Distance(photo.position, destination) < 0.1f, "Unity pointer drag updates stored card position");
            Drag(photo.id, new Vector2(9999, 9999));
            Check(photo.position.x <= DetectiveBoard.Width - DetectiveBoard.CardWidth - 8 && photo.position.y <= DetectiveBoard.Height - DetectiveBoard.CardHeight - 8, "Dragging outside surface clamps card to board");
            var window = GameObject.Find("Detective board window").transform;
            Vector3 scale = window.localScale;
            window.localScale = scale * 0.75f;
            Drag(photo.id, destination);
            window.localScale = scale;
            Check(Vector2.Distance(photo.position, destination) < 0.1f, "Dragging uses board coordinates correctly at another UI scale");
            await Tap(Key.Tab); await Tap(Key.Tab);
            photo = game.board.Find(photo.id);
            var rendered = FindCard(photo.id).GetComponent<RectTransform>().anchoredPosition;
            Check(Vector2.Distance(photo.position, destination) < 0.1f && Vector2.Distance(rendered, new Vector2(destination.x, -destination.y)) < 0.1f, "Card placement survives closing and reopening Tab");
            View.Select(photo.id); ClickName("Read selected evidence"); await Frames(2);
            Check(game.hud.Mode == DemoHud.Screen.Evidence, "Board opens full readable inventory item");
            ClickText("ВЕРНУТЬСЯ [ ESC ]"); await Frames(2);
            Check(game.hud.Mode == DemoHud.Screen.Board && game.board.Find(photo.id) != null, "Inspection returns to existing board state");
            await Tap(Key.Tab);

            await Collect(1, new Vector3(-6.7f, 0.05f, 8.8f));
            ClickText("ВЕРНУТЬСЯ [ ESC ]"); await Frames(2);
            await Collect(2, new Vector3(-4.8f, 0.05f, 8.8f));
            await Tap(Key.Tab);
            Check(game.Found.Count == 3 && game.hud.Mode == DemoHud.Screen.Board, "All clues can be collected and Tab opens board from inspection");
            ClickName("Add evidence 1"); await Frames(2);
            ClickName("Add evidence 2"); await Frames(2);
            Check(game.board.Entries.Count(e => e.isEvidence) == 3, "Every inventory item can be pinned independently");
            ClickName("New note"); await Frames(2);
            View.TitleInput.text = "Встреча с Келлером";
            View.BodyInput.text = "Проверить запись о встрече. Уточнить, видел ли кто-нибудь посетителя у боковой двери.";
            View.SourceInput.value = View.SourceInput.options.FindIndex(o => o.text == DemoGame.EvidenceTitles[2]);
            ClickName("Save note"); await Frames(2);
            var note = game.board.Entries.Single(e => !e.isEvidence && e.title == "Встреча с Келлером");
            Check(note.sourceId == 2 && note.text.Contains("боковой двери"), "Custom case note saves Cyrillic text and selected inventory source");
            Drag(note.id, new Vector2(310, 272));
            View.TitleInput.text = "Сверить время встречи";
            View.BodyInput.text = "Запись содержит только план. Сравнить с датой на фотографии.";
            View.SourceInput.value = View.SourceInput.options.FindIndex(o => o.text == DemoGame.EvidenceTitles[0]);
            ClickName("Save note"); await Frames(2);
            Check(game.board.Find(note.id).title == "Сверить время встречи" && note.sourceId == 0, "Existing note edits preserve identity and update source");
            Check(FindCard(note.id).GetComponentsInChildren<Text>().Any(t => t.text == "ОСНОВАНИЕ: ПРЕДМЕТ 01"), "Visible source label follows note edits");
            var savedPosition = note.position;
            View.Select(note.id); ClickName("Remove card"); await Frames(2);
            Check(game.board.Find(note.id) == null && game.Found.Count == 3, "Deleting note removes only board record, not inventory");
            ClickName("Undo removal"); await Frames(2);
            note = game.board.Find(note.id);
            Check(note != null && note.title == "Сверить время встречи" && note.position == savedPosition, "Undo restores deleted text, source and position");
            View.Select(photo.id); ClickName("Remove card"); await Frames(2);
            Check(!game.board.HasEvidence(0) && game.Found.Contains(0) && FindButton("Add evidence 0").interactable, "Removed evidence stays owned and becomes available to pin again");
            ClickName("Add evidence 0"); await Frames(2);
            Check(game.board.HasEvidence(0) && game.board.Entries.Count(e => e.isEvidence && e.sourceId == 0) == 1, "Evidence can be re-added without duplicates");

            ClickName("New note"); await Frames(2);
            View.TitleInput.text = "Что ещё проверить";
            View.BodyInput.text = "Найти адрес из бумаг отца.";
            await Tap(Key.Tab);
            var draft = game.board.Entries.Single(e => e.title == "Что ещё проверить");
            Check(game.hud.Mode == DemoHud.Screen.Play && draft.text.Contains("адрес"), "Tab commits a valid unfinished draft before closing");
            int count = game.board.Entries.Count;
            await Tap(Key.Tab); await Tap(Key.Tab); await Tap(Key.Tab);
            Check(game.board.Entries.Count == count, "Repeated board reopening does not duplicate saved notes");
            ClickName("New note"); await Frames(2);
            View.TitleInput.text = "Отменить эту запись"; View.BodyInput.text = "Не сохранять.";
            ClickName("Cancel note edit"); await Frames(2);
            Check(game.board.Entries.Count == count && !game.board.Entries.Any(e => e.title == "Отменить эту запись"), "Cancel discards a new draft without modifying board");
            await Tap(Key.Escape);
            Check(game.hud.Mode == DemoHud.Screen.Play && Time.timeScale == 1 && !game.Blocked, "Escape closes board and restores normal gameplay");
            var before = game.player.transform.position;
            Keys(Key.S); await Seconds(0.3f); Keys(); await Frames(2);
            Check(Vector3.Distance(before, game.player.transform.position) > 0.2f, "Movement works after repeated board, inspection and editor interactions");
            var original = game.board;
            var scratch = new GameObject("Board density verification").AddComponent<DetectiveBoard>();
            scratch.Initialize(game);
            for (int i = 0; i < 35; i++) scratch.AddNote("Наблюдение " + i, "Проверить обстоятельства дела.", -1, out _);
            game.board = scratch;
            game.hud.ShowBoard();
            ClickName("New note"); await Frames(2);
            View.TitleInput.text = "Сохранить и на заполненной доске";
            View.BodyInput.text = "Запись не должна пропасть при закрытии.";
            await Tap(Key.Tab);
            Check(scratch.Entries.Any(e => e.title == "Сохранить и на заполненной доске"), "A draft is retained when closing a board containing many records");
            Check(scratch.Entries.All(e => e.position.x >= 8 && e.position.x <= DetectiveBoard.Width - DetectiveBoard.CardWidth - 8), "New cards remain on the surface as the record count grows");
            game.board = original;
            UnityEngine.Object.Destroy(scratch.gameObject);
            game.hud.ShowBoard();
            File.WriteAllText("Docs/board-verification.txt", "PASS — " + DateTime.UtcNow.ToString("u") + "\n" + string.Join("\n", passed));
            return passed.ToArray();
        }
        catch (Exception e)
        {
            File.WriteAllText("Docs/board-verification.txt", "FAIL — " + e.Message + "\n" + string.Join("\n", passed));
            throw;
        }
        finally
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (var device in suspended) if (device.added) InputSystem.EnableDevice(device);
            suspended.Clear(); InputSystem.settings.backgroundBehavior = background;
        }
    }

    static async Task Collect(int id, Vector3 position)
    {
        game.player.Teleport(position, 0);
        game.player.AimAt(game.evidence[id].transform.position);
        await Frames(3);
        Check(game.player.Focused == game.evidence[id], "Clue " + id + " is physically reachable");
        await Tap(Key.E);
    }
    static void Drag(string id, Vector2 destination)
    {
        var card = FindCard(id);
        var entry = game.board.Find(id);
        var localStart = new Vector3(entry.position.x + 40, -entry.position.y - 28, 0);
        var localEnd = new Vector3(destination.x + 40, -destination.y - 28, 0);
        var data = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, View.Surface.TransformPoint(localStart))
        };
        ExecuteEvents.Execute(card.gameObject, data, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(card.gameObject, data, ExecuteEvents.beginDragHandler);
        data.position = RectTransformUtility.WorldToScreenPoint(null, View.Surface.TransformPoint(localEnd));
        ExecuteEvents.Execute(card.gameObject, data, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(card.gameObject, data, ExecuteEvents.endDragHandler);
    }
    static BoardCardDrag FindCard(string id) => UnityEngine.Object.FindObjectsByType<BoardCardDrag>().Single(c => c.EntryId == id && c.gameObject.activeInHierarchy);
    static bool HasButton(string name) => UnityEngine.Object.FindObjectsByType<Button>().Any(b => b.gameObject.activeInHierarchy && b.name == name);
    static Button FindButton(string name) => UnityEngine.Object.FindObjectsByType<Button>().Single(b => b.gameObject.activeInHierarchy && b.name == name);
    static void ClickName(string name) => FindButton(name).onClick.Invoke();
    static void ClickText(string text)
    {
        var button = UnityEngine.Object.FindObjectsByType<Button>().FirstOrDefault(b => b.gameObject.activeInHierarchy && b.GetComponentInChildren<Text>().text == text);
        if (button == null) throw new Exception("Button not found: " + text);
        button.onClick.Invoke();
    }
    static void Check(bool ok, string text) { if (!ok) throw new Exception("CHECK FAILED: " + text); passed.Add("PASS  " + text); }
    static void Keys(params Key[] keys) { keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    static async Task Tap(Key key) { Keys(key); await Frames(3); Keys(); await Frames(3); }
    static Task Frames(int count) { int start = Time.frameCount; return Wait(() => Time.frameCount >= start + count, 4); }
    static Task Seconds(double seconds) { double start = EditorApplication.timeSinceStartup; return Wait(() => EditorApplication.timeSinceStartup >= start + seconds, seconds + 3); }
    static Task Wait(Func<bool> predicate, double seconds)
    {
        var t = new TaskCompletionSource<bool>(); double start = EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            try
            {
                if (predicate()) { EditorApplication.update -= tick; t.TrySetResult(true); }
                else if (EditorApplication.timeSinceStartup - start > seconds) { EditorApplication.update -= tick; t.TrySetException(new Exception("Board check timed out.")); }
            }
            catch (Exception e) { EditorApplication.update -= tick; t.TrySetException(e); }
        };
        EditorApplication.update += tick; return t.Task;
    }
}
