using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hakaton.Demo;
using Hakaton.LocationKit;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class VerifyStation
{
    static readonly List<string> passed = new List<string>();
    static Keyboard keyboard;
    static Mouse mouse;
    static DemoGame game;
    static readonly List<InputDevice> suspendedDevices = new List<InputDevice>();
    public static async Task<string[]> Main()
    {
        passed.Clear();
        game = UnityEngine.Object.FindAnyObjectByType<DemoGame>();
        if (!Application.isPlaying || SceneManager.GetActiveScene().name != "PoliceStation") throw new Exception("Start PoliceStation in Play Mode.");
        keyboard = InputSystem.AddDevice<Keyboard>("Station verification keyboard");
        mouse = InputSystem.AddDevice<Mouse>("Station verification mouse");
        var background = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        foreach (var device in InputSystem.devices)
            if (device != keyboard && device != mouse && device.enabled && (device is Keyboard || device is Mouse))
            { suspendedDevices.Add(device); InputSystem.DisableDevice(device); }
        try
        {
            game.hud.Play(); await Frames(4);
            int frame = Time.frameCount; await Frames(5);
            Check(Time.frameCount > frame, "Play loop advances");
            Check(game.locationTitle == "Полицейский участок", "Custom station introduction and objective are connected");
            Check(UnityEngine.Object.FindObjectsByType<StationNpc>().Length == 32, "Station contains 32 NPCs");
            var walker = UnityEngine.Object.FindObjectsByType<StationNpc>().First(n => n.route != null && n.route.Length > 1);
            var start = walker.transform.position; await Seconds(.4f);
            Check(Vector3.Distance(start,walker.transform.position) > .1f, "Patrolling NPC actually moves");
            var phones=UnityEngine.Object.FindAnyObjectByType<ArrivalHeadphones>();
            Check(phones!=null&&phones.track!=null&&phones.IsPlaying,"Living Bones plays on arrival through headphones");
            Check(phones.GetComponent<AudioSource>().spatialBlend==0,"Headphone music remains non-spatial");
            var weather = UnityEngine.Object.FindAnyObjectByType<DemoWeather>();
            Check(weather != null && weather.rain.isPlaying && !weather.IsSheltered, "Arrival starts outdoors in rain");
            game.player.Teleport(new Vector3(0,.08f,-1.7f),0);
            game.player.AimAt(new Vector3(0,1.2f,0)); await Frames(4);
            var entrance=game.player.Focused as DemoDoor;
            Check(entrance != null && !entrance.isOpen,"Entrance door is reachable and closed");
            Keys(Key.W);await Seconds(.8f);Keys();await Frames(3);
            Check(game.player.transform.position.z < -.25f,"Entrance door blocks movement while closed");
            Keys(Key.E);await Frames(2);Keys();await Seconds(.8f);
            Check(entrance.isOpen,"E opens the station entrance");
            Keys(Key.W);await Seconds(1.1f);Keys();await Frames(3);
            Check(game.player.transform.position.z > .5f,"Player can enter the lobby on foot");
            Check(weather.IsSheltered,"Indoor shelter muffles the rain");
            game.player.Teleport(new Vector3(1.4f,.08f,30),90);
            game.player.AimAt(new Vector3(3,1.2f,30));await Frames(4);
            var archiveDoor=game.player.Focused as DemoDoor;
            Check(archiveDoor != null && archiveDoor.label.Contains("АРХИВ"),"Archive door is accessible from the central corridor");
            Keys(Key.E);await Frames(2);Keys();await Seconds(.8f);
            Keys(Key.W);await Seconds(1.5f);Keys();await Frames(3);
            Check(game.player.transform.position.x>3.6f,"Player can walk into the archive");
            for(int id=3;id<6;id++)
            {
                var clue=game.evidence[id];
                game.player.Teleport(new Vector3(clue.transform.position.x,.08f,25.65f),0);
                game.player.AimAt(clue.transform.position);await Frames(4);
                Check(game.player.Focused==clue,"Document "+id+" is reachable by the interaction ray");
                Keys(Key.E);await Frames(2);Keys();await Frames(3);
                Check(game.Found.Contains(id)&&game.hud.Mode==DemoHud.Screen.Evidence,"Document "+id+" enters the inventory and opens for reading");
                game.hud.Play();await Frames(2);
                Check(game.board.AddEvidence(id,out _),"Document "+id+" pins to the detective board");
            }
            Check(DemoGame.EvidenceText[4].Contains(".45 ACP")&&DemoGame.EvidenceText[5].Contains("9×19 мм"),"Weapon and recovered bullet have explicitly different calibers");
            Check(DemoGame.EvidenceText[3].Contains("06:40")&&DemoGame.EvidenceText[3].Contains("38 см"),"Body discovery record specifies when and the nearby pistol");
            Keys(Key.Tab);await Frames(2);Keys();await Frames(3);
            Check(game.hud.Mode==DemoHud.Screen.Board,"Tab opens the board inside the station");
            Check(game.board.HasEvidence(3)&&game.board.HasEvidence(4)&&game.board.HasEvidence(5),"All three archive documents appear on the board");
            game.hud.Play();game.board.AddNote("Несовпадение калибров","Пистолет .45 ACP; извлечённая пуля 9×19 мм. Где второе оружие?",5,out var note);
            game.board.Move(note.id,new Vector2(215,190));
            int count=game.board.Entries.Count;
            game.SelectDestination("Дом Волковых");
            await Until(()=>SceneManager.GetActiveScene().name=="HouseArrival"&&UnityEngine.Object.FindAnyObjectByType<DemoGame>()?.hud.Mode==DemoHud.Screen.Intro,12);
            game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();
            Check(game.Found.IsSupersetOf(new[]{3,4,5})&&game.board.Entries.Count==count,"Return home preserves inventory and all board cards");
            Check(game.board.Entries.Any(e=>e.title=="Несовпадение калибров"&&e.position==new Vector2(215,190)),"Personal note and card position survive travel");
            game.hud.Play();for(int id=0;id<3;id++){game.Inspect(game.evidence[id]);game.hud.Play();game.board.AddEvidence(id,out _);}
            Check(game.Found.Count==6,"House and archive evidence coexist in inventory");
            game.hud.ShowBoard();await Frames(3);
            var scroll=UnityEngine.Object.FindObjectsByType<ScrollRect>().First(x=>x.content!=null&&x.content.name=="Inventory content");
            Check(scroll.content.rect.height>scroll.viewport.rect.height,"Six-item inventory scrolls instead of overflowing");
            scroll.verticalNormalizedPosition=0;await Frames(2);
            Check(scroll.content.GetComponentsInChildren<Button>().Length==12,"All six documents remain accessible in inventory");
            game.hud.Play();game.SelectDestination("Полицейский участок");
            await Until(()=>SceneManager.GetActiveScene().name=="PoliceStation"&&UnityEngine.Object.FindAnyObjectByType<DemoGame>()?.hud.Mode==DemoHud.Screen.Intro,12);
            game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();
            Check(game.Found.Count==6&&game.board.HasEvidence(0)&&game.board.HasEvidence(5),"Travel from the house to station preserves evidence from both locations");
            Check(game.EvidenceIllustration(0)!=null&&game.EvidenceIllustration(5)!=null,"Evidence illustrations survive travel");
            game.hud.Play();game.player.Teleport(new Vector3(0,.08f,5),0);game.player.AimAt(new Vector3(0,1.6f,25));await Frames(4);
            File.WriteAllText("Docs/station-verification.txt","PASS — "+DateTime.UtcNow.ToString("u")+"\n"+string.Join("\n",passed));
            return passed.ToArray();
        }
        catch(Exception ex){File.WriteAllText("Docs/station-verification.txt","FAIL — "+ex.Message+"\n"+string.Join("\n",passed));throw;}
        finally
        {
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            if(mouse!=null)InputSystem.RemoveDevice(mouse);
            foreach(var device in suspendedDevices)if(device.added)InputSystem.EnableDevice(device);
            suspendedDevices.Clear();InputSystem.settings.backgroundBehavior=background;
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
                { EditorApplication.update -= tick; done.TrySetException(new Exception("Timed out waiting for station check.")); }
            }
            catch (Exception ex) { EditorApplication.update -= tick; done.TrySetException(ex); }
        };
        EditorApplication.update += tick;
        return done.Task;
    }
}
