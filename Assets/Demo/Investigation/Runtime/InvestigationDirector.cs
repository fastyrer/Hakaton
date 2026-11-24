using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Hakaton.Demo.Investigation
{
    [Serializable] public sealed class CaseDialogueLine {public string speaker,text;}
    [Serializable] public sealed class CaseDialogue {public int id;public string title;public CaseDialogueLine[] lines;}
    [Serializable] public sealed class CaseDialogueCollection {public CaseDialogue[] scenes;}
    public sealed class InvestigationDirector : MonoBehaviour
    {
        public FigurineSolution solution;
        public DemoGame Game{get;private set;}
        public CaseState State=>CaseFile.Current;
        public bool ConversationOpen{get;private set;}
        public int CurrentScene{get;private set;}
        public string CurrentLine{get;private set;}
        public string Objective=>GetComponent<CaseChase>()?.Active==true?"Бегите на кухню на первом этаже · закройте за собой дверь [E]":State.Objective;
        CaseDialogueCollection dialogues;
        CaseDialogue dialogue;int line;Action after;bool cinematicHold;
        int dialogueFrame;
        float saveAfter;bool initialized;Action resume;
        readonly Queue<int> sequence=new Queue<int>();
        public void Initialize(DemoGame game)
        {
            Game=game;initialized=true;
            dialogues=JsonUtility.FromJson<CaseDialogueCollection>(Resources.Load<TextAsset>("CaseDialogues").text);
            var pose=State.poses.Find(p=>p.scene==game.gameObject.scene.name);
            if(pose!=null&&!State.Flag("chase_running"))game.player.Teleport(pose.position,pose.yaw);
            // An interrupted scene is replayed rather than being silently consumed.
            CurrentScene=State.activeEvent;
            ApplyWorld();Save();
        }
        void Update()
        {
            if(!initialized||!Game.player.enabled)return;
            if(Time.unscaledTime>saveAfter){saveAfter=Time.unscaledTime+12;Save();}
            if(ConversationOpen)
            {var k=UnityEngine.InputSystem.Keyboard.current;if(Time.frameCount>dialogueFrame&&k!=null&&(k.spaceKey.wasPressedThisFrame||k.enterKey.wasPressedThisFrame))NextLine();return;}
            if(Game.Blocked||GetComponent<CaseChase>()?.Active==true)return;
            if(resume!=null){var a=resume;resume=null;a();return;}
            if(SceneName=="FatherHouse"&&!State.Done(3)&&!State.Flag("arrival_call_seen"))
            {
                State.SetFlag("arrival_call_seen");Save();Show("ВХОДЯЩИЙ ВЫЗОВ","Напарник","Телефон вибрирует в кармане.",new[]{"ОТВЕТИТЬ","СБРОСИТЬ"},new Action[]{()=>Speak(2),Game.hud.Play});return;
            }
            if(CurrentScene!=0){int n=CurrentScene;CurrentScene=0;if(!State.Done(n)&&State.Ready(n)){Speak(n);return;}}
            if(State.Flag("chase_running")&&!State.Flag("chase_survived")){GetComponent<CaseChase>().Begin();return;}
            if(SceneName=="FatherHouse"&&!State.Done(3)&&Game.player.transform.position.z>1&&Game.player.transform.position.z<8&&Mathf.Abs(Game.player.transform.position.x)<2){Speak(3);return;}
            if(State.Ready(9)&&!State.Done(9)&&SceneName=="FatherHouse"){if(State.Has("E03")&&!State.Done(8))Speak(8,()=>Speak(9));else Speak(9);return;}
            foreach(int n in new[]{17,24,25,26,27,28,29,30,34,36})
            {
                if(!State.Done(n)&&State.Ready(n)&&AutomaticHere(n)){Speak(n);return;}
            }
        }
        bool AutomaticHere(int n)=>(n==17||n==34)?SceneName=="PoliceStation":SceneName=="FatherHouse";
        public string SceneName=>gameObject.scene.name;
        public void Save()
        {
            if(!initialized)return;State.scene=SceneName;
            if(GetComponent<CaseChase>()?.Active!=true)
            {
                var p=State.poses.Find(v=>v.scene==SceneName);if(p==null){p=new CasePose{scene=SceneName};State.poses.Add(p);}
                p.position=Game.player.transform.position;p.yaw=Game.player.transform.eulerAngles.y;
            }
            if(!CaseFile.Save())Game.hud.Notify("Не удалось сохранить игру. Проверьте свободное место.",8);
        }
        void OnApplicationPause(bool value){if(value)Save();}
        void OnApplicationQuit()=>Save();
        public void Find(string id)
        {
            State.Find(id);State.Study(id);Save();
            string body=CaseCatalog.Body(id);
            if(id=="E08"&&!State.Done(26))body="Гильза… Опять после стрельбища карманы не вытряхнул.";
            Show("УЛИКА / "+id,CaseCatalog.Title(id),body,new[]{"ПРИКРЕПИТЬ НА ДОСКУ","УБРАТЬ"},new Action[]{()=>{State.Pin(id);Save();Game.hud.ShowBoard();},Game.hud.Play});
        }
        public void Show(string category,string title,string text,string[] labels,Action[] actions)
        {
            var p=Game.hud.CasePanel(category,title);int count=labels.Length;
            var content=CaseUI.Scroll(p,42,164,916,310,Mathf.Max(310,(text.Length/65+text.Count(c=>c=='\n')+2)*26),out var scroll);
            CaseUI.Text(content,text,10,8,880,content.sizeDelta.y-16,22);
            float w=(916-(count-1)*12)/(float)Mathf.Max(1,count);
            for(int i=0;i<count;i++)CaseUI.Button(p,labels[i],42+i*(w+12),502,w,actions[i],54);
        }
        public void Speak(int n,Action completion=null)
        {
            if(State.Done(n)){ShowHint("Разговор","Этот разговор уже состоялся. Всё важное осталось в материалах дела.");return;}
            if(!State.Ready(n)){Game.hud.Notify("Сначала разберитесь с предыдущими находками.");return;}
            dialogue=dialogues.scenes.First(s=>s.id==n);CurrentScene=n;line=0;after=completion;ConversationOpen=true;State.activeEvent=n;GetComponent<CaseMemory>()?.Begin(n,Game);Save();RenderLine();
        }
        void RenderLine()
        {
            var item=dialogue.lines[line];CurrentLine=item.text;
            string who=item.speaker;
            if(who=="+"){for(int previous=line-1;previous>=0;previous--)if(dialogue.lines[previous].speaker!="+"&&dialogue.lines[previous].speaker!="@"){who=dialogue.lines[previous].speaker;break;}}
            var names=new Dictionary<string,string>{{"А","Алексей"},{"Н","Напарник"},{"И","Иван"},{"Д","Дмитрий"},{"П","Павел"},{"С","Священник"},{"Ф","Фёдор"},{"Ф, запись","Фёдор · запись"},{"А, читает","Алексей читает"},{"+",""},{"@",""}};
            if(names.TryGetValue(who,out var name))who=name;
            bool stage=item.speaker=="@";
            // Author's production comments are instructions for staging, never presented as narration.
            if(stage){HandleStage(item.text);line++;
                if(CurrentScene==29&&item.text.StartsWith("Выстрел")){StartCoroutine(ShotPause());return;}
                if(line>=dialogue.lines.Length)FinishDialogue();else RenderLine();return;}
            dialogueFrame=Time.frameCount;
            var panel=Game.hud.CaseConversationPanel(who==""?dialogue.title:who);
            CaseUI.Text(panel,item.text,24,54,990,102,22);
            CaseUI.Button(panel,"ДАЛЕЕ [ПРОБЕЛ / ENTER]",24,172,990,NextLine,42);
        }
        public void NextLine(){if(!ConversationOpen||cinematicHold)return;line++;if(line>=dialogue.lines.Length)FinishDialogue();else RenderLine();}
        void HandleStage(string stage)
        {
            GetComponent<CaseMemory>()?.Stage(stage,Game);
        }
        System.Collections.IEnumerator ShotPause()
        {
            cinematicHold=true;Game.hud.CaseConversationPanel("");
            yield return new WaitForSecondsRealtime(1.15f);cinematicHold=false;
            if(line>=dialogue.lines.Length)FinishDialogue();else RenderLine();
        }
        public void ResumeConversation(){if(ConversationOpen&&!cinematicHold)RenderLine();}
        void FinishDialogue()
        {
            int n=CurrentScene;State.Complete(n);CurrentScene=0;ConversationOpen=false;Game.hud.Play();
            switch(n)
            {
                case 6:State.SetFlag("diamond_key");break;
                case 10:AddFigure("Y");break;
                case 13:AddFigure("X");break;
                case 18:AddFigure("Z");break;
                case 20:AddFigure("K");break;
                case 21:State.SetFlag("chase_running");break;
                case 27:State.SetFlag("EV_ALEX_GUN_RECOGNITION");break;
                case 34:State.ending="truth";break;
                case 36:State.ending="capsule";break;
            }
            GetComponent<CaseMemory>()?.End(n);
            ApplyWorld();Save();var callback=after;after=null;
            if(callback!=null){resume=callback;return;}
            if(n==12)resume=()=>Speak(13);
            if(n==22)Game.hud.Notify("У выхода снова слышен голос Павла.",6);
            if(n==29)resume=()=>Speak(30);
            if(n==34||n==36)ShowEnding();
        }
        void AddFigure(string id){if(!State.figures.Contains(id))State.figures.Add(id);Game.hud.Notify("Получена фигурка "+id,6);}
        public void ApplyWorld()
        {
            foreach(var item in FindObjectsByType<CaseInteraction>(FindObjectsInactive.Include))item.Refresh(this);
        }
        public void Talk(string person)
        {
            switch(person)
            {
                case "Ivan":
                    var opts=new List<string>();var acts=new List<Action>();
                    if(State.Has("E01")&&!State.Done(10)){opts.Add("ПОКАЗАТЬ СХЕМУ");acts.Add(()=>Speak(10));}
                    if(State.Has("E13")&&!State.Done(5)){opts.Add("ПОКАЗАТЬ ФОТО");acts.Add(()=>Speak(5));}
                    if(!State.Done(11)){opts.Add("ОБ ОТЦЕ");acts.Add(()=>Speak(11));}opts.Add("УЙТИ");acts.Add(Game.hud.Play);
                    Show("РАЗГОВОР","Иван","Что спросить у Ивана?",opts.ToArray(),acts.ToArray());break;
                case "Dmitry":if(State.Done(13))ShowHint("Дмитрий","Фигурку я тебе уже отдал. Найдёшь что-нибудь — расскажи.");else if(State.Has("E01"))Speak(State.Done(12)?13:12);else ShowHint("Дмитрий","Пока не знаю, о чём спросить. Сначала осмотрю кабинет отца.");break;
                case "Partner":
                    if(State.Ready(33)&&!State.Done(33))Speak(33,()=>Speak(34));
                    else if(State.Ready(16)&&!State.Done(16))Speak(16);
                    else if(State.Done(16))ShowHint("Напарник","Материалы похожего дела лежат в архиве. Держи меня в курсе.");
                    else Speak(14);break;
                case "Archivist":Speak(15);break;
                case "Priest":
                    if(State.branch=="truth"&&State.Done(32)){State.SetFlag("proof_delivered");Save();ShowHint("Священник","Я сделаю копии. Теперь иди к напарнику.");}
                    else if(!State.Done(19))Speak(19);
                    else if(State.Ready(20)&&!State.Done(20))Speak(20);
                    else ShowHint("Священник","Покажу схему, когда найду первые три фигурки.");break;
                case "Pavel":
                    if(State.Done(31))Choice();
                    else if(State.Ready(31))Speak(31,Choice);
                    else if(State.Ready(23)&&!State.Done(23))Speak(23);
                    else if(State.Ready(9)&&!State.Done(9))Speak(9);
                    else ShowHint("Павел","Пока нечего ему показывать.");break;
            }
        }
        public void ShowHint(string title,string body)=>Show("ЗАПИСИ",title,body,new[]{"ЗАКРЫТЬ"},new Action[]{Game.hud.Play});
        public void Phone()
        {
            if(State.Ready(22)&&!State.Done(22)){Speak(22);return;}
            if(State.Ready(32)&&!State.Done(32)){Speak(32);return;}
            if(!State.Done(4)&&!State.Flag("diamond_key")){Speak(4);return;}
            Show("ТЕЛЕФОН","Вызов", "Кому позвонить?",new[]{"НАПАРНИК","ИВАН","УБРАТЬ"},new Action[]{()=>Speak(2),()=>Speak(State.Has("E13")?5:4),Game.hud.Play});
        }
        public void Choice()
        {
            if(State.ending!=""){ShowEnding();return;}
            if(State.branch=="truth"){ShowHint("Доказательства",State.Flag("proof_delivered")?"Нужно встретиться с напарником.":"Нужно передать документы священнику.");return;}
            if(State.branch=="capsule"){if(!State.Done(35))Speak(35,()=>Speak(36));else if(!State.Done(36))Speak(36);else ShowEnding();return;}
            Show("РЕШЕНИЕ","Что теперь?","Документы можно передать священнику, а затем рассказать напарнику всё. Или принять капсулу из хранилища.",new[]{"ПЕРЕДАТЬ ДОКАЗАТЕЛЬСТВА","ПРИНЯТЬ КАПСУЛУ","ПОДУМАТЬ"},new Action[]{()=>{State.branch="truth";Save();Speak(32);},()=>{State.branch="capsule";Save();Speak(35,()=>Speak(36));},Game.hud.Play});}
        void ShowEnding()=>Show("КОНЕЦ","Дом помнит",State.ending=="truth"?"Документы опубликованы. Алексей дал показания. Дела будут пересмотрены.":"Папка осталась неразобранной. Жизнь снова кажется нормальной.",new[]{"МАТЕРИАЛЫ ДЕЛА","ГЛАВНОЕ МЕНЮ"},new Action[]{Game.hud.ShowBoard,()=>SceneManager.LoadScene("MainMenu")});
        public void TravelMenu()
        {
            var names=new[]{"Дом отца","Иван","Дмитрий","Участок","Дом Алексея","Церковь"};var scenes=new[]{"FatherHouse","IvanHome","DmitryHome","PoliceStation","AlexeyHome","Church"};
            var p=Game.hud.CasePanel("МАШИНА / МАРШРУТ","Куда поедем?");
            for(int i=0;i<names.Length;i++)
            {
                int k=i;bool available=i==0||(i==1||i==2?State.Done(9):i==3?State.figures.Contains("X")&&State.figures.Contains("Y"):i==4?State.Done(17):State.figures.Contains("Z")||State.branch=="truth");
                var b=CaseUI.Button(p,names[i]+(scenes[i]==SceneName?" · вы здесь":""),42+(i%2)*464,170+(i/2)*80,452,()=>Travel(scenes[k]),60);b.interactable=available&&scenes[i]!=SceneName;
            }
            CaseUI.Text(p,"Новые маршруты появляются по мере расследования.\nПрогресс и доска сохраняются автоматически.",42,420,900,60,18);
            CaseUI.Button(p,"ОСТАТЬСЯ",42,502,916,Game.hud.Play,54);
        }
        public void Travel(string scene)
        {if(GetComponent<CaseChase>()?.Active==true)return;Save();State.scene=scene;CaseFile.Save();Time.timeScale=1;SceneManager.LoadScene(scene);}
        public void Mechanism()
        {
            if(State.figures.Count<4){ShowHint("Механизм","Здесь четыре гнезда. Сначала нужно собрать все фигурки.");return;}
            var p=Game.hud.CasePanel("ХРАНИЛИЩЕ / ЧЕТЫРЕ ГНЁЗДА","Механизм фигурок");
            CaseUI.Text(p,"Слева направо. Нажмите на гнездо, чтобы выбрать следующую фигурку.\nФигурки можно переставлять и убирать сколько угодно.",42,167,916,65,21);
            for(int i=0;i<4;i++)
            {
                int k=i;string value=State.sockets[i];CaseUI.Button(p,(i+1)+"   [ "+(string.IsNullOrEmpty(value)?"—":value)+" ]",42+i*230,265,214,()=>{var options=new List<string>{""};options.AddRange(State.figures);int next=(options.IndexOf(State.sockets[k]??"")+1)%options.Count;State.Place(k,options[next]);Save();Mechanism();},90);
            }
            CaseUI.Text(p,"С собой: "+string.Join(", ",State.figures),42,385,916,40,20);
            CaseUI.Button(p,"ПРОВЕРИТЬ",42,502,450,()=>
            {
                if(State.Solve(solution)){ApplyWorld();Save();Game.hud.Play();}
                else if(solution==null||!solution.Valid)ShowHint("Механизм","Порядок фигурок пока неизвестен.");
                else ShowHint("Механизм не открылся",State.Done(23)?"Порядок не подошёл. Попробую переставить фигурки.":"Сначала нужно разобраться с тайной комнатой и тем, кто был в доме.");
            },54);
            CaseUI.Button(p,"ОТОЙТИ",510,502,448,Game.hud.Play,54);
        }
    }
}
