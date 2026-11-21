using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Hakaton.Demo.Trailer
{
    public sealed class OpeningTrailer : MonoBehaviour
    {
        public TrailerShot[] shots;
        public Camera worldCamera, outputCamera;
        public Canvas canvas;
        public RawImage picture;
        public Text characterName, role, chapter, title, subtitle, controls;
        public Image fade, topBar, bottomBar, accent;
        public Material grade;
        public AudioSource music;
        public TrailerActor[] actors;
        public ParticleSystem[] particles;
        public bool externalClock, playOnStart=true;
        [Range(0,1)] public float musicVolume=.65f;
        [Min(0)] public float musicFadeIn, musicFadeOut;
        public string nextScene="HouseArrival";
        public float Duration => shots==null||shots.Length==0?0:shots[shots.Length-1].start+shots[shots.Length-1].duration;
        public float CurrentTime {get;private set;}
        public int CurrentShot {get;private set;}
        public RenderTexture Output {get;private set;}
        public bool Paused {get;private set;}
        RenderTexture world;
        Material instanceGrade;
        double startTime;
        float heldTime;
        void Awake(){EnsureResources();startTime=AudioSettings.dspTime;}
        void Start()
        {
            string destination=Hakaton.Demo.Menu.MenuNavigation.ConsumeTrailerDestination();
            if(!string.IsNullOrEmpty(destination))nextScene=destination;
            if(controls!=null)controls.text="ПРОБЕЛ · ПАУЗА       R · СНАЧАЛА       ESC · "+(nextScene=="MainMenu"?"В МЕНЮ":"К ИГРЕ");
            startTime=AudioSettings.dspTime;
            if(playOnStart&&music!=null&&music.clip!=null){music.time=0;music.Play();}
            RenderAt(0);
        }
        void EnsureResources()
        {
            if(world!=null)return;
            world=new RenderTexture(640,360,24,RenderTextureFormat.ARGB32){name="Trailer · PS1 world",filterMode=FilterMode.Point};world.Create();
            Output=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){name="Trailer · master 720p",filterMode=FilterMode.Bilinear};Output.Create();
            instanceGrade=new Material(grade);
            picture.texture=world;picture.material=instanceGrade;
            worldCamera.enabled=false;worldCamera.aspect=16f/9;worldCamera.targetTexture=world;
            outputCamera.enabled=true;outputCamera.aspect=16f/9;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=outputCamera;canvas.planeDistance=1;
        }
        void Update()
        {
            if(externalClock)return;
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(keyboard.spaceKey.wasPressedThisFrame)TogglePause();
                if(keyboard.rKey.wasPressedThisFrame)Restart();
                if(keyboard.escapeKey.wasPressedThisFrame){ContinueToGame();return;}
            }
            if(Paused)return;
            float t=heldTime+(float)(AudioSettings.dspTime-startTime);
            RenderAt(Mathf.Min(t,Duration));
            if(t>Duration+.5f)ContinueToGame();
        }
        public void TogglePause()
        {
            if(Paused){startTime=AudioSettings.dspTime;music?.UnPause();}
            else{heldTime=CurrentTime;music?.Pause();}
            Paused=!Paused;
        }
        public void Restart()
        {
            heldTime=0;startTime=AudioSettings.dspTime;Paused=false;
            if(music!=null&&music.clip!=null){music.time=0;music.Play();}
            RenderAt(0);
        }
        public void ContinueToGame()
        {
            if(nextScene=="HouseArrival")nextScene=DemoGame.HomeScene;
            if(Application.CanStreamedLevelBeLoaded(nextScene)){Time.timeScale=1;SceneManager.LoadScene(nextScene);}
        }
        public void RenderAt(float seconds)
        {
            EnsureResources();CurrentTime=Mathf.Clamp(seconds,0,Duration);
            int index=0;for(int i=0;i<shots.Length;i++)if(CurrentTime>=shots[i].start)index=i;
            CurrentShot=index;var shot=shots[index];
            float u=Mathf.Clamp01((CurrentTime-shot.start)/shot.duration);
            // Smooth camera travel without a stop in the middle of the shot.
            float move=Mathf.Lerp(u,Mathf.SmoothStep(0,1,u),.3f);
            worldCamera.transform.position=Vector3.Lerp(shot.from,shot.to,move);
            Vector3 look=Vector3.Lerp(shot.lookFrom,shot.lookTo,move);
            worldCamera.transform.rotation=Quaternion.LookRotation(look-worldCamera.transform.position);
            worldCamera.fieldOfView=shot.fieldOfView;
            foreach(var actor in actors)if(actor!=null&&actor.gameObject.activeInHierarchy)actor.Sample(CurrentTime);
            foreach(var particle in particles)if(particle!=null&&particle.gameObject.activeInHierarchy)
            {
                bool smoke=particle.name.Contains("ДЫМ");
                var renderer=particle.GetComponent<ParticleSystemRenderer>();renderer.enabled=smoke||shot.exterior;
                if(renderer.enabled)particle.Simulate(6+CurrentTime%8,true,true,false);
            }
            float reveal=Mathf.SmoothStep(0,1,Mathf.Clamp01((CurrentTime-shot.start-.7f)/.75f));
            float hide=Mathf.SmoothStep(0,1,Mathf.Clamp01((shot.start+shot.duration-CurrentTime-.35f)/.55f));
            float alpha=reveal*hide;
            characterName.text=shot.title?"":shot.name;role.text=shot.title?"":shot.role;chapter.text=shot.label;
            SetAlpha(characterName,alpha);SetAlpha(role,alpha);SetAlpha(chapter,alpha*.75f);SetAlpha(accent,alpha*(string.IsNullOrEmpty(shot.name)?0:1));
            title.text=shot.title?"ДОМ ПОМНИТ":"";subtitle.text=shot.title?"ДЕЛО ОТЦА ЕЩЁ НЕ ЗАКРЫТО":"";
            SetAlpha(title,alpha);SetAlpha(subtitle,alpha*.8f);
            float cut=Mathf.Min(Mathf.Clamp01(u*shot.duration/.23f),Mathf.Clamp01((1-u)*shot.duration/.3f));
            if(index==0)cut*=Mathf.Clamp01(CurrentTime/1.3f);
            if(index==shots.Length-1)cut*=Mathf.Clamp01((Duration-CurrentTime)/1.7f);
            SetAlpha(fade,1-cut);
            instanceGrade.SetFloat("_TrailerTime",CurrentTime);instanceGrade.SetFloat("_Mono",shot.monochrome?1:0);instanceGrade.SetFloat("_Darken",shot.title?1:0);
            if(music!=null)music.volume=musicVolume*(musicFadeIn>0?Mathf.Clamp01(CurrentTime/musicFadeIn):1)*(musicFadeOut>0?Mathf.Clamp01((Duration-CurrentTime)/musicFadeOut):1);
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(worldCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=world});
        }
        public RenderTexture RenderMaster(float seconds)
        {
            var previous=outputCamera.targetTexture;outputCamera.targetTexture=Output;
            var scaler=canvas.GetComponent<CanvasScaler>();bool wasEnabled=scaler.enabled;scaler.enabled=false;canvas.scaleFactor=1;
            RenderAt(seconds);controls.gameObject.SetActive(false);Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(outputCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=Output});
            controls.gameObject.SetActive(true);outputCamera.targetTexture=previous;scaler.enabled=wasEnabled;return Output;
        }
        static void SetAlpha(Graphic g,float alpha){var c=g.color;c.a=alpha;g.color=c;}
        void OnDestroy()
        {
            if(world!=null){world.Release();Destroy(world);}
            if(Output!=null){Output.Release();Destroy(Output);}
            if(instanceGrade!=null)Destroy(instanceGrade);
        }
    }
}
