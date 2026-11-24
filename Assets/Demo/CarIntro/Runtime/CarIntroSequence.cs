using System;
using Hakaton.Demo.Trailer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace Hakaton.Demo.CarIntro
{
    [Serializable] public sealed class CarIntroShot
    {
        public string label;
        public float start,duration,fieldOfView=48;
        public Vector3 from,to,lookFrom,lookTo;
        public bool followCar,insideCar;
    }
    [DefaultExecutionOrder(50)]
    public sealed class CarIntroSequence : MonoBehaviour
    {
        public const float Length=31.5f;
        public const float BrakeStart=22.72f, ParkTime=27.704f, ArrivalStart=22.72f, TitleAccent=27.184f;
        public const float RainStartVolume=.02f,RainEndVolume=DemoWeather.OutdoorRainVolume,RainRiseStart=24.124f,RainRiseEnd=29.2f;
        public AudioSource ambience;
        public OpeningTrailer picture;
        public CarIntroShot[] cameraShots;
        public Transform car,wiperLeft,wiperRight,steering;
        public Transform[] wheels;
        public Transform rainRoot;
        public Renderer[] rainGlass;
        public bool playOnStart=true,externalClock;
        public bool IsPlaying{get;private set;}
        public bool Paused{get;private set;}
        public float CurrentTime{get;private set;}
        double started;float held;bool leaving;int inputAfterFrame;
        Material[] glass,sourceGlass;
        public static Vector3 Road(float u)
        {
            var a=new Vector3(-12,0,-195);var b=new Vector3(-10,0,-146);var c=new Vector3(7,0,-65);var d=new Vector3(-4.7f,0,-14);
            float v=1-u;return v*v*v*a+3*v*v*u*b+3*v*u*u*c+u*u*u*d;
        }
        public static Quaternion Heading(float u)
        {var delta=Road(Mathf.Min(1,u+.002f))-Road(Mathf.Max(0,u-.002f));return Quaternion.LookRotation(-delta.normalized);}
        void Awake()
        {
            picture.externalClock=true;picture.playOnStart=false;picture.BeforeWorldRender+=ComposeFrame;
            glass=new Material[rainGlass.Length];sourceGlass=new Material[rainGlass.Length];for(int i=0;i<glass.Length;i++){sourceGlass[i]=rainGlass[i].sharedMaterial;glass[i]=new Material(sourceGlass[i]);rainGlass[i].sharedMaterial=glass[i];}
        }
        void Start(){if(playOnStart&&!Investigation.CaseFile.Current.Done(1)&&Resources.Load<TextAsset>("CaseDialogues")!=null)gameObject.AddComponent<Investigation.CaseCarMonologue>().Initialize(this);else if(playOnStart)BeginAfterMonologue();else HoldForMonologue();}
        public void HoldForMonologue()
        {
            IsPlaying=false;Paused=false;held=0;picture.music.Stop();ambience?.Stop();RenderAt(0);
            var color=picture.fade.color;color.a=0;picture.fade.color=color;picture.controls.gameObject.SetActive(false);
        }
        // Call after the author-owned, manually advanced dialogue closes.
        public void BeginAfterMonologue()
        {
            held=0;started=AudioSettings.dspTime;Paused=false;leaving=false;IsPlaying=true;inputAfterFrame=Time.frameCount+1;
            picture.controls.gameObject.SetActive(true);picture.music.Stop();picture.music.time=0;picture.music.Play();if(ambience!=null){ambience.time=0;ambience.Play();}RenderAt(0);
        }
        void Update()
        {
            if(externalClock||!IsPlaying)return;
            var k=Keyboard.current;
            if(k!=null&&Time.frameCount>inputAfterFrame)
            {
                if(k.escapeKey.wasPressedThisFrame){Finish();return;}
                if(k.spaceKey.wasPressedThisFrame)TogglePause();
                if(k.rKey.wasPressedThisFrame)BeginAfterMonologue();
            }
            if(Paused)return;
            float t=held+(float)(AudioSettings.dspTime-started);RenderAt(Mathf.Min(t,Length));if(t>=Length)Finish();
        }
        public void TogglePause()
        {
            if(!IsPlaying)return;
            if(Paused){started=AudioSettings.dspTime;picture.music.UnPause();ambience?.UnPause();}
            else{held=CurrentTime;picture.music.Pause();ambience?.Pause();}
            Paused=!Paused;
        }
        public void Finish()
        {
            if(leaving)return;leaving=true;IsPlaying=false;Time.timeScale=1;CarIntroArrival.Begin(this);
        }
        public void RenderAt(float time)
        {
            CurrentTime=Mathf.Clamp(time,0,Length);float u=TravelProgress(CurrentTime);float motion=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(BrakeStart,ParkTime,CurrentTime));
            car.SetPositionAndRotation(Road(u)+Vector3.up*(Mathf.Sin(CurrentTime*7)*.012f*motion),Heading(u));
            if(rainRoot!=null)rainRoot.position=car.position;
            float sweep=17+Mathf.Sin(CurrentTime*3.2f)*48;
            wiperLeft.localRotation=Quaternion.Euler(0,0,sweep);wiperRight.localRotation=Quaternion.Euler(0,0,sweep-6);
            steering.localRotation=Quaternion.Euler(18,0,Mathf.Sin(CurrentTime*.3f)*7*motion);
            foreach(var wheel in wheels)wheel.localRotation=Quaternion.Euler(0,0,90)*Quaternion.Euler(0,u*181.8f/.338f*Mathf.Rad2Deg,0);
            foreach(var material in glass)material.SetFloat("_IntroTime",CurrentTime);
            int index=0;
            for(int i=0;i<cameraShots.Length;i++)
            {
                var a=cameraShots[i];var b=picture.shots[i];
                b.from=a.followCar?car.TransformPoint(a.from):a.from;b.to=a.followCar?car.TransformPoint(a.to):a.to;
                b.lookFrom=a.followCar?car.TransformPoint(a.lookFrom):a.lookFrom;b.lookTo=a.followCar?car.TransformPoint(a.lookTo):a.lookTo;
                if(CurrentTime>=a.start)index=i;
            }
            picture.worldCamera.cullingMask=~(1<<5);if(cameraShots[index].insideCar)picture.worldCamera.cullingMask&=~(1<<8);
            Physics.SyncTransforms();picture.RenderAt(CurrentTime);
        }
        public RenderTexture RenderMaster(float time){RenderAt(time);return picture.RenderMaster(time);}
        public void RestoreWindowMaterials(){if(sourceGlass!=null)for(int i=0;i<sourceGlass.Length;i++)if(rainGlass[i]!=null)rainGlass[i].sharedMaterial=sourceGlass[i];}
        public static float TravelProgress(float time)
        {
            float d=ParkTime-BrakeStart;float b=Mathf.Clamp01((time-BrakeStart)/d);
            return Mathf.Clamp01((Mathf.Min(Mathf.Max(0,time),BrakeStart)+d*(b-b*b*b+.5f*b*b*b*b))/(BrakeStart+d*.5f));
        }
        public static float RainVolume(float time)=>Mathf.Lerp(RainStartVolume,RainEndVolume,Mathf.SmoothStep(0,1,Mathf.InverseLerp(RainRiseStart,RainRiseEnd,time)));
        static float Ease(float x){x=Mathf.Clamp01(x);return x*x*x*(x*(x*6-15)+10);}
        void ComposeFrame(float time)
        {
            if(ambience!=null)ambience.volume=RainVolume(time);
            if(time<ArrivalStart)return;
            // The last two captions share one continuous crane move; no cut/fade at their boundary.
            float u=Ease(Mathf.InverseLerp(ArrivalStart,30.25f,time));var camera=picture.worldCamera;
            camera.transform.position=Vector3.Lerp(new Vector3(3,11,-37),new Vector3(0,1.70f,-14),u);
            var look=Vector3.Lerp(new Vector3(-1,2.6f,6),new Vector3(0,1.70f,6),u);
            camera.transform.rotation=Quaternion.LookRotation(look-camera.transform.position);camera.fieldOfView=Mathf.Lerp(52,72,u);
            var c=picture.fade.color;c.a=Ease(Mathf.InverseLerp(30.6f,Length,time));picture.fade.color=c;
            float alpha=Mathf.SmoothStep(0,1,Mathf.InverseLerp(TitleAccent,TitleAccent+.65f,time))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(29.5f,30.3f,time)));
            picture.characterName.text="ДОМ ПОМНИТ";c=picture.characterName.color;c.a=alpha;picture.characterName.color=c;c=picture.accent.color;c.a=alpha;picture.accent.color=c;
        }
        void OnDestroy(){if(picture!=null)picture.BeforeWorldRender-=ComposeFrame;if(glass!=null)foreach(var m in glass)if(m!=null)Destroy(m);}
    }
}
