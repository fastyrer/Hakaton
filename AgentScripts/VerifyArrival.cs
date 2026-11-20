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

public static class VerifyArrival
{
    static readonly List<string> passed = new List<string>();
    static Keyboard keyboard;
    static Mouse mouse;
    static readonly List<InputDevice> suspendedDevices = new List<InputDevice>();
    static DemoGame game;

    public static async Task<string[]> Main()
    {
        passed.Clear();
        game = UnityEngine.Object.FindAnyObjectByType<DemoGame>();
        if (!Application.isPlaying || game == null || !game.arrivalScene) throw new Exception("Open HouseArrival in Play Mode first.");
        keyboard = InputSystem.AddDevice<Keyboard>("Arrival verification keyboard");
        mouse = InputSystem.AddDevice<Mouse>("Arrival verification mouse");
        var background = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        suspendedDevices.Clear();
        foreach (var device in InputSystem.devices)
            if (device != keyboard && device != mouse && device.enabled && (device is Keyboard || device is Mouse))
            { suspendedDevices.Add(device); InputSystem.DisableDevice(device); }
        try
        {
            Check(game.hud.Mode == DemoHud.Screen.Intro && game.player.transform.position.z < -10, "Starts outside the house");
            Click("НАЧАТЬ"); await Frames(4);
            Check(!game.Blocked && Time.timeScale == 1, "Start button restores movement");
            int frame = Time.frameCount; await Frames(5);
            Check(Time.frameCount > frame, "Player loop is advancing");
            var weather = UnityEngine.Object.FindAnyObjectByType<DemoWeather>();
            Check(RenderSettings.fog && RenderSettings.fogEndDistance > 30, "Exterior fog is active");
            Check(weather != null && weather.rain.isPlaying && weather.rain.particleCount > 100, "Rain is emitting visible particles");
            Check(!weather.IsSheltered, "Initial yard position is outdoors");
            var entrance = GameObject.Find("Front entrance · working door").GetComponent<DemoDoor>();
            game.player.Teleport(new Vector3(0, 0.12f, -7.5f), 0);
            game.player.AimAt(new Vector3(0, 1.25f, -5.05f));
            Keys(Key.W); await Seconds(1.3f); Keys(); await Frames(3);
            Check(game.player.transform.position.z < -5.3f, "Closed front door physically blocks entrance");
            Check(game.player.Focused == entrance, "Front door is reachable from the porch");
            Keys(Key.E); await Frames(2); Keys(); await Seconds(0.85f);
            Check(entrance.isOpen, "E opens front door");
            Keys(Key.W); await Seconds(1.3f); Keys(); await Frames(4);
            Check(game.player.transform.position.z > -4.5f && game.HasEnteredHouse, "Player can walk through doorway into house");
            Check(weather.IsSheltered, "Indoor position muffles rain audio");
            var particles = new ParticleSystem.Particle[5000];
            int n = weather.rain.GetParticles(particles), leaks = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = particles[i].position;
                if (Mathf.Abs(p.x) < 8.5f && p.z > -4.5f && p.z < 12.5f && p.y < 2.7f && p.y > 0) leaks++;
            }
            Check(leaks == 0, "Roof prevents rain from falling inside rooms");
            game.player.Teleport(new Vector3(0, 0.1f, -3.6f), 180);
            Keys(Key.W); await Until(() => game.player.transform.position.z < -10, 10); Keys(); await Frames(3);
            Check(!weather.IsSheltered, "Player can return from house to rainy yard");
            var car = GameObject.Find("03 · Patrol car / interact to travel").GetComponent<DemoInteractable>();
            game.player.Teleport(car.transform.TransformPoint(new Vector3(-2.7f, 0.1f, 0)), 0);
            game.player.AimAt(car.transform.TransformPoint(new Vector3(0, 1.25f, 0)));
            await Frames(4);
            Check(game.player.Focused == car, "Car can be targeted from its side");
            Keys(Key.W); await Seconds(0.7f); Keys(); await Frames(3);
            Check(car.transform.InverseTransformPoint(game.player.transform.position).x < -1.2f, "Car collider blocks walking through the body");
            Keys(Key.E); await Frames(3); Keys(); await Frames(2);
            Check(game.hud.Mode == DemoHud.Screen.Travel && game.Blocked && Time.timeScale == 0, "E opens destination menu and pauses movement");
            Vector3 frozen = game.player.transform.position;
            Keys(Key.W); await Seconds(0.25f); Keys(); await Frames(2);
            Check(Vector3.Distance(frozen, game.player.transform.position) < 0.01f, "Destination menu prevents player drift");
            Check(!Find("ДОМ ВОЛКОВЫХ · ВЫ ЗДЕСЬ").interactable, "Current location is marked and cannot be selected");
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            Click("ПОЛИЦЕЙСКИЙ УЧАСТОК"); await Frames(2);
            Check(game.SelectedDestination == "Полицейский участок" && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == scene, "Police station opens a placeholder without scene transition");
            Check(HasText("Локация пока недоступна"), "Placeholder clearly states location is unavailable");
            game.hud.Back(); await Frames(2);
            Click("ПРИДОРОЖНОЕ КАФЕ"); await Frames(2);
            Check(game.SelectedDestination == "Придорожное кафе" && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == scene, "Diner opens its placeholder without transition");
            Click("ОСТАТЬСЯ У ДОМА"); await Frames(3);
            Check(!game.Blocked && Time.timeScale == 1, "Leaving placeholder restores gameplay");
            game.player.AimAt(car.transform.TransformPoint(new Vector3(0, 1.25f, 0))); await Frames(3);
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
            await Frames(3);
            InputSystem.QueueStateEvent(mouse, new MouseState()); await Frames(2);
            Check(game.hud.Mode == DemoHud.Screen.Travel, "Left mouse click on car also opens destinations");
            Keys(Key.Escape); await Frames(2); Keys(); await Frames(2);
            Check(!game.Blocked && Time.timeScale == 1, "Escape closes travel menu and restores input");
            Keys(Key.S); await Seconds(0.3f); Keys(); await Frames(2);
            Check(Vector3.Distance(frozen, game.player.transform.position) > 0.2f, "Movement still works after repeated menu interactions");
            File.WriteAllText("Docs/arrival-verification.txt", "PASS — " + DateTime.UtcNow.ToString("u") + "\n" + string.Join("\n", passed));
            return passed.ToArray();
        }
        catch (Exception ex)
        {
            File.WriteAllText("Docs/arrival-verification.txt", "FAIL — " + ex.Message + "\n" + string.Join("\n", passed));
            throw;
        }
        finally
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (var device in suspendedDevices) if (device.added) InputSystem.EnableDevice(device);
            suspendedDevices.Clear();
            InputSystem.settings.backgroundBehavior = background;
        }
    }

    static void Check(bool ok, string message) { if (!ok) throw new Exception("CHECK FAILED: " + message); passed.Add("PASS  " + message); }
    static void Keys(params Key[] keys)
    {
        keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    }
    static Button Find(string label)
    {
        foreach (var b in UnityEngine.Object.FindObjectsByType<Button>())
            if (b.gameObject.activeInHierarchy && b.GetComponentInChildren<Text>().text == label) return b;
        throw new Exception("Button not found: " + label);
    }
    static void Click(string text) => Find(text).onClick.Invoke();
    static bool HasText(string text)
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<Text>()) if (t.gameObject.activeInHierarchy && t.text == text) return true;
        return false;
    }
    static Task Frames(int count) { int start = Time.frameCount; return Until(() => Time.frameCount >= start + count, 4); }
    static Task Seconds(float seconds) { double start = EditorApplication.timeSinceStartup; return Until(() => EditorApplication.timeSinceStartup >= start + seconds, seconds + 3); }
    static Task Until(Func<bool> predicate, double timeout)
    {
        var done = new TaskCompletionSource<bool>();
        double start = EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            try
            {
                if (predicate()) { EditorApplication.update -= tick; done.TrySetResult(true); }
                else if (EditorApplication.timeSinceStartup - start > timeout)
                { EditorApplication.update -= tick; done.TrySetException(new Exception("Timed out waiting for arrival check.")); }
            }
            catch (Exception ex) { EditorApplication.update -= tick; done.TrySetException(ex); }
        };
        EditorApplication.update += tick;
        return done.Task;
    }
}
