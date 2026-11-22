using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace Hakaton.Demo.Trailer.Editor
{
    public static class TrailerFrames
    {
        [Serializable] sealed class ExportInfo
        {
            public int frames,fps;
            public float duration,musicVolume,musicFadeIn,musicFadeOut;
            public string music;
        }
        public static async Task<string> ContactFrames()
        {
            var trailer=UnityEngine.Object.FindAnyObjectByType<OpeningTrailer>();
            if(!Application.isPlaying||trailer==null)throw new InvalidOperationException("Откройте OpeningTrailer и включите Play.");
            trailer.externalClock=true;trailer.music.Pause();
            Directory.CreateDirectory("Docs/Trailer");var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                for(int i=0;i<trailer.shots.Length;i++)
                {
                    float time=trailer.shots[i].start+trailer.shots[i].duration*.55f;
                    trailer.RenderAt(time);await NextUpdate();
                    Read(trailer,time,image);
                    File.WriteAllBytes("Docs/Trailer/shot-"+i.ToString("00")+".jpg",image.EncodeToJPG(95));
                }
                return "Docs/Trailer";
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }
        public static async Task<string> Export(string folder="/tmp/hakaton-trailer-frames",int fps=24)
        {
            var trailer=UnityEngine.Object.FindAnyObjectByType<OpeningTrailer>();
            if(!Application.isPlaying||trailer==null)throw new InvalidOperationException("Откройте OpeningTrailer и включите Play.");
            Directory.CreateDirectory(folder);
            if(File.Exists(Path.Combine(folder,"complete.json")))File.Delete(Path.Combine(folder,"complete.json"));
            trailer.externalClock=true;trailer.music.Stop();
            int total=Mathf.CeilToInt(trailer.Duration*fps);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                for(int i=0;i<total;i++)
                {
                    trailer.RenderAt((i+.5f)/fps);await NextUpdate();
                    Read(trailer,(i+.5f)/fps,image);
                    File.WriteAllBytes(Path.Combine(folder,i.ToString("00000")+".jpg"),image.EncodeToJPG(93));
                    if(i%12==0)
                    {
                        File.WriteAllText(Path.Combine(folder,"progress.json"),"{\"frame\":"+i+",\"total\":"+total+"}");
                        await NextUpdate();
                    }
                }
                var info=new ExportInfo{frames=total,fps=fps,duration=trailer.Duration,
                    music=AssetDatabase.GetAssetPath(trailer.music.clip),musicVolume=trailer.musicVolume,
                    musicFadeIn=trailer.musicFadeIn,musicFadeOut=trailer.musicFadeOut};
                File.WriteAllText(Path.Combine(folder,"complete.json"),JsonUtility.ToJson(info,true));
                return folder;
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }
        static void Read(OpeningTrailer trailer,float seconds,Texture2D image)
        {
            var target=trailer.RenderMaster(seconds);var previous=RenderTexture.active;
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply(false);RenderTexture.active=previous;
        }
        static Task NextUpdate()
        {
            var done=new TaskCompletionSource<bool>();
            EditorApplication.CallbackFunction next=null;
            next=()=>{EditorApplication.update-=next;done.TrySetResult(true);};EditorApplication.update+=next;
            return done.Task;
        }
    }
}
