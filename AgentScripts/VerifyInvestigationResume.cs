using System;
using System.Linq;
using System.Threading.Tasks;
using Hakaton.Demo;
using Hakaton.Demo.Investigation;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifyInvestigationResume
{
    static async Task Frames(int n=3){int end=Time.frameCount+n;while(Time.frameCount<end)await Task.Yield();}
    static async Task<InvestigationDirector> Load(){SceneManager.LoadScene("IvanHome");await Frames(4);return UnityEngine.Object.FindAnyObjectByType<InvestigationDirector>();}
    public static async Task<string[]> Main()
    {
        CaseFile.TestPath="/tmp/hakaton-investigation-resume-test.json";CaseFile.NewGame();CaseFile.Current.Study("E01");CaseFile.Current.Complete(3);CaseFile.Current.Complete(9);
        var d=await Load();d.Game.hud.Play();d.Speak(10);d.NextLine();d.Save();
        d=await Load();if(d.State.activeEvent!=10)throw new Exception("Interrupted event lost during arrival introduction");d.Save();
        d=await Load();if(d.State.activeEvent!=10)throw new Exception("Repeated load consumed interrupted event");d.Game.hud.Play();await Frames(4);
        if(!d.ConversationOpen||d.CurrentScene!=10)throw new Exception("Interrupted dialogue did not resume");
        int safety=100;while(d.ConversationOpen&&safety-->0)d.NextLine();if(!d.State.Done(10)||d.State.figures.Count(x=>x=="Y")!=1)throw new Exception("Resumed dialogue did not grant exactly one figure");d.Save();
        d=await Load();d.Game.hud.Play();await Frames(4);if(d.ConversationOpen||d.State.figures.Count(x=>x=="Y")!=1)throw new Exception("Completed event repeated after reload");
        d.Speak(10);if(d.ConversationOpen||d.State.completed.Count(x=>x==10)!=1)throw new Exception("One-shot conversation replayed");d.enabled=false;
        return new[]{"Interrupted event survives reloading before closing arrival introduction","Interrupted dialogue resumes with authored text","Resumed event grants figurine exactly once","Completed one-shot event does not repeat after loading or reinteraction"};
    }
}
