using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hakaton.Demo;
using Hakaton.Demo.CarIntro;
using Hakaton.Demo.Menu;
public static class VerifyCarIntro
{
    static List<string> checks=new List<string>();
    public static async Task<string[]> Main()
    {
        checks.Clear();var s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();if(!Application.isPlaying||s==null)throw new Exception("Start CarIntro");s.externalClock=true;var d=s.picture;
        Check(d.Duration==CarIntroSequence.Length&&s.cameraShots.Length==7,"Seven cinematic flights fill the complete 31.5-second musical edit");
        Check(d.music.clip.name.Contains("Deception")&&d.music.clip.length>CarIntroSequence.Length&&d.musicFadeIn==0,"Original Deception plays continuously from its opening");
        Check(d.shots.All(x=>x.role==""&&x.label=="")&&d.shots.Take(6).All(x=>x.name==""),"Cutscene contains no monologue, dialogue subtitles or story spoilers");
        Check(UnityEngine.Object.FindObjectsByType<AudioListener>().Length==1&&UnityEngine.Object.FindObjectsByType<DemoPlayer>().Length==0,"Only the cinematic listener is active; gameplay and HUD are disabled");
        float cursor=0;foreach(var shot in s.cameraShots){Check(Mathf.Abs(shot.start-cursor)<.001f,"Continuous edit: "+shot.label);cursor+=shot.duration;}
        s.RenderAt(1);var a=s.car.position;var w=s.wiperLeft.localRotation;var wheel=s.wheels[0].localRotation;s.RenderAt(2);
        Check(Vector3.Distance(a,s.car.position)>4,"Car travels along the actual forest road");Check(Quaternion.Angle(w,s.wiperLeft.localRotation)>5,"Wipers move on the cinematic clock");Check(Quaternion.Angle(wheel,s.wheels[0].localRotation)>20,"Car wheels rotate on the cinematic clock");
        s.RenderAt(1);Check(Vector3.Distance(a,s.car.position)<.001f&&Quaternion.Angle(w,s.wiperLeft.localRotation)<.01f,"Seeking produces deterministic car and wiper poses");
        Check((d.worldCamera.cullingMask&(1<<8))==0,"Interior view hides the driver's head while retaining hands and dashboard");s.RenderAt(5);Check((d.worldCamera.cullingMask&(1<<8))!=0,"Exterior views show the full driver model");
        foreach(var shot in s.cameraShots)
        {
            s.RenderAt(shot.start+shot.duration*.5f);var p=d.worldCamera.transform.position;s.RenderAt(shot.start+shot.duration*.75f);Check(Vector3.Distance(p,d.worldCamera.transform.position)>.1f,"Camera flies through shot: "+shot.label);
        }
        Check(d.particles[0].particleCount>100,"Real rain particles render around the moving car");
        s.HoldForMonologue();await Delay(.1);Check(!s.IsPlaying&&!d.music.isPlaying&&s.CurrentTime==0&&d.fade.color.a==0,"Manual dialogue can hold on a visible, stationary cabin frame with no music");
        s.externalClock=false;s.BeginAfterMonologue();await Delay(.3);Check(s.CurrentTime>.1f&&d.music.isPlaying,"Explicit post-monologue hook starts camera and original music together");s.TogglePause();float frozen=s.CurrentTime;await Delay(.25);Check(s.Paused&&Mathf.Abs(s.CurrentTime-frozen)<.02f&&!d.music.isPlaying,"Pause freezes both music and vehicle");s.TogglePause();await Delay(.25);Check(!s.Paused&&s.CurrentTime>frozen+.1f&&d.music.isPlaying,"Resume continues the cinematic and music");
        s.Finish();await Until(()=>SceneManager.GetActiveScene().name==DemoGame.HomeScene&&UnityEngine.Object.FindAnyObjectByType<DemoGame>()?.hud.Mode==DemoHud.Screen.Play,15);Check(SceneManager.GetActiveScene().name=="FatherHouse","Skipping the cinematic enters the playable father house");
        Time.timeScale=1;SceneManager.LoadScene("MainMenu");await Until(()=>UnityEngine.Object.FindAnyObjectByType<MainMenu>()!=null,15);var menu=UnityEngine.Object.FindAnyObjectByType<MainMenu>();menu.StartGame();await Until(()=>SceneManager.GetActiveScene().name=="CarIntro"&&UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>()!=null,15);s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();await Delay(.2);Check(s.IsPlaying&&s.picture.music.clip.name.Contains("Deception"),"New Game starts the new car cinematic");
        // Advance the clock close to its actual end and exercise the automatic handoff.
        var held=typeof(CarIntroSequence).GetField("held",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);held.SetValue(s,CarIntroSequence.Length-.3f);await Until(()=>SceneManager.GetActiveScene().name=="FatherHouse"&&UnityEngine.Object.FindAnyObjectByType<DemoGame>()?.hud.Mode==DemoHud.Screen.Play,15);Check(UnityEngine.Object.FindAnyObjectByType<DemoGame>().Found.Count==0,"Natural end enters the house with a fresh investigation");
        File.WriteAllText("Docs/CarIntro/verification.txt","PASS\n"+string.Join("\n",checks));return checks.ToArray();
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS  "+message);}
    static Task Delay(double seconds){double start=EditorApplication.timeSinceStartup;return Until(()=>EditorApplication.timeSinceStartup-start>=seconds,seconds+4);}
    static Task Until(Func<bool> test,double timeout){var t=new TaskCompletionSource<bool>();double start=EditorApplication.timeSinceStartup;EditorApplication.CallbackFunction f=null;f=()=>{try{if(test()){EditorApplication.update-=f;t.TrySetResult(true);}else if(EditorApplication.timeSinceStartup-start>timeout){EditorApplication.update-=f;t.TrySetException(new Exception("Timeout"));}}catch(Exception e){EditorApplication.update-=f;t.TrySetException(e);}};EditorApplication.update+=f;return t.Task;}
}
