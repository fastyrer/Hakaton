using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hakaton.Demo;
using Hakaton.Demo.Station;
using Hakaton.LocationKit;
using UnityEditor;
using UnityEngine;
public static class VerifyStationStyle
{
    static readonly List<string> passed=new List<string>();
    public static async Task<string[]> Main()
    {
        passed.Clear();var game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();
        if(!Application.isPlaying||game==null||UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="PoliceStation")throw new Exception("Start PoliceStation in Play Mode.");
        game.hud.Play();await Delay(1.7);
        var root=UnityEngine.Object.FindAnyObjectByType<LocationRoot>();var detail=UnityEngine.Object.FindAnyObjectByType<StationAmbience>();
        Check(detail!=null,"Station has its detailed live interior");
        Check(detail.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Окно с жалюзи")==24,"Twenty-four real windows have blinds");
        Check(detail.fans.Length==7&&detail.clockHands.Length==5,"Seven fans and five clocks animate the station");
        var turn=detail.fans[0].rotation;await Delay(.3);
        Check(Quaternion.Angle(turn,detail.fans[0].rotation)>7,"Fans move in actual gameplay");
        Check(game.hud.worldVerticalResolution==360&&UnityEngine.Object.FindAnyObjectByType<RetroView>().display.texture.height==360,"Station details render at the same clarity as the menu");
        Check(game.hud.retroMaterial.shader.name=="Hakaton/Menu Grade","Station uses the menu's warm detective colour treatment");
        Check(root.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="Плитка").All(r=>r.sharedMaterial.name=="Worn_vinyl"),"Harsh checkerboard floor is replaced throughout the building");
        Check(detail.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Тумба под рабочим столом")==14,"Fourteen workstations have drawer pedestals");
        Check(detail.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Копировальный аппарат")==2,"Two furnished copying stations fill the office");
        Check(UnityEngine.Object.FindObjectsByType<StationNpc>().Length==32,"All thirty-two NPCs remain in the location");
        Check(detail.GetComponentsInChildren<ParticleSystem>().Count(p=>p.isPlaying&&p.particleCount>0)==2,"Two desk ashtrays emit live smoke");
        var weather=UnityEngine.Object.FindAnyObjectByType<DemoWeather>();
        var particles=new ParticleSystem.Particle[5000];int count=weather.rain.GetParticles(particles);
        int leaks=particles.Take(count).Count(p=>Mathf.Abs(p.position.x)<19.7f&&p.position.z>.3f&&p.position.z<39.7f&&p.position.y>.05f&&p.position.y<2.8f);
        Check(count>100&&leaks==0,"Rain remains outside the building, with no indoor drops");
        foreach(var p in new[]{new Vector3(0,10,20),new Vector3(-19,10,1),new Vector3(19,10,1),new Vector3(-19,10,39),new Vector3(19,10,39)})
            Check(Physics.Raycast(p,Vector3.down,out var hit,15,1<<2,QueryTriggerInteraction.Ignore)&&hit.collider.name=="Крыша · защита от дождя","Weather roof covers "+p.x+" / "+p.z);
        Check(!Physics.CapsuleCast(new Vector3(0,.4f,5),new Vector3(0,1.5f,5),.25f,Vector3.forward,33,~0,QueryTriggerInteraction.Ignore),"Central walking corridor remains clear");
        foreach(var door in root.GetComponentsInChildren<DemoDoor>())
        {
            bool wasOpen=door.isOpen;door.SetOpen(true);Physics.SyncTransforms();
            try
            {
                var frame=door.GetComponentInParent<LocationPiece>().transform;
                bool clear=false;
                foreach(float offset in new[]{0f,-.45f,.45f})
                {
                    var start=frame.position+frame.right*offset-frame.forward*1.05f;
                    var bottom=start+Vector3.up*.4f;var top=start+Vector3.up*1.5f;
                    Vector3 end=frame.forward*2.1f;
                    bool occupied=Physics.OverlapCapsule(bottom,top,.25f,~0,QueryTriggerInteraction.Ignore).Any(c=>c.gameObject!=game.player.gameObject)||
                        Physics.OverlapCapsule(bottom+end,top+end,.25f,~0,QueryTriggerInteraction.Ignore).Any(c=>c.gameObject!=game.player.gameObject);
                    bool blocked=occupied||Physics.CapsuleCast(bottom,top,.25f,frame.forward,2.1f,~0,QueryTriggerInteraction.Ignore);
                    if(!blocked){clear=true;break;}
                }
                Check(clear,"Doorway stays traversable: "+door.label);
            }
            finally{door.SetOpen(wasOpen);Physics.SyncTransforms();}
        }
        Check(game.evidence.Where(e=>e!=null).Select(e=>e.evidenceId).OrderBy(i=>i).SequenceEqual(new[]{3,4,5}),"Archive retains exactly its three original evidence items");
        File.WriteAllText("Docs/station-style-verification.txt","PASS\n"+string.Join("\n",passed));return passed.ToArray();
    }
    static void Check(bool ok,string text){if(!ok)throw new Exception("CHECK FAILED: "+text);passed.Add("PASS  "+text);}
    static Task Delay(double seconds)
    {
        var done=new TaskCompletionSource<bool>();double start=EditorApplication.timeSinceStartup;EditorApplication.CallbackFunction tick=null;
        tick=()=>{if(EditorApplication.timeSinceStartup-start>=seconds){EditorApplication.update-=tick;done.TrySetResult(true);}};EditorApplication.update+=tick;return done.Task;
    }
}
