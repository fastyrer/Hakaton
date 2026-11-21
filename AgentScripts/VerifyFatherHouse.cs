using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Hakaton.Demo;
using Hakaton.Demo.FatherHouse;
using Hakaton.LocationKit;
public static class VerifyFatherHouse
{
    static List<string> passed=new List<string>();static Keyboard keyboard;static Mouse mouse;static DemoGame game;
    static List<InputDevice> suspended=new List<InputDevice>();
    public static async Task<string[]> Main()
    {
        passed.Clear();game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();
        if(!Application.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="FatherHouse")throw new Exception("Start FatherHouse in Play Mode");
        var background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard=InputSystem.AddDevice<Keyboard>("House test keyboard");mouse=InputSystem.AddDevice<Mouse>("House test mouse");suspended.Clear();
        foreach(var d in InputSystem.devices)if(d!=keyboard&&d!=mouse&&d.enabled&&(d is Keyboard||d is Mouse)){suspended.Add(d);InputSystem.DisableDevice(d);}
        try
        {
            game.hud.Play();await Frames(5);
            Check(DemoGame.HomeScene=="FatherHouse","New game and home destination resolve to FatherHouse");
            Check(UnityEngine.Object.FindObjectsByType<DemoPlayer>().Length==1&&UnityEngine.Object.FindObjectsByType<AudioListener>().Length==1,"Exactly one player and audio listener");
            Check(UnityEngine.Object.FindObjectsByType<HouseDocument>().Length==14,"Fourteen readable personal documents");
            Check(UnityEngine.Object.FindObjectsByType<HouseSecretDoor>().Length==4,"Four movable panels connect three hidden spaces");
            Check(UnityEngine.Object.FindObjectsByType<HousePuzzleAnchor>().Length==9,"Nine editable anchors reserve places for future puzzles");
            var root=UnityEngine.Object.FindAnyObjectByType<LocationRoot>();var weather=UnityEngine.Object.FindAnyObjectByType<DemoWeather>();
            game.player.Teleport(new Vector3(0,.08f,-14),0);await Seconds(.5f);Check(!weather.IsSheltered,"Forest yard is outdoors");
            var entrance=root.GetComponentsInChildren<DemoDoor>().First(d=>d.label=="Входная дверь");entrance.SetOpen(false);
            game.player.Teleport(new Vector3(0,.08f,-1.5f),0);game.player.AimAt(new Vector3(0,1.15f,0));await Frames(4);
            Check(game.player.Focused==entrance,"Front door can be focused from porch");Keys(Key.W);await Seconds(.8f);Keys();await Frames(3);
            Check(game.player.transform.position.z<-.25f,"Closed front door physically blocks player");await Tap(Key.E);await Seconds(.85f);Check(entrance.isOpen,"E opens front door");
            await Walk(new Vector3(0,0,2));Check(weather.IsSheltered,"Indoor rain sound is muffled");
            // Both flights and the landing are traversed with ordinary player input.
            game.player.Teleport(new Vector3(3,.08f,4.2f),0);await Walk(new Vector3(3,1.7f,10));Check(Mathf.Abs(game.player.transform.position.y-1.7f)<.25f,"First stair flight reaches physical landing");
            await Walk(new Vector3(5,1.7f,10));await Walk(new Vector3(5,3.4f,4.2f));Check(Mathf.Abs(game.player.transform.position.y-3.4f)<.25f,"Second flight reaches upstairs without teleport");
            await Walk(new Vector3(5,1.7f,10));await Walk(new Vector3(3,1.7f,10));await Walk(new Vector3(3,0,4.2f));Check(Mathf.Abs(game.player.transform.position.y)<.25f,"Main stairs can be descended without falling");
            root.GetComponentsInChildren<DemoDoor>().First(d=>d.label=="Спуск в подвал").SetOpen(true);
            game.player.Teleport(new Vector3(3.4f,.08f,17),0);await Walk(new Vector3(3.4f,-3.4f,24.8f));Check(Mathf.Abs(game.player.transform.position.y+3.4f)<.25f,"Basement stair descends through real floor opening");
            await Walk(new Vector3(3.4f,0,17));Check(Mathf.Abs(game.player.transform.position.y)<.25f,"Basement stair climbs back to ground floor");
            foreach(var secret in root.GetComponentsInChildren<HouseSecretDoor>())
            {
                secret.isOpen=false;await Seconds(1.7f);var t=secret.transform;var start=t.position-t.forward*1.1f+Vector3.up*.08f;
                game.player.Teleport(start,t.eulerAngles.y);game.player.AimAt(t.position+Vector3.up*1.2f);await Frames(4);
                Check(game.player.Focused==secret,"Secret panel can be focused: "+secret.name);await Tap(Key.E);await Seconds(1.7f);Check(secret.isOpen,"E slides secret panel: "+secret.name);
                await Walk(t.position+t.forward*1.05f);Check(Vector3.Dot(game.player.transform.position-t.position,t.forward)>.65f,"Player walks through secret opening: "+secret.name);
            }
            // Check all normal door openings for a full-height player capsule on either side.
            foreach(var door in root.GetComponentsInChildren<DemoDoor>())
            {
                bool was=door.isOpen;door.SetOpen(true);Physics.SyncTransforms();var f=door.transform.parent;bool clear=false;
                foreach(float offset in new[]{0f,-.38f,.38f})
                {
                    var s=f.position+f.right*offset-f.forward*.85f;var bottom=s+Vector3.up*.36f;var top=s+Vector3.up*1.48f;var delta=f.forward*1.7f;
                    bool occupied=Physics.OverlapCapsule(bottom,top,.24f,~0,QueryTriggerInteraction.Ignore).Any(c=>c.gameObject!=game.player.gameObject)||Physics.OverlapCapsule(bottom+delta,top+delta,.24f,~0,QueryTriggerInteraction.Ignore).Any(c=>c.gameObject!=game.player.gameObject);
                    if(!occupied&&!Physics.CapsuleCast(bottom,top,.24f,f.forward,1.7f,~0,QueryTriggerInteraction.Ignore)){clear=true;break;}
                }
                door.SetOpen(was);Check(clear,"Clear doorway: "+door.label);
            }
            var note=root.GetComponentsInChildren<HouseDocument>().First(d=>d.heading=="Не открывать окна");
            game.player.Teleport(new Vector3(-5.4f,.08f,6.8f),0);game.player.AimAt(note.transform.position);await Frames(4);Check(game.player.Focused==note,"Personal note is reachable on table");await Tap(Key.E);Check(game.hud.Mode==DemoHud.Screen.Document&&Time.timeScale==0,"E opens personal note and pauses movement");await Tap(Key.Tab);Check(game.hud.Mode==DemoHud.Screen.Board,"Tab opens detective board from note");await Tap(Key.Escape);Check(game.hud.Mode==DemoHud.Screen.Play&&Time.timeScale==1,"Escape returns to play");
            Check(game.evidence.Where(e=>e!=null).Select(e=>e.evidenceId).OrderBy(x=>x).SequenceEqual(new[]{0,1,2}),"House has three original family evidence items");
            foreach(var item in game.evidence.Where(e=>e!=null))
            {
                var p=item.transform.position;bool focused=false;
                foreach(var side in new[]{Vector3.right,Vector3.forward,Vector3.back,Vector3.left})
                {
                    game.player.Teleport(new Vector3(p.x,p.y-.94f+.08f,p.z)+side*1.25f,0);await Seconds(.3f);game.player.AimAt(p);await Frames(3);
                    if(game.player.Focused==item){focused=true;break;}
                }
                Check(focused,"Evidence reachable: "+item.label);await Tap(Key.E);Check(game.Found.Contains(item.evidenceId),"Evidence added to inventory: "+item.evidenceId);await Tap(Key.Escape);
            }
            game.board.AddEvidence(0,out var card);Check(card!=null,"Collected evidence can be added to board");
            game.player.Teleport(new Vector3(0,.08f,14),0);await Seconds(2);
            var particles=new ParticleSystem.Particle[5000];int n=weather.rain.GetParticles(particles);int leaks=particles.Take(n).Count(p=>Mathf.Abs(p.position.x)<11.7f&&p.position.z>.3f&&p.position.z<27.7f&&p.position.y>0&&p.position.y<6.4f);
            Check(n>100&&leaks==0,"Roof blocks all rain inside both floors");
            Check(weather.IsSheltered,"Central hall remains sheltered");
            File.WriteAllText("Docs/father-house-verification.txt","PASS\n"+string.Join("\n",passed));return passed.ToArray();
        }
        catch(Exception e){File.WriteAllText("Docs/father-house-verification.txt","FAIL "+e.Message+"\n"+string.Join("\n",passed));throw;}
        finally{Keys();InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);foreach(var d in suspended)if(d.added)InputSystem.EnableDevice(d);InputSystem.settings.backgroundBehavior=background;}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message+"; player="+game.player.transform.position);passed.Add("PASS  "+message);}
    static void Keys(params Key[] keys){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
    static async Task Tap(Key k){Keys(k);await Frames(3);Keys();await Frames(3);}
    static async Task Walk(Vector3 target)
    {
        double start=EditorApplication.timeSinceStartup;
        try
        {
            while(Vector2.Distance(new Vector2(game.player.transform.position.x,game.player.transform.position.z),new Vector2(target.x,target.z))>.14f)
            {
                Vector3 delta=target-game.player.transform.position;game.player.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,0);Keys(Key.W);await Frames(1);
                if(EditorApplication.timeSinceStartup-start>12)throw new Exception("Walk blocked toward "+target+" from "+game.player.transform.position);
            }
        }
        finally{Keys();await Frames(3);}
    }
    static Task Frames(int n){int f=Time.frameCount;return Until(()=>Time.frameCount>=f+n,12);}
    static Task Seconds(float n){double t=EditorApplication.timeSinceStartup;return Until(()=>EditorApplication.timeSinceStartup>=t+n,n+5);}
    static Task Until(Func<bool> predicate,double timeout)
    {
        var done=new TaskCompletionSource<bool>();double t=EditorApplication.timeSinceStartup;EditorApplication.CallbackFunction tick=null;
        tick=()=>{try{if(predicate()){EditorApplication.update-=tick;done.TrySetResult(true);}else if(EditorApplication.timeSinceStartup-t>timeout){EditorApplication.update-=tick;done.TrySetException(new Exception("Timeout"));}}catch(Exception e){EditorApplication.update-=tick;done.TrySetException(e);}};EditorApplication.update+=tick;return done.Task;
    }
}
