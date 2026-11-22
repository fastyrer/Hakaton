using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hakaton.Demo;
using Hakaton.Demo.FatherHouse;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class VerifyPuzzles
{
    static readonly List<string> passed = new List<string>();
    static readonly List<InputDevice> suspended = new List<InputDevice>();
    static DemoGame game;
    static Keyboard keyboard;
    static Mouse mouse;

    public static async Task<string[]> Main()
    {
        passed.Clear();
        game = UnityEngine.Object.FindAnyObjectByType<DemoGame>();
        if (!Application.isPlaying || game == null || SceneManager.GetActiveScene().name != "FatherHouse")
            throw new Exception("Start FatherHouse in Play Mode");
        var background = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard = InputSystem.AddDevice<Keyboard>("Puzzle test keyboard");
        mouse = InputSystem.AddDevice<Mouse>("Puzzle test mouse");
        foreach (var device in InputSystem.devices)
            if (device != keyboard && device != mouse && device.enabled && (device is Keyboard || device is Mouse))
            { suspended.Add(device); InputSystem.DisableDevice(device); }
        try
        {
            game.hud.Play(); await Frames(5);
            var clock = UnityEngine.Object.FindAnyObjectByType<CuckooClockPuzzle>();
            Check(clock != null && clock.key != null && clock.keyDropPoint != null, "Clock puzzle is configured");
            Check(game.GetPuzzleState(CuckooClockPuzzle.PuzzleId) == PuzzleState.Undiscovered, "Puzzle starts undiscovered");
            var doors = UnityEngine.Object.FindObjectsByType<DemoDoor>();
            var study = Door(doors, "Кабинет отца");
            var permanent = new[] { Door(doors, "Кладовая"), Door(doors, "Кухонная кладовая") };
            foreach (var door in permanent)
            { door.Interact(game); Check(!door.isOpen && !door.HasAccess(game), "Permanently locked: " + door.label); }
            Check(doors.Where(d => d.transform.position.y > 3).All(d => d.lockMode == DemoDoor.LockMode.None), "All upstairs doors remain unlocked");
            string[] free = { "Гостиная", "Мастерская", "Кухонный проход", "Столовая", "Кухня", "Из столовой в кухню" };
            Check(free.All(name => Door(doors, name).HasAccess(game)), "Required ground-floor doors remain accessible");
            Check(Door(doors, "Дверь в мастерскую").lockMode == DemoDoor.LockMode.Puzzle && Door(doors, "Спуск в подвал").lockMode == DemoDoor.LockMode.Puzzle,
                "Future story locks are configurable");
            Check(UnityEngine.Object.FindObjectsByType<Transform>().Count(t => t.name == "Бубны на замке") == 2, "Study door has a visible diamond lock");

            await FocusDoor(study);
            await Tap(Key.E);
            Check(!study.isOpen, "Study stays closed without key");
            float side = Vector3.Dot(game.player.transform.position - study.transform.parent.position, study.transform.parent.forward);
            Keys(Key.W); await Seconds(0.5); Keys(); await Frames(3);
            Check(Vector3.Dot(game.player.transform.position - study.transform.parent.position, study.transform.parent.forward) > 0.2f && side > 0,
                "Closed study physically blocks the player");

            game.player.Teleport(new Vector3(-7.76f, 0.08f, 7.9f), 0);
            var bounds = clock.GetComponent<BoxCollider>().bounds;
            game.player.AimAt(bounds.center); await Frames(5);
            Check(game.player.Focused == clock, "Clock is reachable by interaction ray");
            Check(game.GetPuzzleState(CuckooClockPuzzle.PuzzleId) == PuzzleState.Available, "Focused puzzle becomes available");
            await Tap(Key.E);
            Check(game.GetPuzzleState(CuckooClockPuzzle.PuzzleId) == PuzzleState.Started, "Clock interaction starts puzzle");
            await Seconds(0.8);
            Check(game.GetPuzzleState(CuckooClockPuzzle.PuzzleId) == PuzzleState.Solved && clock.key.gameObject.activeSelf,
                "Solving clock releases one key");
            Check(Vector3.Distance(clock.key.transform.position, clock.keyDropPoint.position) < 0.01f, "Key reaches floor drop point");
            game.player.AimAt(bounds.center); await Frames(3); await Tap(Key.E);
            Check(UnityEngine.Object.FindObjectsByType<QuestPickup>().Length == 1 && game.GetPuzzleState(CuckooClockPuzzle.PuzzleId) == PuzzleState.Solved,
                "Repeated clock interaction does not duplicate key");

            game.player.AimAt(clock.key.transform.position); await Frames(5);
            Check(game.player.Focused == clock.key, "Dropped key is reachable");
            await Tap(Key.E);
            Check(game.HasItem(CuckooClockPuzzle.StudyKeyItem) && !clock.key.gameObject.activeSelf, "Key is collected only once");

            await FocusDoor(study); await Tap(Key.E); await Seconds(0.8);
            Check(study.isOpen && study.HasAccess(game), "Key unlocks study through normal interaction");
            var frame = study.transform.parent;
            await Walk(frame.position - frame.forward * 0.8f);
            Check(Vector3.Dot(game.player.transform.position - frame.position, frame.forward) < 0, "Player can enter unlocked study");

            game.SelectDestination("Полицейский участок");
            await Until(() => SceneManager.GetActiveScene().name == "PoliceStation" && UnityEngine.Object.FindAnyObjectByType<DemoGame>() != null, 30);
            game = UnityEngine.Object.FindAnyObjectByType<DemoGame>();
            Check(game.HasItem(CuckooClockPuzzle.StudyKeyItem) && game.GetPuzzleState(CuckooClockPuzzle.PuzzleId) == PuzzleState.Solved,
                "Travel preserves key and solved state");
            game.SelectDestination("Дом Волковых");
            await Until(() => SceneManager.GetActiveScene().name == "FatherHouse" && UnityEngine.Object.FindAnyObjectByType<CuckooClockPuzzle>() != null, 30);
            game = UnityEngine.Object.FindAnyObjectByType<DemoGame>();
            clock = UnityEngine.Object.FindAnyObjectByType<CuckooClockPuzzle>();
            Check(game.HasItem(CuckooClockPuzzle.StudyKeyItem) && !clock.key.gameObject.activeSelf && Door(UnityEngine.Object.FindObjectsByType<DemoDoor>(), "Кабинет отца").HasAccess(game),
                "Return home restores collected key without respawn");

            File.WriteAllText("Docs/puzzle-verification.txt", "PASS\n" + string.Join("\n", passed));
            return passed.ToArray();
        }
        catch (Exception e)
        {
            File.WriteAllText("Docs/puzzle-verification.txt", "FAIL " + e.Message + "\n" + string.Join("\n", passed));
            throw;
        }
        finally
        {
            Keys();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (var device in suspended) if (device.added) InputSystem.EnableDevice(device);
            suspended.Clear(); InputSystem.settings.backgroundBehavior = background;
        }
    }

    static DemoDoor Door(DemoDoor[] doors, string label) => doors.Single(d => d.label == label);
    static async Task FocusDoor(DemoDoor door)
    {
        var frame = door.transform.parent;
        game.player.Teleport(frame.position + frame.forward * 1.05f + Vector3.up * 0.08f, 0);
        game.player.AimAt(door.transform.TransformPoint(new Vector3(0.75f, 1.1f, 0)));
        await Frames(5);
        Check(game.player.Focused == door, "Door is reachable: " + door.label);
    }
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); passed.Add("PASS  " + message); }
    static void Keys(params Key[] keys) { keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    static async Task Tap(Key key) { Keys(key); await Frames(3); Keys(); await Frames(3); }
    static async Task Walk(Vector3 target)
    {
        double start = EditorApplication.timeSinceStartup;
        while (Vector2.Distance(new Vector2(game.player.transform.position.x, game.player.transform.position.z), new Vector2(target.x, target.z)) > 0.14f)
        {
            Vector3 delta = target - game.player.transform.position;
            game.player.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 0);
            Keys(Key.W); await Frames(1);
            if (EditorApplication.timeSinceStartup - start > 8) throw new Exception("Walk blocked");
        }
        Keys(); await Frames(3);
    }
    static Task Frames(int count) { int start = Time.frameCount; return Until(() => Time.frameCount >= start + count, 8); }
    static Task Seconds(double seconds) { double start = EditorApplication.timeSinceStartup; return Until(() => EditorApplication.timeSinceStartup >= start + seconds, seconds + 5); }
    static Task Until(Func<bool> predicate, double timeout)
    {
        var done = new TaskCompletionSource<bool>(); double start = EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            try
            {
                if (predicate()) { EditorApplication.update -= tick; done.TrySetResult(true); }
                else if (EditorApplication.timeSinceStartup - start > timeout)
                { EditorApplication.update -= tick; done.TrySetException(new Exception("Timeout")); }
            }
            catch (Exception e) { EditorApplication.update -= tick; done.TrySetException(e); }
        };
        EditorApplication.update += tick; return done.Task;
    }
}
