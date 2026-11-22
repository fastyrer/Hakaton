using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Hakaton.Demo.CarIntro;
using Hakaton.Demo.Vehicles;
public static class VerifyCrownVictoria
{
 public static string[] Main(){var s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();s.externalClock=true;s.picture.music.Stop();var r=s.car.GetComponent<CrownVictoriaRig>();var checks=new List<string>();Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception(msg);checks.Add("PASS  "+msg);};
 check(r!=null,"Cutscene uses the new Crown Victoria rig");var verts=r.bodyShell.GetComponent<MeshFilter>().sharedMesh.vertices;
 check(Mathf.Abs(verts.Max(v=>v.z)-verts.Min(v=>v.z)-5.3848f)<.001f,"Actual shell length matches the Ford 212-inch reference");check(Mathf.Abs(verts.Max(v=>v.x)-verts.Min(v=>v.x)-1.98628f)<.001f,"Actual shell width matches the Ford 78.2-inch reference");check(r.wheels.Length==4&&Mathf.Abs(Mathf.Abs(r.wheels[2].localPosition.z-r.wheels[0].localPosition.z)-2.91338f)<.001f,"Four independent wheels use the 114.7-inch wheelbase");
 foreach(float axle in new[]{-1.6f,1.31338f}){var section=verts.Where(v=>Mathf.Abs(v.z-axle)<.001f).ToArray();check(section.Length>0&&section.Min(v=>v.y)>.73f,"Wheel arch is cut into the shell above axle "+axle);}
 check(r.rainWindows.Length==6&&s.rainGlass.SequenceEqual(r.rainWindows),"Six shaped rain windows are connected to the cinematic clock");foreach(var window in r.rainWindows){var mesh=window.GetComponent<MeshFilter>().sharedMesh;var uv=mesh.uv;var t=mesh.triangles;check(Enumerable.Range(0,t.Length/3).All(i=>{var a=uv[t[i*3]];var b=uv[t[i*3+1]]-a;var c=uv[t[i*3+2]]-a;return Mathf.Abs(b.x*c.y-b.y*c.x)>.01f;}),"Rain UVs have no collapsed triangles: "+window.name);}
 check(s.wiperLeft==r.wiperLeft&&s.steering==r.steering&&s.wheels.SequenceEqual(r.wheels),"Wipers, steering and all four wheels retain animation bindings");check(r.driverSocket.childCount==1&&r.driverSocket.GetChild(0).name=="Алексей · водитель","Driver is attached at the new cabin seat");check(s.car.GetComponentsInChildren<MeshFilter>().All(m=>m.sharedMesh!=null&&m.sharedMesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z))),"All vehicle meshes contain finite coordinates");
 s.RenderAt(2);var q=r.wheels[3].localRotation;s.RenderAt(2.5f);check(Quaternion.Angle(q,r.wheels[3].localRotation)>10,"Rear wheel animation works on the new axle");s.RenderAt(0);File.WriteAllText("Docs/CarIntro/vehicle-verification.txt",string.Join("\n",checks)+"\n");return checks.ToArray();}
}
