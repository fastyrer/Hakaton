using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Hakaton.Demo.Investigation
{
    [Serializable] public sealed class CaseEvidence {public string id;public bool studied;}
    [Serializable] public sealed class CaseCard {public string id,source,title,text;public Vector2 position;}
    [Serializable] public sealed class CaseLink {public string a,b;public bool Matches(string x,string y)=>(a==x&&b==y)||(a==y&&b==x);}
    [Serializable] public sealed class CasePose {public string scene;public Vector3 position;public float yaw;}
    [Serializable] public sealed class CaseState
    {
        public int version=1;
        public string scene="FatherHouse",ending="",branch="";
        public List<CaseEvidence> evidence=new List<CaseEvidence>();
        public List<CaseCard> cards=new List<CaseCard>();
        public List<CaseCard> removed=new List<CaseCard>();
        public List<CaseLink> links=new List<CaseLink>();
        public List<string> deductions=new List<string>(),pendingDeductions=new List<string>(),flags=new List<string>(),figures=new List<string>();
        public List<int> completed=new List<int>();
        public List<CasePose> poses=new List<CasePose>();
        public string[] sockets=new string[4];
        public int activeEvent;
        public bool Has(string id)=>evidence.Any(e=>e.id==id);
        public bool Studied(string id)=>evidence.Any(e=>e.id==id&&e.studied);
        public bool Done(int n)=>completed.Contains(n);
        public bool Flag(string id)=>flags.Contains(id);
        public bool D(string id)=>deductions.Contains(id);
        public void SetFlag(string id){if(!Flag(id))flags.Add(id);}
        public void Complete(int n){if(!Done(n))completed.Add(n);activeEvent=0;Refresh();}
        public void Find(string id){if(CaseCatalog.Find(id)==null)return;if(!Has(id))evidence.Add(new CaseEvidence{id=id});}
        public void Study(string id){Find(id);var e=evidence.Find(v=>v.id==id);if(e!=null)e.studied=true;Refresh();}
        public bool Available(string id)=>Has(id)||D(id)||cards.Any(c=>c.id==id&&string.IsNullOrEmpty(c.source));
        public CaseCard Pin(string id)
        {
            if(!Available(id))return null;
            var existing=cards.Find(c=>c.id==id);if(existing!=null)return existing;
            var card=new CaseCard{id=id,source=id,title=CaseCatalog.Title(id),text=CaseCatalog.Body(id),position=new Vector2(60+(cards.Count%5)*265,60+(cards.Count/5)*235)};
            cards.Add(card);return card;
        }
        public CaseCard Note(string title,string text,string source="")
        {
            if(!string.IsNullOrEmpty(source)&&!Has(source))return null;
            var c=new CaseCard{id="N-"+Guid.NewGuid().ToString("N"),title=title,text=text,source=source,position=new Vector2(90,90)};cards.Add(c);return c;
        }
        public void Remove(string id){var c=cards.Find(v=>v.id==id);if(c==null)return;cards.Remove(c);removed.Add(c);}
        public void Undo(){if(removed.Count==0)return;var c=removed[removed.Count-1];removed.RemoveAt(removed.Count-1);if(!cards.Any(v=>v.id==c.id))cards.Add(c);}
        public string Link(string a,string b)
        {
            if(a==b||!NodeAvailable(a)||!NodeAvailable(b))return null;
            if(links.Any(l=>l.Matches(a,b)))return null;
            links.Add(new CaseLink{a=a,b=b});
            for(int r=0;r<CaseCatalog.Recipes.GetLength(0);r++)
                if(new CaseLink{a=CaseCatalog.Recipes[r,1],b=CaseCatalog.Recipes[r,2]}.Matches(Resolve(a),Resolve(b)))
                {var d=CaseCatalog.Recipes[r,0];if(!D(d)&&!pendingDeductions.Contains(d))pendingDeductions.Add(d);Refresh();return d;}
            return null;
        }
        public string Resolve(string id){var c=cards.Concat(removed).FirstOrDefault(v=>v.id==id);return c!=null&&!string.IsNullOrEmpty(c.source)?c.source:id;}
        bool NodeAvailable(string id)=>Available(id)||cards.Any(c=>c.id==id);
        public void Unlink(string a,string b)=>links.RemoveAll(l=>l.Matches(a,b));
        public void Refresh()
        {
            foreach(var id in pendingDeductions.ToArray())
            {
                // A connection can be made early, but recognition is a later story beat.
                if(id=="D03_ALEX_CALIBER"&&!Done(26))continue;
                pendingDeductions.Remove(id);if(!D(id)){deductions.Add(id);Pin(id);}
            }
        }
        public string EventState(int n)=>Done(n)?"triggered":activeEvent==n?"running":Ready(n)?"ready":"not_ready";
        public bool Ready(int n)
        {
            switch(n)
            {
                case 9:return Studied("E01")&&Studied("E02");
                case 10:case 12:return Has("E01");
                case 13:return Done(12);
                case 16:return D("D01_CALIBER_MISMATCH");
                case 17:return Done(16)&&Studied("E07");
                case 18:return Has("E01")&&Done(17);
                case 20:return Has("E01")&&new[]{"X","Y","Z"}.All(figures.Contains)&&Done(17);
                case 21:return figures.Contains("K")&&Flag("cctv_open");
                case 22:return Flag("chase_survived");
                case 23:return Done(22);
                case 24:return Flag("vault_open");
                case 25:return Studied("E11")&&Done(24);
                case 26:return D("D04_DATE_CONTRADICTION")&&Done(25);
                case 27:return Done(26)&&D("D03_ALEX_CALIBER")&&D("D05_LETTER_OPERATION");
                case 28:return Done(27);
                case 29:return Done(28);
                case 30:return Done(29);
                case 31:return Done(30);
                case 32:return Done(31)&&branch=="truth";
                case 33:return Done(32)&&Flag("proof_delivered");
                case 34:return Done(33);
                case 35:return Done(31)&&branch=="capsule";
                case 36:return Done(35);
                default:return true;
            }
        }
        public bool Place(int slot,string figure)
        {
            if(slot<0||slot>3||(!string.IsNullOrEmpty(figure)&&!figures.Contains(figure)))return false;
            for(int i=0;i<4;i++)if(sockets[i]==figure)sockets[i]="";
            sockets[slot]=figure;return true;
        }
        public bool Solve(FigurineSolution solution)
        {if(!Done(23)||!new[]{"X","Y","Z","K"}.All(figures.Contains)||solution==null||!solution.Valid||!sockets.SequenceEqual(solution.leftToRight))return false;SetFlag("vault_open");return true;}
        public string Objective
        {
            get
            {
                if(ending!="")return "Расследование завершено · Tab — материалы дела";
                if(Done(31))return branch=="truth"?(Flag("proof_delivered")?"Встретьтесь с напарником в участке":"Передайте документы священнику"):"Решите, что делать с капсулой в хранилище";
                if(Done(30))return "Поговорите с Павлом в кабинете";
                if(Done(26))return "Сопоставьте гильзу с разными калибрами, письмо — с папкой Фёдора";
                if(Done(25))return "На доске сравните даты в деле и документе из хранилища";
                if(Flag("vault_open"))return "Изучите документы и прослушайте запись Фёдора в хранилище";
                if(Done(23))return "Установите четыре фигурки в механизм перед хранилищем";
                if(Flag("chase_survived"))return "Позвоните напарнику и найдите Павла у выхода";
                if(figures.Contains("K"))return "Вернитесь к отцу · найдите тайный проход за шкафом кабинета";
                if(Done(17)&&figures.Contains("Z"))return "Возьмите письмо и гильзу дома · покажите схему священнику";
                if(Done(17))return "Поищите фигурку, письмо и старую куртку у себя дома";
                if(Done(16))return "Изучите похожее дело в архиве";
                if(D("D01_CALIBER_MISMATCH"))return "Покажите вывод о калибрах напарнику";
                if(figures.Contains("X")&&figures.Contains("Y"))return "В участке изучите дело отца и сопоставьте сведения об оружии на доске [Tab]";
                if(Done(9))return "Покажите схему Ивану и Дмитрию · машина во дворе открывает маршруты";
                if(Has("E01"))return "Осмотрите схему, следы на ковре и бутылку в кабинете";
                if(Flag("diamond_key"))return "Откройте кабинет отца бубновым ключом";
                return "Осмотрите дом · ключ от кабинета может быть в часах";
            }
        }
    }
    public static class CaseFile
    {
        static CaseState state;
        public static string TestPath;
        public static string Path=>TestPath??System.IO.Path.Combine(Application.persistentDataPath,"investigation-v1.json");
        public static string LastError{get;private set;}
        public static CaseState Current=>state??(state=Load()??new CaseState());
        public static bool Exists=>Read(Path)!=null||Read(Path+".bak")!=null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetMemory(){state=null;TestPath=null;LastError=null;}
        public static void NewGame(){state=new CaseState();Save();}
        public static void Reload(){state=Load()??new CaseState();}
        static CaseState Read(string path)
        {
            try{if(!File.Exists(path))return null;var json=File.ReadAllText(path);if(!json.Contains("\"version\"")||!json.Contains("\"evidence\"")||!json.Contains("\"cards\""))return null;var s=JsonUtility.FromJson<CaseState>(json);return s!=null&&s.version==1&&s.evidence!=null&&s.cards!=null&&s.completed!=null&&s.links!=null&&s.flags!=null&&s.removed!=null&&s.deductions!=null&&s.pendingDeductions!=null&&s.poses!=null&&s.figures!=null&&s.sockets!=null&&s.sockets.Length==4?s:null;}catch{return null;}
        }
        static CaseState Load()=>Read(Path)??Read(Path+".bak");
        public static bool Save()
        {
            try
            {
                var json=JsonUtility.ToJson(Current,true);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));File.WriteAllText(Path+".tmp",json);
                if(File.Exists(Path)){if(Read(Path)!=null)File.Copy(Path,Path+".bak",true);File.Delete(Path);}File.Move(Path+".tmp",Path);LastError=null;return true;
            }
            catch(Exception e){LastError=e.Message;Debug.LogError("Не удалось сохранить расследование: "+e.Message);return false;}
        }
    }
}
