using System;
using System.Linq;
using System.Threading.Tasks;
using Hakaton.Demo;
using Hakaton.Demo.Investigation;
using Hakaton.Demo.Menu;
using Hakaton.Demo.CarIntro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class VerifyInvestigationMenu
{
    static async Task Frames(int n=3){int end=Time.frameCount+n;while(Time.frameCount<end)await Task.Yield();}
    static async Task Wait(Func<bool> ready){float end=Time.realtimeSinceStartup+10;while(!ready()&&Time.realtimeSinceStartup<end)await Frames(1);if(!ready())throw new Exception("Scene did not load");}
    static void Click(string text){UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).First(b=>b.GetComponentsInChildren<Text>().Any(t=>t.text==text)).onClick.Invoke();}
    public static async Task<string[]> Main()
    {
        Click("ПРОДОЛЖИТЬ");await Wait(()=>SceneManager.GetActiveScene().name=="PoliceStation");await Frames(4);
        if(!CaseFile.Current.Done(34)||CaseFile.Current.ending!="truth")throw new Exception("Continue reset progress");
        SceneManager.LoadScene("MainMenu");await Frames(4);UnityEngine.Object.FindAnyObjectByType<MainMenu>().StartGame();await Wait(()=>SceneManager.GetActiveScene().name=="CarIntro");await Frames(4);
        var intro=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();if(intro.IsPlaying||UnityEngine.Object.FindAnyObjectByType<CaseCarMonologue>()==null)throw new Exception("New game did not hold car for authored monologue");
        if(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length!=1)throw new Exception("Monologue input event system missing or duplicated");
        int count=0;while(UnityEngine.Object.FindAnyObjectByType<CaseCarMonologue>()!=null&&count++<30){Click("ДАЛЕЕ [ПРОБЕЛ / ENTER]");await Frames(2);}
        if(!CaseFile.Current.Done(1)||!intro.IsPlaying)throw new Exception("Monologue did not start intro drive");
        intro.Finish();await Wait(()=>SceneManager.GetActiveScene().name=="FatherHouse");await Frames(6);
        var game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();if(game.hud.Mode==DemoHud.Screen.Intro)game.hud.Play();await Wait(()=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Any(b=>b.GetComponentsInChildren<Text>().Any(t=>t.text=="ОТВЕТИТЬ")));
        if(!UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Any(b=>b.GetComponentsInChildren<Text>().Any(t=>t.text=="ОТВЕТИТЬ")))throw new Exception("Incoming partner call not shown");Click("СБРОСИТЬ");await Frames(3);
        if(!CaseFile.Current.Flag("arrival_call_seen")||CaseFile.Current.Done(2))throw new Exception("Declined call forced conversation");
        return new[]{"Continue restores saved scene and completed ending","New game resets investigation and holds native car shot for monologue","Car monologue has one functioning UI event system","Completing monologue starts existing cinematic drive","Car arrival enters father house","Incoming partner call can be declined without forced dialogue"};
    }
}
