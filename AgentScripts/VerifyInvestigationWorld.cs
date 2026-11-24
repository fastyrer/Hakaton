using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hakaton.Demo;
using Hakaton.Demo.Investigation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
public static class VerifyInvestigationWorld
{
    static List<string> passed=new List<string>();static DemoGame game;static InvestigationDirector d;static Keyboard keyboard;static List<InputDevice> suspended=new List<InputDevice>();
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);passed.Add(s);}
    static async Task Frames(int n=3){int target=Time.frameCount+n;while(Time.frameCount<target)await Task.Yield();}
    static async Task Seconds(float n){float end=Time.realtimeSinceStartup+n;while(Time.realtimeSinceStartup<end)await Task.Yield();}
    static void Bind(){game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();d=game.GetComponent<InvestigationDirector>();game.hud.Play();}
    static void Finish(){int s=100;while(d.ConversationOpen&&s-->0)d.NextLine();}
    static async Task Scene(string name){SceneManager.LoadScene(name);await Frames(5);Bind();}
    static async Task Reach(string id,Vector3 from)
    {
        var item=UnityEngine.Object.FindObjectsByType<CaseInteraction>(FindObjectsInactive.Exclude).First(x=>x.id==id);game.hud.Play();game.player.Teleport(from,0);await Frames();game.player.AimAt(item.transform.position+(item.action==CaseInteraction.ActionKind.Person?Vector3.up*1.2f:Vector3.zero));await Frames();
        Check(game.player.Focused==item,id+" reachable by unobstructed interaction ray in "+SceneManager.GetActiveScene().name);
    }
    static async Task Walk(Vector3 to,float timeout=5)
    {
        game.hud.Play();float until=Time.realtimeSinceStartup+timeout;
        while(Vector2.Distance(new Vector2(game.player.transform.position.x,game.player.transform.position.z),new Vector2(to.x,to.z))>.22f&&Time.realtimeSinceStartup<until)
        {
            Vector3 delta=to-game.player.transform.position;game.player.transform.rotation=Quaternion.LookRotation(new Vector3(delta.x,0,delta.z));InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));await Frames(1);
        }
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(1);Check(Vector2.Distance(new Vector2(game.player.transform.position.x,game.player.transform.position.z),new Vector2(to.x,to.z))<.4f,"Walkable route to "+to);
    }
    public static async Task<string[]> Main()
    {
        passed.Clear();keyboard=InputSystem.AddDevice<Keyboard>("World test keyboard");foreach(var device in InputSystem.devices)if(device!=keyboard&&device.enabled&&(device is Keyboard||device is Mouse)){suspended.Add(device);InputSystem.DisableDevice(device);}
        var behavior=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;string previous=CaseFile.TestPath;
        try
        {
            CaseFile.TestPath="/tmp/hakaton-investigation-world-test.json";CaseFile.NewGame();Bind();
            d.State.Complete(3);d.State.Complete(8);d.State.Complete(9);d.State.Complete(21);d.State.Complete(22);d.State.Complete(23);d.State.Complete(24);d.State.Complete(25);d.State.figures.AddRange(new[]{"X","Y","Z","K"});d.State.SetFlag("diamond_key");d.State.SetFlag("cctv_open");d.State.SetFlag("vault_open");d.ApplyWorld();await Seconds(2);
            await Reach("clock",new Vector3(-6.5f,.08f,8.3f));
            await Reach("E01",new Vector3(-5.8f,.08f,10.7f));await Reach("E02",new Vector3(-4.0f,.08f,14.5f));await Reach("E03",new Vector3(-4.4f,.08f,13.6f));await Reach("E13",new Vector3(-6.7f,.08f,5.5f));
            await Reach("E12",new Vector3(-10.3f,.08f,12.7f));await Reach("E10",new Vector3(10.0f,-3.32f,25.3f));await Reach("E11",new Vector3(10.9f,-3.32f,25.3f));
            var chase=d.GetComponent<CaseChase>();d.State.flags.Remove("chase_survived");chase.Begin();
            foreach(var door in UnityEngine.Object.FindObjectsByType<DemoDoor>(FindObjectsInactive.Exclude).Where(x=>x.label=="Кухонный проход"||x.label=="Кабинет отца"))door.SetOpen(true);
            await Walk(new Vector3(-4.5f,0,14.5f));await Walk(new Vector3(0,0,14.5f));await Walk(new Vector3(4,0,14f));await Walk(new Vector3(7.5f,0,14));chase.kitchen.SetOpen(false);await Frames(4);Check(!chase.Active&&d.State.Flag("chase_survived"),"Physical chase can be escaped by reaching kitchen and closing door");
            await Scene("PoliceStation");d.State.Complete(16);d.State.Complete(17);d.ApplyWorld();
            await Reach("E04",new Vector3(7,.08f,25.8f));await Reach("E05",new Vector3(10,.08f,26.1f));await Reach("E06",new Vector3(13,.08f,25.8f));await Reach("E07",new Vector3(16.6f,.08f,26.3f));
            foreach(var pair in new[]{("IvanHome","Ivan"),("DmitryHome","Dmitry"),("Church","Priest")})
            {
                await Scene(pair.Item1);foreach(var door in UnityEngine.Object.FindObjectsByType<DemoDoor>(FindObjectsInactive.Exclude))door.SetOpen(true);await Walk(new Vector3(6,0,2.2f));await Walk(new Vector3(6,0,5.6f));await Reach(pair.Item2,new Vector3(6,.08f,5.6f));
            }
            await Scene("AlexeyHome");await Reach("E08",new Vector3(4.4f,.08f,6.8f));await Reach("E09",new Vector3(6,.08f,6.8f));
            return passed.ToArray();
        }
        finally
        {
            if(d!=null)d.enabled=false;
            InputSystem.RemoveDevice(keyboard);foreach(var device in suspended)if(device.added)InputSystem.EnableDevice(device);suspended.Clear();InputSystem.settings.backgroundBehavior=behavior;
            CaseFile.TestPath=previous;CaseFile.Reload();
        }
    }
}
