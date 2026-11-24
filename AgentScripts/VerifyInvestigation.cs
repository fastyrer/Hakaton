using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hakaton.Demo.Investigation;
using UnityEngine;
public static class VerifyInvestigation
{
    static List<string> passed=new List<string>();
    static void Check(bool ok,string title){if(!ok)throw new Exception(title);passed.Add(title);}
    public static string[] Main()
    {
        passed.Clear();
        var s=new CaseState();
        Check(s.Link("E05","E06")==null,"Unowned evidence cannot be combined");
        foreach(var e in CaseCatalog.Clues)s.Study(e.id);
        Check(s.Link("E03","E09")==null&&s.deductions.Count==0,"Wrong links remain editable and cause no discoveries");s.Unlink("E09","E03");Check(s.links.Count==0,"Wrong link can be removed in reverse order");
        Check(s.Link("E06","E05")=="D01_CALIBER_MISMATCH"&&s.Ready(16),"Reversed caliber recipe creates D01 and unlocks partner");
        s.Link("D01_CALIBER_MISMATCH","E08");Check(!s.D("D03_ALEX_CALIBER")&&s.pendingDeductions.Contains("D03_ALEX_CALIBER"),"Early casing combination is deferred without recognition spoiler");
        s.Link("E07","D01_CALIBER_MISMATCH");Check(s.D("D02_SIMILAR_CASE_PATTERN"),"Optional similar-case hypothesis");
        s.Complete(16);Check(s.Ready(17),"Scene17 depends on studying E07 and scene16");
        var independent=new CaseState();independent.Study("E07");independent.Complete(16);Check(independent.Ready(17)&&!independent.D("D02_SIMILAR_CASE_PATTERN"),"Scene17 works without optional D02");
        s.Link("E10","E04");s.Link("E11","E09");Check(!s.Ready(26)&&!s.Ready(27)&&!s.Ready(28),"Premature discoveries do not start final memory sequence");
        s.Complete(24);s.Complete(25);Check(s.Ready(26)&&!s.Ready(27),"Recording completion gates real-date conversation");
        s.Complete(26);Check(s.D("D03_ALEX_CALIBER")&&s.Ready(27)&&!s.Ready(28),"Pending casing recognition resolves only after scene26");
        for(int n=27;n<=30;n++){Check(s.Ready(n),"Memory scene "+n+" ready in order");s.Complete(n);Check(s.EventState(n)=="triggered","Memory scene "+n+" consumed exactly once");}
        Check(s.Ready(31),"Final choice is locked until confession");
        s.Unlink("E06","E05");s.Remove("D01_CALIBER_MISMATCH");Check(s.D("D01_CALIBER_MISMATCH"),"Deduction survives deletion of thread and card");s.Undo();Check(s.cards.Any(c=>c.id=="D01_CALIBER_MISMATCH"),"Undo restores same stable ID");
        var note=s.Note("Моя версия","Проверить документы","E05");Check(note!=null&&s.Note("bad","bad","E99")==null,"Own notes accept only collected evidence sources");note.position=new Vector2(423,611);
        s.Link("E12","E02");Check(s.D("D06_UNKNOWN_VISITOR"),"Optional anonymous visitor recipe");
        var puzzle=new CaseState();puzzle.figures.AddRange(new[]{"X","Y","Z","K"});puzzle.Complete(23);
        var solution=ScriptableObject.CreateInstance<FigurineSolution>();solution.leftToRight=new[]{"K","Z","Y","X"};
        for(int i=0;i<4;i++)puzzle.Place(i,new[]{"X","Y","Z","K"}[i]);Check(!puzzle.Solve(solution),"Wrong figurine order does not open vault and permits retry");
        for(int i=0;i<4;i++)puzzle.Place(i,solution.leftToRight[i]);Check(puzzle.Solve(solution)&&puzzle.Ready(24),"Configured figurine solution opens vault and scene24");
        var none=ScriptableObject.CreateInstance<FigurineSolution>();Check(!none.Valid&&!new CaseState().Solve(none),"No invented default solution; scheme alone cannot open vault");
        var path="/tmp/hakaton-investigation-model-test.json";string previous=CaseFile.TestPath;
        try
        {
            CaseFile.TestPath=path;CaseFile.NewGame();var saved=CaseFile.Current;
            foreach(var e in CaseCatalog.Clues)saved.Study(e.id);saved.Link("E05","E06");saved.Link("D01_CALIBER_MISMATCH","E08");saved.activeEvent=26;saved.scene="PoliceStation";var n=saved.Note("Проверить","Текст с переносом\nстроки","E04");n.position=new Vector2(420,620);string stable=n.id;
            saved.figures.Add("Y");saved.sockets[0]="Y";CaseFile.Save();CaseFile.Reload();var restored=CaseFile.Current;
            Check(restored.cards.Any(c=>c.id==stable&&c.position==new Vector2(420,620)&&c.source=="E04")&&restored.links.Count==2,"Disk roundtrip preserves IDs, coordinates, notes and connections");
            Check(restored.activeEvent==26&&restored.pendingDeductions.Contains("D03_ALEX_CALIBER")&&restored.sockets[0]=="Y"&&restored.figures.Contains("Y"),"Disk roundtrip preserves running/pending events and figurines");
            CaseFile.Save();File.WriteAllText(path,"not valid json");CaseFile.Reload();Check(CaseFile.Current.cards.Any(c=>c.id==stable),"Backup recovers interrupted/corrupt primary save");
        }
        finally{CaseFile.TestPath=previous;CaseFile.Reload();UnityEngine.Object.DestroyImmediate(solution);UnityEngine.Object.DestroyImmediate(none);}
        return passed.ToArray();
    }
}
