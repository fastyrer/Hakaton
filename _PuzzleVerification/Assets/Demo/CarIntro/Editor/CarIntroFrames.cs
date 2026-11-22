using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace Hakaton.Demo.CarIntro.Editor
{
    public static class CarIntroFrames
    {
        [Serializable] sealed class Info{public int frames,fps;public float duration,musicVolume,musicFadeIn,musicFadeOut;public string music,ambience;public float rainStartVolume,rainEndVolume,rainRiseStart,rainRiseEnd;}
        public static async Task<string> Contact()
        {
            var s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();if(!Application.isPlaying||s==null)throw new InvalidOperationException("Start CarIntro");s.externalClock=true;s.picture.music.Stop();s.ambience?.Stop();Directory.CreateDirectory("Docs/CarIntro");var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try{for(int i=0;i<s.cameraShots.Length;i++){var shot=s.cameraShots[i];float t=shot.start+shot.duration*.55f;s.RenderAt(t);await Next();Read(s,t,image);File.WriteAllBytes("Docs/CarIntro/shot-"+i.ToString("00")+".jpg",image.EncodeToJPG(95));}return "Docs/CarIntro";}finally{UnityEngine.Object.DestroyImmediate(image);}
        }
        public static async Task<string> Export(string folder="/tmp/hakaton-car-intro-frames",int fps=24)
        {
            var s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();if(!Application.isPlaying||s==null)throw new InvalidOperationException("Start CarIntro");s.externalClock=true;s.picture.music.Stop();s.ambience?.Stop();Directory.CreateDirectory(folder);string marker=Path.Combine(folder,"complete.json");if(File.Exists(marker))File.Delete(marker);
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);int total=Mathf.CeilToInt(CarIntroSequence.Length*fps);
            try
            {
                for(int i=0;i<total;i++){float t=(i+.5f)/fps;s.RenderAt(t);await Next();Read(s,t,image);File.WriteAllBytes(Path.Combine(folder,i.ToString("00000")+".jpg"),image.EncodeToJPG(93));if(i%12==0)File.WriteAllText(Path.Combine(folder,"progress.json"),"{\"frame\":"+i+",\"total\":"+total+"}");}
                WriteMetadata(s,folder,fps);return folder;
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }
        public static string RefreshMetadata(string folder="/tmp/hakaton-car-intro-frames",int fps=24)
        {
            var s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();var prior=JsonUtility.FromJson<Info>(File.ReadAllText(Path.Combine(folder,"complete.json")));
            if(s==null||prior.fps!=fps||prior.duration!=CarIntroSequence.Length||Directory.GetFiles(folder,"?????.jpg").Length!=prior.frames)throw new InvalidOperationException("Re-export the picture frames first.");
            WriteMetadata(s,folder,fps);return "Unchanged picture frames matched; refreshed native audio parameters";
        }
        static void WriteMetadata(CarIntroSequence s,string folder,int fps)
        {
            var d=s.picture;File.WriteAllText(Path.Combine(folder,"complete.json"),JsonUtility.ToJson(new Info{frames=Mathf.CeilToInt(CarIntroSequence.Length*fps),fps=fps,duration=CarIntroSequence.Length,musicVolume=d.musicVolume,musicFadeIn=d.musicFadeIn,musicFadeOut=d.musicFadeOut,music=AssetDatabase.GetAssetPath(d.music.clip),ambience=AssetDatabase.GetAssetPath(s.ambience.clip),rainStartVolume=CarIntroSequence.RainStartVolume,rainEndVolume=CarIntroSequence.RainEndVolume,rainRiseStart=CarIntroSequence.RainRiseStart,rainRiseEnd=CarIntroSequence.RainRiseEnd},true));
        }
        static void Read(CarIntroSequence s,float t,Texture2D image){var target=s.RenderMaster(t);var old=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply(false);RenderTexture.active=old;}
        static Task Next(){var t=new TaskCompletionSource<bool>();EditorApplication.CallbackFunction f=null;f=()=>{EditorApplication.update-=f;t.TrySetResult(true);};EditorApplication.update+=f;return t.Task;}
    }
}
