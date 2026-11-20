using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Hakaton.Demo;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public static class VerifyDemo
{
    static readonly List<string> passed = new List<string>();
    static Keyboard keyboard;
    static DemoGame game;

    public static async Task<string[]> Main()
    {
        passed.Clear();
        game = UnityEngine.Object.FindAnyObjectByType<DemoGame>();
        if (!Application.isPlaying || game == null) throw new Exception("Open HouseDemo in Play Mode first.");
        keyboard = InputSystem.AddDevice<Keyboard>("Demo smoke test");
        try
        {
            Check(game.hud.Mode == DemoHud.Screen.Intro, "Starts with introduction");
            Click("ВОЙТИ В ДОМ");
            await Frames(3);
            Check(!game.Blocked && Time.timeScale == 1, "Intro button resumes game and player input");
            int frame = Time.frameCount;
            await Frames(4);
            Check(Time.frameCount > frame, "Player loop advances");

            var start = game.player.transform.position;
            Keys(Key.W);
            await Seconds(0.6f);
            Keys(); await Frames(2);
            Check(game.player.transform.position.z > start.z + 0.7f, "WASD moves CharacterController");
            Check(game.player.transform.position.y > -0.1f, "Floor collider supports player");

            game.player.Teleport(new Vector3(0, 0.05f, 2), 90);
            Keys(Key.W); await Seconds(1.1f); Keys(); await Frames(2);
            Check(game.player.transform.position.x < 1.2f, "Walls block player movement");

            game.player.Teleport(new Vector3(0.1f, 0.05f, 0), 90);
            game.kitchenDoor.SetOpen(false);
            game.player.AimAt(new Vector3(1.5f, 1.2f, 0));
            await Frames(3);
            Check(game.player.Focused == game.kitchenDoor, "Interaction ray finds kitchen door");
            Keys(Key.E); await Frames(2); Keys(); await Seconds(0.8f);
            Check(game.kitchenDoor.isOpen, "E opens hinged door");
            Keys(Key.W); await Until(() => game.player.transform.position.x > 2.7f, 2); Keys(); await Frames(2);
            Check(game.player.transform.position.x > 2.5f, "Open doorway is physically passable");
            bool light = game.player.flashlight.enabled;
            Keys(Key.F); await Frames(2); Keys(); await Frames(2);
            Check(game.player.flashlight.enabled != light, "F toggles flashlight");

            game.player.Teleport(new Vector3(-5.5f, 0.05f, -1.8f), 0);
            game.player.AimAt(game.evidence[0].transform.position);
            await Frames(3);
            Check(game.player.Focused == game.evidence[0], "Tabletop clue is reachable by interaction ray");
            Keys(Key.E); await Frames(2); Keys(); await Frames(2);
            Check(game.Found.Contains(0) && game.hud.Mode == DemoHud.Screen.Evidence, "E opens clue and records evidence");
            int count = game.Found.Count;
            game.Inspect(game.evidence[0]);
            Check(game.Found.Count == count, "Repeated inspection does not duplicate evidence");
            Click("ВЕРНУТЬСЯ [ ESC ]"); await Frames(2);
            Keys(Key.Tab); await Frames(2); Keys(); await Frames(2);
            Check(game.hud.Mode == DemoHud.Screen.Board, "Tab opens detective board");
            game.hud.Back(); await Frames(2);
            Check(!game.Blocked, "Board closes and restores control");

            for (int i = 1; i < 3; i++)
            {
                game.player.Teleport(new Vector3(i == 1 ? -6.7f : -4.8f, 0.05f, 8.8f), 0);
                game.player.AimAt(game.evidence[i].transform.position);
                await Frames(3);
                Check(game.player.Focused == game.evidence[i], "Study clue " + i + " is physically reachable");
                Keys(Key.E); await Frames(2); Keys(); await Frames(2);
                game.hud.Play();
            }
            Check(game.Found.Count == 3, "All three evidence records are present");
            game.Talk();
            game.Present(0);
            Check(game.Phase == DemoGame.Stage.Explore, "Wrong evidence allows another attempt");
            game.Present(2);
            Click("ПРОДОЛЖИТЬ"); await Frames(2);
            Check(game.Phase == DemoGame.Stage.Changed, "Correct evidence changes house state");
            Check(game.hallLights[0].color.r > game.hallLights[0].color.g * 3, "Changed hallway has red emergency lighting");

            game.UseFuseBox();
            Check(game.Phase == DemoGame.Stage.Chase && game.threat.gameObject.activeSelf, "Fuse box starts active threat");
            game.hud.ShowPause();
            Vector3 frozen = game.threat.position;
            float chaseTime = game.ChaseElapsed;
            await Seconds(0.4f);
            Check(game.threat.position == frozen && game.ChaseElapsed == chaseTime, "Pause freezes pursuer and chase timer");
            game.hud.Play();
            await Until(() => game.Phase == DemoGame.Stage.Dead, 6);
            Check(game.hud.Mode == DemoHud.Screen.Death, "Pursuer catches stationary player and shows defeat");
            Click("ПОВТОРИТЬ ПОГОНЮ"); await Frames(2);
            Check(game.Phase == DemoGame.Stage.Chase && game.Found.Count == 3 && game.ChaseElapsed < 0.6f, "Checkpoint resets chase and preserves evidence");

            // Actual sprint input along the known corridor, then turn through kitchen doorway.
            Keys(Key.W, Key.LeftShift);
            await Until(() => game.player.transform.position.z < 0.5f, 4);
            Keys(); await Frames(2);
            Check(game.Phase == DemoGame.Stage.Chase, "Sprint outruns pursuer along corridor");
            game.player.AimAt(new Vector3(6, 1.65f, 0));
            Keys(Key.W, Key.LeftShift);
            await Until(() => game.InKitchen, 2);
            Keys(); await Frames(2);
            game.player.AimAt(game.kitchenDoor.GetComponentInChildren<BoxCollider>().bounds.center);
            await Frames(3);
            Check(game.player.Focused == game.kitchenDoor, "Kitchen door can be reached from safe side");
            Keys(Key.E); await Frames(2); Keys(); await Frames(2);
            Check(game.Phase == DemoGame.Stage.Complete && !game.threat.gameObject.activeSelf, "Closing kitchen door ends chase safely");
            Check(game.hud.Mode == DemoHud.Screen.Ending, "Playable loop reaches ending");
            File.WriteAllText("Docs/verification.txt", "PASS — " + DateTime.UtcNow.ToString("u") + "\n" + string.Join("\n", passed));
            return passed.ToArray();
        }
        catch (Exception ex)
        {
            File.WriteAllText("Docs/verification.txt", "FAIL — " + ex.Message + "\n" + string.Join("\n", passed));
            throw;
        }
        finally
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        }
    }

    static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception("CHECK FAILED: " + description);
        passed.Add("PASS  " + description);
    }
    static void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    static void Click(string label)
    {
        foreach (var button in UnityEngine.Object.FindObjectsByType<Button>())
            if (button.gameObject.activeInHierarchy && button.GetComponentInChildren<Text>().text == label)
            { button.onClick.Invoke(); return; }
        throw new Exception("Button not found: " + label);
    }
    static Task Frames(int count)
    {
        int start = Time.frameCount;
        return Wait(() => Time.frameCount >= start + count, 4);
    }
    static Task Seconds(float seconds)
    {
        double start = EditorApplication.timeSinceStartup;
        return Wait(() => EditorApplication.timeSinceStartup >= start + seconds, seconds + 3);
    }
    static Task Until(Func<bool> predicate, float timeout) => Wait(predicate, timeout);
    static Task Wait(Func<bool> predicate, double timeout)
    {
        var completion = new TaskCompletionSource<bool>();
        double start = EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            try
            {
                if (predicate()) { EditorApplication.update -= tick; completion.TrySetResult(true); }
                else if (EditorApplication.timeSinceStartup - start > timeout)
                { EditorApplication.update -= tick; completion.TrySetException(new Exception("Timed out waiting for verification condition. Phase: " + game.Phase)); }
            }
            catch (Exception ex) { EditorApplication.update -= tick; completion.TrySetException(ex); }
        };
        EditorApplication.update += tick;
        return completion.Task;
    }
}
