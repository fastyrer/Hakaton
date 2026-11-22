using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Hakaton.Demo.CarIntro;
public static class InspectCrownVictoria
{
 public static string Main(){var s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();s.externalClock=true;s.picture.music.Stop();s.RenderAt(6);var cam=s.picture.worldCamera;var ts=s.car.GetComponentsInChildren<Transform>(true);var layers=Array.ConvertAll(ts,t=>t.gameObject.layer);var fog=RenderSettings.fog;var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;var rt=new RenderTexture(1280,720,24);rt.Create();var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);var l=new GameObject("QA light").AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.8f;l.color=Color.white;l.transform.rotation=Quaternion.Euler(42,-30,0);l.cullingMask=1<<31;var previous=RenderTexture.active;var bg=cam.backgroundColor;var flags=cam.clearFlags;
 try{foreach(var t in ts)t.gameObject.layer=31;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.55f,.55f);cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.19f,.22f,.25f);cam.fieldOfView=36;var poses=new[]{new Vector3(-6,2.8f,-8),new Vector3(8,1.7f,0),new Vector3(6,2.8f,8)};var names=new[]{"front","side","rear"};for(int i=0;i<poses.Length;i++){cam.transform.position=s.car.TransformPoint(poses[i]);cam.transform.LookAt(s.car.TransformPoint(new Vector3(0,.8f,0)),Vector3.up);RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("Docs/CarIntro/crown-victoria-"+names[i]+".png",tex.EncodeToPNG());}return "Three native model views saved";}
 finally{for(int i=0;i<ts.Length;i++)ts[i].gameObject.layer=layers[i];RenderSettings.fog=fog;RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;cam.backgroundColor=bg;cam.clearFlags=flags;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(l.gameObject);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);s.RenderAt(6);}}
}
