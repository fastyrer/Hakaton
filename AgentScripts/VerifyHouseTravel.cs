using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using UnityEditor;
using UnityEngine;
using Hakaton.Demo;
using Hakaton.Demo.Menu;
using Hakaton.Demo.Trailer;
public static class VerifyHouseTravel
{
    public static async Task<string[]> Main()
    {
        var results=new System.Collections.Generic.List<string>();var game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();
        if(!Application.isPlaying||game==null)throw new Exception("Start house");game.hud.Play();
        var car=UnityEngine.Object.FindObjectsByType<DemoInteractable>().First(i=>i.kind==DemoInteractable.Interaction.Car);
        game.player.Teleport(car.transform.TransformPoint(new Vector3(-2.7f,.1f,0)),0);game.player.AimAt(car.transform.TransformPoint(new Vector3(0,1.2f,0)));await Seconds(.4);
        if(game.player.Focused!=car)throw new Exception("Car unreachable");car.Interact(game);if(game.hud.Mode!=DemoHud.Screen.Travel)throw new Exception("No travel menu");results.Add("PASS  Car focuses and opens travel destinations");
        foreach(var e in game.evidence.Where(e=>e!=null))game.Inspect(e);game.board.AddEvidence(0,out var card);int cards=game.board.Entries.Count;
        game.SelectDestination("Полицейский участок");await Until(()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="PoliceStation"&&UnityEngine.Object.FindAnyObjectByType<DemoGame>()?.hud.Mode==DemoHud.Screen.Intro,30);
        game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();if(!game.Found.IsSupersetOf(new[]{0,1,2})||game.board.Entries.Count!=cards)throw new Exception("House state lost en route to station");results.Add("PASS  Station receives all three family clues and board cards");
        game.SelectDestination("Дом Волковых");await Until(()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="FatherHouse"&&UnityEngine.Object.FindAnyObjectByType<DemoGame>()?.hud.Mode==DemoHud.Screen.Intro,30);
        game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();if(!game.Found.IsSupersetOf(new[]{0,1,2})||game.board.Entries.Count!=cards)throw new Exception("Return state lost");results.Add("PASS  Return route loads FatherHouse and preserves investigation");game.hud.Play();
        game.SelectDestination("Придорожное кафе");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="FatherHouse"||game.hud.Mode!=DemoHud.Screen.Travel)throw new Exception("Diner stub changed");game.hud.Play();results.Add("PASS  Diner remains a reversible placeholder");
        Time.timeScale=1;UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");await Until(()=>UnityEngine.Object.FindAnyObjectByType<MainMenu>()!=null,30);var menu=UnityEngine.Object.FindAnyObjectByType<MainMenu>();menu.StartGame();
        await Until(()=>UnityEngine.Object.FindAnyObjectByType<OpeningTrailer>()!=null&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="OpeningTrailer",30);await Seconds(.3);var trailer=UnityEngine.Object.FindAnyObjectByType<OpeningTrailer>();
        if(trailer.nextScene!="FatherHouse")throw new Exception("New game not routed to new house");trailer.ContinueToGame();
        await Until(()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="FatherHouse"&&UnityEngine.Object.FindAnyObjectByType<DemoGame>()?.hud.Mode==DemoHud.Screen.Intro,30);
        game=UnityEngine.Object.FindAnyObjectByType<DemoGame>();if(game.Found.Count!=0||game.board.Entries.Count!=1)throw new Exception("New game not fresh");results.Add("PASS  Menu -> Frank's Choice introduction -> FatherHouse starts fresh investigation");
        File.WriteAllText("Docs/father-house-travel-verification.txt",string.Join("\n",results));return results.ToArray();
    }
    static Task Seconds(double n){double t=EditorApplication.timeSinceStartup;return Until(()=>EditorApplication.timeSinceStartup>=t+n,n+5);}
    static Task Until(Func<bool> predicate,double timeout)
    {
        var done=new TaskCompletionSource<bool>();double t=EditorApplication.timeSinceStartup;EditorApplication.CallbackFunction tick=null;
        tick=()=>{try{if(predicate()){EditorApplication.update-=tick;done.TrySetResult(true);}else if(EditorApplication.timeSinceStartup-t>timeout){EditorApplication.update-=tick;done.TrySetException(new Exception("Timeout"));}}catch(Exception e){EditorApplication.update-=tick;done.TrySetException(e);}};EditorApplication.update+=tick;return done.Task;
    }
}
