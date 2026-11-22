using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hakaton.Demo;
using Hakaton.Demo.Menu;
using Hakaton.Demo.Trailer;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class VerifyMainMenu
{
    static readonly List<string> passed=new List<string>();
    static readonly List<InputDevice> suspended=new List<InputDevice>();
    static Keyboard keyboard;
    public static async Task<string[]> Main()
    {
        passed.Clear();var menu=UnityEngine.Object.FindAnyObjectByType<MainMenu>();
        if(!Application.isPlaying||menu==null)throw new Exception("Start MainMenu in Play Mode.");
        bool hadVolume=PlayerPrefs.HasKey(DemoPreferences.VolumeKey),hadMouse=PlayerPrefs.HasKey(DemoPreferences.MouseKey);
        float oldVolume=PlayerPrefs.GetFloat(DemoPreferences.VolumeKey),oldMouse=PlayerPrefs.GetFloat(DemoPreferences.MouseKey),oldListener=AudioListener.volume;
        var background=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard=InputSystem.AddDevice<Keyboard>("Main menu verification keyboard");
        foreach(var device in InputSystem.devices)
            if(device!=keyboard&&device.enabled&&device is Keyboard){suspended.Add(device);InputSystem.DisableDevice(device);}
        try
        {
            Check(EditorBuildSettings.scenes[0].path=="Assets/Scenes/MainMenu.unity","Main menu is the first enabled build scene");
            Check(!menu.SettingsOpen&&!menu.IsLoading&&Time.timeScale==1,"Menu starts in its normal interactive state");
            Check(Cursor.visible&&Cursor.lockState==CursorLockMode.None,"Menu releases the mouse cursor");
            Check(UnityEngine.Object.FindObjectsByType<DemoGame>().Length==0,"Menu has no active gameplay controller");
            Check(UnityEngine.Object.FindObjectsByType<Renderer>().Length>150,"Office background consists of real rendered models");
            var view=UnityEngine.Object.FindAnyObjectByType<RetroView>();await Frames(4);
            Check(view.display.texture is RenderTexture&&view.worldCamera.targetTexture!=null,"UI displays a live camera RenderTexture");
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.Video.VideoPlayer>().Length==0,"Background is not a video player");
            Check(view.displayMaterial.shader.name=="Hakaton/Menu Grade","Menu uses its warm detective colour grade");
            var atmosphere=UnityEngine.Object.FindAnyObjectByType<MenuAtmosphere>();var rotation=atmosphere.fan.localRotation;
            await Delay(.3);Check(Quaternion.Angle(rotation,atmosphere.fan.localRotation)>8,"Ceiling fan rotates during menu navigation");
            var smoke=UnityEngine.Object.FindObjectsByType<ParticleSystem>().Single(p=>p.name=="Дым над пепельницей");
            Check(smoke.isPlaying&&smoke.particleCount>5,"Ashtray emits visible live smoke");
            Check(UnityEngine.Object.FindObjectsByType<StationNpc>().Length==2,"Our police officers are present in the background corridor");
            Check(UnityEngine.Object.FindObjectsByType<Light>().Any(l=>l.type==LightType.Directional&&l.shadows!=LightShadows.None),"Window lighting casts real blind shadows");
            foreach(var button in new[]{menu.newGame,menu.introduction,menu.settings,menu.quit})
                Check(Hit(button),button.name+" has an unobstructed pointer target");
            Click(menu.settings);await Frames(3);
            Check(menu.SettingsOpen&&!menu.menuPanel.gameObject.activeSelf,"Settings button opens its panel");
            DragVolume(menu.volume,.32f);await Frames(2);
            Check(Mathf.Abs(AudioListener.volume-.32f)<.04f&&Mathf.Abs(DemoPreferences.Volume-.32f)<.04f,"Pointer input changes master volume and stores its value");
            menu.mouse.value=.19f;await Frames(2);
            Check(Mathf.Abs(DemoPreferences.MouseSensitivity-.19f)<.001f,"Mouse sensitivity is stored for the game");
            float savedVolume=DemoPreferences.Volume;
            await Tap(Key.Escape);
            Check(!menu.SettingsOpen&&menu.menuPanel.gameObject.activeSelf,"Escape closes settings and restores navigation");
            Click(menu.introduction);menu.StartGame();
            Check(menu.IsLoading&&menu.transition.raycastTarget,"Scene transition fades and blocks duplicate actions");
            await Until(()=>SceneManager.GetActiveScene().name=="OpeningTrailer"&&UnityEngine.Object.FindAnyObjectByType<OpeningTrailer>()?.music.isPlaying==true,12);
            var trailer=UnityEngine.Object.FindAnyObjectByType<OpeningTrailer>();
            Check(trailer.nextScene=="MainMenu","Watching the introduction routes back to the menu");
            Check(trailer.music.clip.name.Contains("Tyler Bates")&&trailer.Duration>216,"Menu opens the full Frank's Choice introduction");
            await Tap(Key.Escape);
            await Until(()=>SceneManager.GetActiveScene().name=="MainMenu"&&UnityEngine.Object.FindAnyObjectByType<MainMenu>()!=null,12);
            menu=UnityEngine.Object.FindAnyObjectByType<MainMenu>();await Frames(3);
            Check(Mathf.Abs(menu.volume.value-savedVolume)<.001f&&Mathf.Abs(menu.mouse.value-.19f)<.001f,"Settings survive watching the intro and returning to the menu");
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1,"Returning does not duplicate the input system");
            Click(menu.newGame);
            await Until(()=>SceneManager.GetActiveScene().name=="CarIntro"&&UnityEngine.Object.FindAnyObjectByType<Hakaton.Demo.CarIntro.CarIntroSequence>()?.picture.music.isPlaying==true,12);
            var carIntro=UnityEngine.Object.FindAnyObjectByType<Hakaton.Demo.CarIntro.CarIntroSequence>();
            Check(carIntro.picture.Duration==Hakaton.Demo.CarIntro.CarIntroSequence.Length&&carIntro.picture.music.clip.name.Contains("Deception"),"New game starts the Deception car cinematic");
            await Tap(Key.Escape);
            await Until(()=>SceneManager.GetActiveScene().name==DemoGame.HomeScene&&UnityEngine.Object.FindAnyObjectByType<DemoGame>()?.hud.Mode==DemoHud.Screen.Intro,12);
            var game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();
            Check(Mathf.Abs(game.player.sensitivity-.19f)<.001f,"Gameplay applies menu mouse sensitivity");
            Check(Mathf.Abs(AudioListener.volume-savedVolume)<.001f,"Gameplay keeps the selected volume");
            Check(game.Found.Count==0&&game.board.Entries.Count==1,"New game starts a fresh investigation");
            game.hud.Play();game.hud.ShowPause();await Frames(2);
            var back=UnityEngine.Object.FindObjectsByType<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="ГЛАВНОЕ МЕНЮ");Click(back);
            await Until(()=>SceneManager.GetActiveScene().name=="MainMenu"&&UnityEngine.Object.FindAnyObjectByType<MainMenu>()!=null,12);await Frames(3);
            Check(Time.timeScale==1&&Cursor.visible,"Pause menu returns to an interactive main menu");
            File.WriteAllText("Docs/main-menu-verification.txt","PASS\n"+string.Join("\n",passed));return passed.ToArray();
        }
        finally
        {
            if(hadVolume)PlayerPrefs.SetFloat(DemoPreferences.VolumeKey,oldVolume);else PlayerPrefs.DeleteKey(DemoPreferences.VolumeKey);
            if(hadMouse)PlayerPrefs.SetFloat(DemoPreferences.MouseKey,oldMouse);else PlayerPrefs.DeleteKey(DemoPreferences.MouseKey);
            PlayerPrefs.Save();AudioListener.volume=oldListener;
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
            foreach(var device in suspended)if(device.added)InputSystem.EnableDevice(device);suspended.Clear();InputSystem.settings.backgroundBehavior=background;
        }
    }
    static bool Hit(Button b)
    {
        var point=RectTransformUtility.WorldToScreenPoint(null,b.GetComponent<RectTransform>().TransformPoint(b.GetComponent<RectTransform>().rect.center));
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
        return hits.Count>0&&(hits[0].gameObject==b.gameObject||hits[0].gameObject.transform.IsChildOf(b.transform));
    }
    static void Click(Button b)=>ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    static void DragVolume(Slider slider,float fraction)
    {
        var r=slider.GetComponent<RectTransform>();var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(new Vector3(r.rect.width*fraction,17,0)));
        ExecuteEvents.Execute(slider.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=p},ExecuteEvents.pointerDownHandler);
    }
    static async Task Tap(Key key){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));await Frames(2);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(2);}
    static void Check(bool ok,string text){if(!ok)throw new Exception("CHECK FAILED: "+text);passed.Add("PASS  "+text);}
    static Task Frames(int count){int start=Time.frameCount;return Until(()=>Time.frameCount>=start+count,5);}
    static Task Delay(double seconds){double start=EditorApplication.timeSinceStartup;return Until(()=>EditorApplication.timeSinceStartup-start>=seconds,seconds+3);}
    static Task Until(Func<bool> predicate,double timeout)
    {
        var done=new TaskCompletionSource<bool>();double start=EditorApplication.timeSinceStartup;EditorApplication.CallbackFunction tick=null;
        tick=()=>{try{if(predicate()){EditorApplication.update-=tick;done.TrySetResult(true);}else if(EditorApplication.timeSinceStartup-start>timeout){EditorApplication.update-=tick;done.TrySetException(new Exception("Menu verification timeout"));}}catch(Exception ex){EditorApplication.update-=tick;done.TrySetException(ex);}};EditorApplication.update+=tick;return done.Task;
    }
}
