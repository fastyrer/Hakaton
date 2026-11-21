using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hakaton.Demo.Trailer;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifyTrailer
{
    static List<string> passed=new List<string>();
    public static async Task<string[]> Main()
    {
        passed.Clear();var d=UnityEngine.Object.FindAnyObjectByType<OpeningTrailer>();
        if(!Application.isPlaying||d==null)throw new Exception("Start OpeningTrailer in Play Mode");
        d.externalClock=true;
        Check(d.shots.Length==30&&Mathf.Abs(d.Duration-d.music.clip.length)<.001f,"Thirty shots cover the entire supplied track");
        Check(d.music.clip!=null&&d.music.clip.name.Contains("Tyler Bates")&&d.music.clip.name.Contains("Choice"),"The supplied Tyler Bates Frank s Choice audio clip is connected");
        for(int i=0;i<d.shots.Length;i++)
        {
            var shot=d.shots[i];Check(shot.duration>3&&Vector3.Distance(shot.from,shot.to)>.4f,"Shot "+i+" has its own camera flight");
            if(i>0)Check(Mathf.Abs(shot.start-(d.shots[i-1].start+d.shots[i-1].duration))<.001f,"Shot "+i+" follows without a timeline gap");
            d.RenderAt(shot.start+shot.duration*.55f);
            Check(d.CurrentShot==i&&d.characterName.text==(shot.title?"":shot.name),"Shot "+i+" shows the correct character caption");
        }
        foreach(string name in new[]{"АЛЕКСЕЙ","ИВАН","ДМИТРИЙ","ПАВЕЛ","ФЁДОР","ДЕТЕКТИВ"})
            Check(d.shots.Any(s=>s.name==name),"Scenario character is introduced: "+name);
        d.RenderAt(d.shots[4].start+3);
        var smoke=d.particles.Single(p=>p.name=="ДЫМ · сигарета");
        Check(smoke.particleCount>5&&smoke.GetComponent<ParticleSystemRenderer>().enabled,"Cigarette smoke has visible rendered particles");
        Check(d.particles.Where(p=>!d.IsSmoke(p)).All(p=>!p.GetComponent<ParticleSystemRenderer>().enabled),"Rain is hidden during interior shots");
        Check(d.musicFadeIn==0&&d.musicFadeOut==0,"Original track intro and outro are preserved without added audio fades");
        d.RenderAt(d.shots[1].start+2);
        Check(d.particles.Where(p=>!d.IsSmoke(p)).All(p=>p.GetComponent<ParticleSystemRenderer>().enabled),"Both exterior flights show rain");
        var escort=d.actors.Single(a=>a.name=="Конвоир");var prisoner=d.actors.Single(a=>a.name.Contains("Бездомный"));
        d.RenderAt(d.shots[6].start+.1f);var p0=prisoner.transform.position;
        d.RenderAt(d.shots[7].start+d.shots[7].duration-.1f);
        Check(escort.transform.position.x<-3&&prisoner.transform.position.x<-3&&Vector3.Distance(p0,prisoner.transform.position)>5,"Officer escorts the detainee from corridor into isolation wing");
        var reel=d.actors.First(a=>a.reels!=null&&a.reels.Length>0).reels[0];d.RenderAt(34);var rotation=reel.localRotation;d.RenderAt(35);
        Check(Quaternion.Angle(rotation,reel.localRotation)>60,"Magnetic tape reels rotate on the music timeline");
        Check(d.actors.Count(a=>a.name.StartsWith("Агент прослушки "))==3,"Three listening agents sit in a separate wiretap room");
        Check(d.actors.Any(a=>a.name.Contains("Наблюдатель")&&a.head.GetComponentsInChildren<Transform>().Any(t=>t.name.Contains("Очки"))),"Surveillance figure wears glasses");
        d.externalClock=false;d.Restart();await Delay(.35);
        Check(d.CurrentTime>.1f&&d.music.isPlaying,"Realtime preview advances with music");
        d.TogglePause();float frozen=d.CurrentTime;await Delay(.3);
        Check(d.Paused&&Mathf.Abs(d.CurrentTime-frozen)<.02f&&!d.music.isPlaying,"Pause freezes both camera and music");
        d.TogglePause();await Delay(.3);
        Check(!d.Paused&&d.CurrentTime>frozen+.1f&&d.music.isPlaying,"Resume restarts both camera and music");
        d.ContinueToGame();await Until(()=>SceneManager.GetActiveScene().name==Hakaton.Demo.DemoGame.HomeScene,10);
        Check(SceneManager.GetActiveScene().name==Hakaton.Demo.DemoGame.HomeScene,"Skipping the opening enters the playable game");
        File.WriteAllText("Docs/Trailer/verification.txt","PASS\n"+string.Join("\n",passed));return passed.ToArray();
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);passed.Add("PASS  "+message);}
    static Task Delay(double seconds){double start=EditorApplication.timeSinceStartup;return Until(()=>EditorApplication.timeSinceStartup-start>=seconds,seconds+3);}
    static Task Until(Func<bool> test,double timeout)
    {
        var done=new TaskCompletionSource<bool>();double start=EditorApplication.timeSinceStartup;EditorApplication.CallbackFunction tick=null;
        tick=()=>{try{if(test()){EditorApplication.update-=tick;done.TrySetResult(true);}else if(EditorApplication.timeSinceStartup-start>timeout){EditorApplication.update-=tick;done.TrySetException(new Exception("Trailer check timed out"));}}catch(Exception ex){EditorApplication.update-=tick;done.TrySetException(ex);}};
        EditorApplication.update+=tick;return done.Task;
    }
}
