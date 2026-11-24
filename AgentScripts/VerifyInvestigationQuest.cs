using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hakaton.Demo;
using Hakaton.Demo.Investigation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class VerifyInvestigationQuest
{
    static DemoGame g;static InvestigationDirector d;static List<string> passed=new List<string>();
    static void Check(bool ok,string s){if(!ok)throw new Exception(s);passed.Add(s);}
    static async Task Frames(int n=3){int target=Time.frameCount+n;while(Time.frameCount<target)await Task.Yield();}
    static void Bind(){g=UnityEngine.Object.FindAnyObjectByType<DemoGame>();d=g.GetComponent<InvestigationDirector>();g.hud.Play();}
    static async Task Finish(int expected=0)
    {if(expected>0)Check(d.CurrentScene==expected,"Authored scene "+expected+" dispatched");int scene=d.CurrentScene;int safety=250;while(d.ConversationOpen&&d.CurrentScene==scene&&safety-->0){d.NextLine();await Frames(1);}if(safety<=0)throw new Exception("Dialogue did not finish");await Frames(3);}
    static CaseInteraction Item(string id)=>UnityEngine.Object.FindObjectsByType<CaseInteraction>(FindObjectsInactive.Include).First(x=>x.id==id);
    static void Interact(string id){Item(id).Interact(g);}
    static void Click(string text){var b=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).First(x=>x.GetComponentsInChildren<Text>().Any(t=>t.text==text));b.onClick.Invoke();}
    static async Task Go(string scene){d.Travel(scene);await Frames(5);Bind();}
    static async Task Collect(string id){Interact(id);g.hud.Play();await Frames(1);}
    public static async Task<string[]> Main()
    {
        passed.Clear();CaseFile.TestPath="/tmp/hakaton-investigation-quest-test.json";CaseFile.NewGame();Bind();d.State.Complete(3);
        Interact("clock");await Finish(6);Check(d.State.Flag("diamond_key"),"Clock grants study key");
        Interact("study");await Finish(7);
        await Collect("E01");await Collect("E02");await Collect("E03");await Frames(3);
        if(d.CurrentScene==8)await Finish(8);if(d.CurrentScene==9)await Finish(9);Check(d.State.Done(9),"Study exploration triggers Pavel once");
        await Go("IvanHome");Interact("Ivan");Click("ПОКАЗАТЬ СХЕМУ");await Finish(10);Check(d.State.figures.Contains("Y"),"Ivan hands over Y");
        await Go("DmitryHome");Interact("Dmitry");await Finish(12);if(d.CurrentScene==13)await Finish(13);Check(d.State.figures.Contains("X"),"Dmitry hands over X after scenes12–13");
        await Go("PoliceStation");Interact("Partner");await Finish(14);await Collect("E04");await Collect("E05");await Collect("E06");
        Check(!Item("E07").GetComponent<Collider>().enabled,"Similar case inaccessible before partner documents conversation");
        d.State.Pin("E05");d.State.Pin("E06");d.State.Link("E06","E05");Interact("Partner");await Finish(16);Check(Item("E07").GetComponent<Collider>().enabled,"Partner unlocks similar-case material");
        await Collect("E07");await Frames(3);await Finish(17);Check(!d.State.D("D02_SIMILAR_CASE_PATTERN"),"Main route requires no optional similar-case deduction");
        await Go("AlexeyHome");Interact("Z");await Finish(18);await Collect("E08");await Collect("E09");Check(d.State.figures.Contains("Z"),"Old things grant Z and jacket/letter evidence");
        d.State.Link("D01_CALIBER_MISMATCH","E08");Check(!d.State.D("D03_ALEX_CALIBER"),"Early casing connection stays pending during ActII");
        await Go("Church");Interact("Priest");if(d.CurrentScene==20)await Finish(20);else{await Finish(19);Interact("Priest");await Finish(20);}Check(d.State.figures.Contains("K"),"Priest grants K with schematic and first three figurines");
        await Go("FatherHouse");Interact("cctv");await Finish(21);await Frames(3);Check(d.GetComponent<CaseChase>().Active,"Opening secret passage starts chase without optional board link");
        g.player.Teleport(new Vector3(8,.08f,14),0);d.GetComponent<CaseChase>().kitchen.SetOpen(false);await Frames(3);Check(d.State.Flag("chase_survived"),"Chase completes at closed kitchen safety checkpoint");
        d.Phone();await Finish(22);Interact("Pavel");await Finish(23);
        var solution=ScriptableObject.CreateInstance<FigurineSolution>();solution.leftToRight=new[]{"Z","K","X","Y"};d.solution=solution;
        for(int i=0;i<4;i++)d.State.Place(i,solution.leftToRight[i]);Check(d.State.Solve(solution),"Test-only author configuration solves mechanism without production default");d.ApplyWorld();g.hud.Play();await Frames(3);await Finish(24);
        await Collect("E11");await Frames(3);await Finish(25);await Collect("E10");d.State.Link("E10","E04");d.State.Link("E11","E09");g.hud.Play();await Frames(3);
        await Finish(26);await Finish(27);await Finish(28);await Finish(29);await Finish(30);Check(d.State.Flag("EV_ALEX_GUN_RECOGNITION"),"Required gun recognition event emitted");
        Interact("Pavel");await Finish(31);Click("ПЕРЕДАТЬ ДОКАЗАТЕЛЬСТВА");await Finish(32);await Go("Church");Interact("Priest");g.hud.Play();Check(d.State.Flag("proof_delivered"),"Evidence physically delivered at church");
        await Go("PoliceStation");Interact("Partner");await Finish(33);if(d.CurrentScene==34)await Finish(34);Check(d.State.ending=="truth","Complete route reaches public-evidence ending");
        UnityEngine.Object.Destroy(solution);d.enabled=false;return passed.ToArray();
    }
}
