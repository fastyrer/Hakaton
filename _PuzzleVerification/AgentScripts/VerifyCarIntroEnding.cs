using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Hakaton.Demo;
using Hakaton.Demo.CarIntro;
using Hakaton.Demo.Vehicles;
public static class VerifyCarIntroEnding
{
 public static async Task<string[]> Main()
 {
  var lines=new List<string>();Action<bool,string> check=(ok,m)=>{if(!ok)throw new Exception(m);lines.Add("PASS  "+m);};var s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();s.externalClock=true;s.picture.music.Stop();s.ambience.Stop();
  check(s.picture.music.clip.length>s.picture.Duration&&s.picture.Duration==31.5f,"Picture uses the original full-length source; music is not replaced with a spliced edit");check(s.picture.musicFadeOut==4.5f&&s.ambience!=null&&s.ambience.loop,"Music fades out over 4.5 seconds; separate recorded rain bed is connected");
  s.RenderAt(27.183f);var p=s.picture.worldCamera.transform.position;var q=s.picture.worldCamera.transform.rotation;s.RenderAt(27.185f);check(Vector3.Distance(p,s.picture.worldCamera.transform.position)<.02f&&Quaternion.Angle(q,s.picture.worldCamera.transform.rotation)<.1f,"Final shot boundary preserves camera position and orientation");check(s.picture.fade.color.a==0,"There is no black flash between the two arrival shots");
  s.RenderAt(29);var car=s.car.position;var wheel=s.wheels[0].localRotation;s.RenderAt(31);check(Vector3.Distance(car,s.car.position)<.001f&&Quaternion.Angle(wheel,s.wheels[0].localRotation)<.01f,"Car and wheels have stopped before the last music tail");check(s.ambience.volume>0&&s.ambience.volume<=.071f,"Rain remains quiet throughout the handoff");
  s.RenderAt(30.25f);p=s.picture.worldCamera.transform.position;s.RenderAt(30.55f);check(Vector3.Distance(p,s.picture.worldCamera.transform.position)<.001f,"Final camera lands and holds before the fade");check(s.picture.fade.color.a==0,"House remains visible throughout the landing hold");s.RenderAt(31.5f);check(s.picture.fade.color.a==1,"Only the final outgoing frame is fully black");
  s.RenderAt(27);check(Mathf.Abs(s.picture.music.volume-.74f)<.001f,"Music stays at full level before its final fade");s.RenderAt(29.25f);check(Mathf.Abs(s.picture.music.volume-.37f)<.001f,"Music passes smoothly through half gain during the fade");s.RenderAt(31.5f);check(s.picture.music.volume==0,"Music reaches zero before scene loading");
  s.Finish();await Until(()=>SceneManager.GetActiveScene().name==DemoGame.HomeScene&&UnityEngine.Object.FindAnyObjectByType<DemoGame>()?.hud.Mode==DemoHud.Screen.Play,12);var game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();check(!game.player.enabled&&UnityEngine.Object.FindAnyObjectByType<CarIntroArrival>()!=null,"Home opens under the visual bridge without a second introduction card");check(UnityEngine.Object.FindObjectsByType<CrownVictoriaRig>().Length==1,"The parked cinematic car replaces the house blockout exactly once");
  var rig=UnityEngine.Object.FindAnyObjectByType<CrownVictoriaRig>();check(rig.driverSocket.GetComponentsInChildren<Renderer>().Length==0,"The parked vehicle has no duplicate driver");check(rig.GetComponent<DemoInteractable>()?.kind==DemoInteractable.Interaction.Car&&rig.GetComponent<BoxCollider>()!=null,"Parked Crown Victoria retains travel interaction and collision");await Until(()=>UnityEngine.Object.FindAnyObjectByType<CarIntroArrival>()==null,5);check(rig.rainWindows.All(w=>w.sharedMaterial!=null&&w.sharedMaterial.shader.name=="Hakaton/Car Rain Glass"),"Parked windows keep valid materials after cinematic resources are destroyed");check(game.player.enabled&&Time.timeScale==1,"Fade completes and restores player control");check(UnityEngine.Object.FindObjectsByType<DemoWeather>().All(w=>w.RainFade==1),"Outdoor weather returns to its quiet sound level");
  var homeRain=UnityEngine.Object.FindObjectsByType<DemoWeather>().SelectMany(w=>w.GetComponents<AudioSource>()).Where(a=>a.loop&&a.clip!=null&&a.clip.name.Contains("Rain soft loop")).ToArray();check(homeRain.Length>0&&homeRain.All(a=>a.volume<=.071f),"Home uses the recorded rain without a volume jump");
  // A normal visit/direct scene load must keep its usual introduction.
  SceneManager.LoadScene(DemoGame.HomeScene);await Until(()=>UnityEngine.Object.FindAnyObjectByType<DemoGame>()?.hud.Mode==DemoHud.Screen.Intro,12);check(UnityEngine.Object.FindAnyObjectByType<CarIntroArrival>()==null,"One-shot arrival does not leak into later visits");File.WriteAllText("Docs/CarIntro/ending-verification.txt",string.Join("\n",lines)+"\n");return lines.ToArray();
 }
 static Task Until(Func<bool> f,double seconds){var t=new TaskCompletionSource<bool>();double began=EditorApplication.timeSinceStartup;EditorApplication.CallbackFunction tick=null;tick=()=>{try{if(f()){EditorApplication.update-=tick;t.TrySetResult(true);}else if(EditorApplication.timeSinceStartup-began>seconds){EditorApplication.update-=tick;t.TrySetException(new Exception("Timed out"));}}catch(Exception e){EditorApplication.update-=tick;t.TrySetException(e);}};EditorApplication.update+=tick;return t.Task;}
}
