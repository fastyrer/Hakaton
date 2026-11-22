using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Hakaton.Demo;
using Hakaton.Demo.Trailer;
using Hakaton.Demo.Trailer.Editor;
using Hakaton.LocationKit;
using UnityEngine;
public static class VerifyTrailerStation
{
    public static string[] Main()
    {
        var d=UnityEngine.Object.FindAnyObjectByType<OpeningTrailer>();var list=new List<string>();
        if(!Application.isPlaying||d==null)throw new Exception("Start OpeningTrailer in Play Mode");
        Action<bool,string> check=(ok,text)=>{if(!ok)throw new Exception(text);list.Add("PASS  "+text);};
        var baseline=JsonUtility.FromJson<TrailerStationRefresh.Snapshot>(File.ReadAllText(TrailerStationRefresh.BaselinePath));
        check(d.shots.Length==baseline.shots.Length&&d.shots.Zip(baseline.shots,(a,b)=>JsonUtility.ToJson(a)==JsonUtility.ToJson(b)).All(x=>x),"All thirty cuts, camera flights, framing, captions and shot concepts are unchanged");
        check(d.Duration==baseline.duration&&d.musicVolume==baseline.musicVolume&&d.musicFadeIn==baseline.musicFadeIn&&d.musicFadeOut==baseline.musicFadeOut,"Full soundtrack duration and music treatment are unchanged");
        check(d.actors.Where(a=>a.transform.IsChildOf(d.transform)).Select(a=>a.name).SequenceEqual(baseline.stagedActorNames),"All fifteen original staged actors and recorders remain");
        var location=UnityEngine.Object.FindAnyObjectByType<LocationRoot>();
        check(location!=null&&location.name=="ЛОКАЦИЯ · нынешний полицейский участок","Trailer uses a fresh copy of the current police station");
        check(location.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Окно с жалюзи")==24,"Current station's twenty-four windows and blinds are present");
        check(location.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Копировальный аппарат")==2,"Current station's two copiers are present");
        check(location.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Тумба под рабочим столом")==14,"All fourteen detailed workstations are present");
        check(d.actors.Count(a=>a.GetComponent<StationNpc>()!=null)==32,"Thirty-two original background NPCs retain their place in the scene");
        check(!location.game.gameObject.activeInHierarchy&&UnityEngine.Object.FindObjectsByType<AudioListener>().Length==1,"Gameplay, headphones and player listener stay inactive in the trailer");
        check(d.stationAmbience!=null&&d.stationAmbience.fans.Length==8&&d.stationAmbience.clockHands.Length==6,"Eight fans and six clock hands follow the cinematic clock");
        check(d.particles.Length==4&&d.smokeParticles.Length==3,"Rain, the original cigarette and two current desk ashtrays are explicitly classified");
        var smoker=d.actors.Single(a=>a.smoking);check(smoker.seated&&smoker.transform.GetComponentsInChildren<Transform>().Count(t=>t.name=="Ботинок на столе")==2,"Detective still sits with both feet on the desk");
        var shades=d.transform.GetComponentsInChildren<Transform>().Count(t=>t.name=="Жалюзи · полоса");check(shades==12,"Surveillance composition retains the same twelve foreground blind slats");
        d.externalClock=true;d.RenderAt(83);var fan=d.stationAmbience.fans[0].localRotation;var hand=d.stationAmbience.clockHands[0].localRotation;d.RenderAt(85);
        check(Quaternion.Angle(fan,d.stationAmbience.fans[0].localRotation)>60,"Fans animate when the cinematic clock advances");
        d.RenderAt(83);check(Quaternion.Angle(fan,d.stationAmbience.fans[0].localRotation)<.01f&&Quaternion.Angle(hand,d.stationAmbience.clockHands[0].localRotation)<.01f,"Seeking produces deterministic fan and clock poses");
        foreach(int i in new[]{4,5,8,9,10,11,12,22,23,24,25,27})
        {
            d.RenderAt(d.shots[i].start+d.shots[i].duration*.55f);
            check(d.particles.Where(p=>!d.IsSmoke(p)).All(p=>!p.GetComponent<ParticleSystemRenderer>().enabled),"No exterior rain overlays interior shot "+i);
        }
        File.WriteAllText("Docs/Trailer/station-refresh-verification.txt",string.Join("\n",list));return list.ToArray();
    }
}
