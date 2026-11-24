using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hakaton.Demo;
using Hakaton.Demo.Investigation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
public static class VerifyInvestigationPlay
{
    static List<string> passed=new List<string>();static DemoGame game;static InvestigationDirector d;static Keyboard keyboard;static List<InputDevice> suspended=new List<InputDevice>();
    static void Check(bool ok,string title){if(!ok)throw new Exception(title);passed.Add(title);}
    static async Task Frames(int count=3){int target=Time.frameCount+count;while(Time.frameCount<target)await Task.Yield();}
    static async Task Tap(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));await Frames();InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames();}
    static void Click(string text){var b=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).First(b=>b.GetComponentsInChildren<Text>().Any(t=>t.text==text));b.onClick.Invoke();}
    static async Task CloseDialogue(){int scene=d.CurrentScene;int safety=250;while(d.ConversationOpen&&d.CurrentScene==scene&&safety-->0){d.NextLine();await Frames(1);}}
    public static async Task<string[]> Main()
    {
        passed.Clear();game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();d=game.GetComponent<InvestigationDirector>();
        if(!Application.isPlaying||d==null)throw new Exception("Start FatherHouse in Play first");
        keyboard=InputSystem.AddDevice<Keyboard>("Case test keyboard");foreach(var device in InputSystem.devices)if(device!=keyboard&&device.enabled&&(device is Keyboard||device is Mouse)){suspended.Add(device);InputSystem.DisableDevice(device);}
        var behavior=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        string previous=CaseFile.TestPath;
        try
        {
            CaseFile.TestPath="/tmp/hakaton-investigation-play-test.json";CaseFile.NewGame();d.State.Complete(3);game.hud.Play();
            await Tap(Key.Tab);Check(game.hud.Mode==DemoHud.Screen.Board&&game.hud.CaseBoard!=null,"Tab opens case board and pauses world");
            var playerPosition=game.player.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));await Frames(8);InputSystem.QueueStateEvent(keyboard,new KeyboardState());Check(game.player.transform.position==playerPosition,"Movement stays blocked while using board");
            Click("НОВАЯ ЗАМЕТКА");await Frames();var note=d.State.cards.Single();var inputs=UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsInactive.Exclude);inputs.OrderBy(x=>x.GetComponent<RectTransform>().sizeDelta.y).First().text="Моя гипотеза";inputs.OrderByDescending(x=>x.GetComponent<RectTransform>().sizeDelta.y).First().text="Проверить все документы";
            await Tap(Key.Tab);Check(note.title=="Моя гипотеза"&&note.text=="Проверить все документы","Closing Tab commits edited Cyrillic note");
            var clue=UnityEngine.Object.FindObjectsByType<CaseInteraction>(FindObjectsInactive.Exclude).Single(x=>x.id=="E01");
            game.player.Teleport(new Vector3(-5.8f,.08f,10.7f),0);game.player.AimAt(clue.transform.position);await Frames();
            Check(game.player.Focused==clue,"Study evidence is reachable by physical player ray");await Tap(Key.E);Check(d.State.Has("E01")&&d.State.Studied("E01"),"E inspects world evidence and registers found/studied flags");
            Click("ПРИКРЕПИТЬ НА ДОСКУ");await Frames();Check(d.State.cards.Any(c=>c.id=="E01"),"Found evidence can be pinned from inspection");
            game.hud.Play();
            d.State.Study("E05");d.State.Study("E06");d.State.Pin("E05");d.State.Pin("E06");game.hud.ShowBoard();var view=game.hud.CaseBoard;view.Select("E05");Click("СВЯЗАТЬ С…");view.Select("E06");await Frames();Check(d.State.D("D01_CALIBER_MISMATCH"),"Board interaction links two selected cards and confirms D01");
            view=game.hud.CaseBoard;var card=d.State.cards.Single(c=>c.id=="E05");var origin=card.position;view.SetZoom(.6f);view.Move("E05",new Vector2(60,-30));view.Save();Check(Vector2.Distance(card.position,origin+new Vector2(100,50))<.1f,"Card drag respects board zoom");
            game.hud.Play();d.State.Study("E07");d.State.Complete(16);Check(d.State.Ready(17),"E07 unlocks scene17 without optional hypothesis");d.Speak(17);await CloseDialogue();await Frames();
            // Event queue must wait while the board is open, then dispatch the prescribed order.
            foreach(var e in CaseCatalog.Clues)d.State.Study(e.id);d.State.Complete(9);
            d.State.Complete(24);d.State.Complete(25);d.State.Link("E04","E10");d.State.Link("E09","E11");d.State.Link("D01_CALIBER_MISMATCH","E08");
            game.hud.ShowBoard();await Frames(4);Check(!d.ConversationOpen,"Story events wait until board closes");game.hud.Play();await Frames(4);Check(d.CurrentScene==26,"True-date scene has priority over queued recognition");await CloseDialogue();await Frames(4);Check(d.CurrentScene==27,"Recognition follows true-date conversation");await CloseDialogue();await Frames(4);Check(d.CurrentScene==28,"Letter flashback follows recognition");await CloseDialogue();await Frames(4);Check(d.CurrentScene==29,"Murder flashback follows letter");await CloseDialogue();await Frames(4);Check(d.CurrentScene==30,"Confession follows murder flashback");await CloseDialogue();await Frames(4);Check(d.State.Ready(31)&&!d.ConversationOpen,"Final choice unlocks after full sequence only");
            d.Speak(31);await CloseDialogue();d.State.branch="capsule";d.Speak(35);await CloseDialogue();d.Speak(36);await CloseDialogue();Check(d.State.ending=="capsule","Capsule ending reaches finished state");
            d.State.ending="";d.State.branch="truth";d.Speak(32);await CloseDialogue();d.State.SetFlag("proof_delivered");d.Speak(33);await CloseDialogue();d.Speak(34);await CloseDialogue();Check(d.State.ending=="truth","Evidence ending reaches finished state");
            d.Save();string id=note.id;CaseFile.Reload();Check(d.State.cards.Any(c=>c.id==id)&&d.State.Done(30)&&d.State.ending=="truth","Play-mode disk reload preserves board and completed ending");
            return passed.ToArray();
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);foreach(var device in suspended)if(device.added)InputSystem.EnableDevice(device);suspended.Clear();InputSystem.settings.backgroundBehavior=behavior;
            CaseFile.TestPath=previous;CaseFile.Reload();game.hud.Play();
        }
    }
}
