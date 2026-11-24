using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Hakaton.Demo.Investigation
{
    public sealed class InvestigationBoardView : MonoBehaviour
    {
        public InvestigationDirector director;
        public string Selected{get;private set;}
        public string LinkStart{get;private set;}
        public RectTransform Surface{get;private set;}
        public ScrollRect Scroll{get;private set;}
        public float Zoom{get;private set;}=1;
        RectTransform nodes,edges,inspector;Text hint;
        InputField title,body;string noteSource="";
        readonly Dictionary<string,RectTransform> visuals=new Dictionary<string,RectTransform>();
        CaseState S=>director.State;
        public void Initialize(InvestigationDirector owner)
        {director=owner;Build();}
        void Build()
        {
            CaseUI.Box(transform,"Фон",0,0,1280,720,new Color(.025f,.035f,.028f));
            CaseUI.Text(transform,"ДОСКА РАССЛЕДОВАНИЯ",22,16,700,38,26,CaseUI.Gold);
            CaseUI.Button(transform,"ТЕЛЕФОН",845,12,190,()=>{Commit();director.Phone();});
            CaseUI.Button(transform,"ЗАКРЫТЬ [Tab]",1047,12,210,()=>director.Game.hud.Play());
            CaseUI.Text(transform,"ИНВЕНТАРЬ",16,77,210,25,17,CaseUI.Gold);
            var inventory=CaseUI.Scroll(transform,16,112,218,457,Mathf.Max(457,(S.evidence.Count+S.deductions.Count)*70),out var sc);int i=0;
            foreach(var clue in CaseCatalog.Clues.Where(c=>S.Has(c.id)))
            {
                string id=clue.id;bool connected=S.links.Any(l=>Resolve(l.a)==id||Resolve(l.b)==id);
                bool used=Enumerable.Range(0,CaseCatalog.Recipes.GetLength(0)).Any(r=>S.D(CaseCatalog.Recipes[r,0])&&(CaseCatalog.Recipes[r,1]==id||CaseCatalog.Recipes[r,2]==id));
                var b=CaseUI.Button(inventory,id+"   "+clue.title,4,i++*70,198,()=>{Commit();S.Pin(id);Save();Rebuild();Select(id);},44);
                b.GetComponentInChildren<Text>().fontSize=14;
                CaseUI.Text(inventory,(S.Studied(id)?"изучено":"найдено")+(S.cards.Any(c=>c.id==id)?" · доска":"")+(connected?" · связь":"")+(used?" · вывод":""),7,(i-1)*70+46,194,22,11);
            }
            foreach(string id in S.deductions)
            {var key=id;CaseUI.Button(inventory,CaseCatalog.Title(id),4,i++*70,198,()=>{Commit();S.Pin(key);Save();Rebuild();Select(key);},60);}
            CaseUI.Button(transform,"НОВАЯ ЗАМЕТКА",16,583,218,()=>{Commit();var c=S.Note("Новая запись","");Save();Rebuild();Select(c.id);});
            CaseUI.Button(transform,"ВЕРНУТЬ УДАЛЁННОЕ",16,637,218,()=>{Commit();S.Undo();Save();Rebuild();}).interactable=S.removed.Count>0;
            var bg=CaseUI.Box(transform,"Доска · перетаскивайте пустое поле",248,110,744,548,new Color(.115f,.13f,.10f));bg.gameObject.AddComponent<RectMask2D>();
            Scroll=bg.gameObject.AddComponent<ScrollRect>();Surface=CaseUI.Rect(bg.transform,"Полотно",0,0,2400,1800);Scroll.content=Surface;Scroll.viewport=bg.rectTransform;Scroll.horizontal=Scroll.vertical=true;Scroll.scrollSensitivity=40;Scroll.movementType=ScrollRect.MovementType.Clamped;
            // The background receives drag gestures even between cards.
            var surfaceImage=Surface.gameObject.AddComponent<Image>();surfaceImage.color=new Color(.13f,.145f,.105f);
            edges=CaseUI.Rect(Surface,"Нити",0,0,2400,1800);nodes=CaseUI.Rect(Surface,"Карточки",0,0,2400,1800);
            foreach(var card in S.cards)DrawCard(card);
            RedrawEdges();
            CaseUI.Button(transform,"−",248,64,50,()=>SetZoom(Zoom-.15f),35);
            CaseUI.Button(transform,"+",306,64,50,()=>SetZoom(Zoom+.15f),35);
            CaseUI.Button(transform,"К ЦЕНТРУ",364,64,140,()=>{SetZoom(1);Surface.anchoredPosition=Vector2.zero;},35);
            hint=CaseUI.Text(transform,"Тяните карточки. Связать → другая карточка. Пустое поле — панорама.",248,670,744,36,16);
            inspector=CaseUI.Rect(transform,"Инспектор",1008,76,254,625);
            SetZoom(Zoom);Select(Selected);
        }
        void Rebuild()
        {
            var pos=Surface!=null?Surface.anchoredPosition:Vector2.zero;var selected=Selected;
            foreach(Transform child in transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}visuals.Clear();title=body=null;Build();Surface.anchoredPosition=pos;Select(selected);
        }
        void DrawCard(CaseCard card)
        {
            bool deduction=card.id.StartsWith("D");
            var im=CaseUI.Box(nodes,card.id,card.position.x,card.position.y,242,200,deduction?new Color(.29f,.26f,.16f):new Color(.76f,.72f,.59f));
            var drag=im.gameObject.AddComponent<CaseCardDrag>();drag.board=this;drag.id=card.id;
            visuals[card.id]=im.rectTransform;
            CaseUI.Text(im.transform,deduction?"ВЫВОД":card.id.StartsWith("N-")?"ЛИЧНАЯ ЗАПИСЬ":card.id,12,10,218,22,13,deduction?CaseUI.Gold:new Color(.30f,.27f,.20f));
            CaseUI.Text(im.transform,card.title,12,37,218,54,20,deduction?CaseUI.Ink:new Color(.09f,.09f,.07f));
            var summary=card.text.Length>125?card.text.Substring(0,122)+"…":card.text;
            CaseUI.Text(im.transform,summary,12,100,218,86,15,deduction?CaseUI.Ink:new Color(.15f,.14f,.10f));
        }
        string Resolve(string id){var c=S.cards.Concat(S.removed).FirstOrDefault(v=>v.id==id);return c!=null&&!string.IsNullOrEmpty(c.source)?c.source:id;}
        public void Select(string id)
        {
            if(!string.IsNullOrEmpty(LinkStart)&&!string.IsNullOrEmpty(id)&&id!=LinkStart)
            {
                Commit();string a=LinkStart;LinkStart=null;string result=S.Link(a,id);Save();Rebuild();
                if(result!=null)
                {
                    if(S.D(result)){Selected=result;ShowInspector();director.Game.hud.Notify(CaseCatalog.Body(result),12);}
                    else hint.text="Связь сохранена. Значение находки пока неясно.";
                }
                else hint.text="Связь сохранена. Можно удалить её в инспекторе.";
                return;
            }
            Commit();Selected=id;ShowInspector();
        }
        void ShowInspector()
        {
            foreach(Transform c in inspector){c.gameObject.SetActive(false);Destroy(c.gameObject);}title=body=null;
            var card=S.cards.Find(c=>c.id==Selected);
            if(card==null){CaseUI.Text(inspector,"Выберите карточку.\n\nНеверные связи не мешают расследованию. Вывод остаётся в материалах даже после удаления карточки или нити.",0,10,250,240,18);return;}
            if(card.id.StartsWith("N-"))
            {
                title=CaseUI.Input(inspector,card.title,0,5,254,64,false);body=CaseUI.Input(inspector,card.text,0,82,254,265,true);noteSource=card.source;
                var sources=new List<string>{""};sources.AddRange(S.evidence.Select(e=>e.id));
                CaseUI.Button(inspector,string.IsNullOrEmpty(noteSource)?"Источник: личная запись":"Источник: "+noteSource,0,360,254,()=>
                {int k=(sources.IndexOf(noteSource)+1)%sources.Count;Commit();noteSource=sources[k];card.source=noteSource;Save();ShowInspector();});
            }
            else
            {
                CaseUI.Text(inspector,card.title,0,5,254,75,24,CaseUI.Gold);
                var content=CaseUI.Scroll(inspector,0,86,254,310,Mathf.Max(310,(card.text.Length/24+3)*24),out var s);
                CaseUI.Text(content,card.text,4,4,230,content.sizeDelta.y-8,18);
            }
            CaseUI.Button(inspector,LinkStart==Selected?"ОТМЕНИТЬ СВЯЗЬ":"СВЯЗАТЬ С…",0,410,254,()=>{Commit();LinkStart=LinkStart==Selected?null:Selected;hint.text=LinkStart==null?"Связывание отменено.":"Теперь выберите вторую карточку.";ShowInspector();});
            CaseUI.Button(inspector,"УДАЛИТЬ КАРТОЧКУ",0,462,254,()=>{Commit();S.Remove(Selected);Selected=null;LinkStart=null;Save();Rebuild();});
            CaseUI.Text(inspector,"Связи · нажмите для удаления",0,517,254,25,14,CaseUI.Gold);
            var list=S.links.Where(l=>l.a==card.id||l.b==card.id).ToList();var rows=CaseUI.Scroll(inspector,0,546,254,75,Mathf.Max(75,list.Count*42),out var linksScroll);
            for(int i=0;i<list.Count;i++){var l=list[i];string other=l.a==card.id?l.b:l.a;CaseUI.Button(rows,"× "+CaseCatalog.Title(Resolve(other)),2,i*42,236,()=>{S.Unlink(l.a,l.b);Save();Rebuild();Select(card.id);},38);}
        }
        public void Move(string id,Vector2 delta)
        {
            var card=S.cards.Find(c=>c.id==id);if(card==null)return;
            card.position+=new Vector2(delta.x,-delta.y)/Zoom;card.position=new Vector2(Mathf.Clamp(card.position.x,0,2158),Mathf.Clamp(card.position.y,0,1600));
            visuals[id].anchoredPosition=new Vector2(card.position.x,-card.position.y);RedrawEdges();
        }
        void RedrawEdges()
        {
            foreach(Transform c in edges){c.gameObject.SetActive(false);Destroy(c.gameObject);}
            foreach(var link in S.links)
            {
                var a=S.cards.Find(c=>c.id==link.a);var b=S.cards.Find(c=>c.id==link.b);if(a==null||b==null)continue;
                Vector2 from=new Vector2(a.position.x+121,-a.position.y-22),to=new Vector2(b.position.x+121,-b.position.y-22),d=to-from;
                bool confirmed=Enumerable.Range(0,CaseCatalog.Recipes.GetLength(0)).Any(r=>S.D(CaseCatalog.Recipes[r,0])&&new CaseLink{a=CaseCatalog.Recipes[r,1],b=CaseCatalog.Recipes[r,2]}.Matches(Resolve(link.a),Resolve(link.b)));
                var im=CaseUI.Box(edges,confirmed?"Подтверждённая связь":"Связь",0,0,d.magnitude,confirmed?5:3,confirmed?CaseUI.Gold:new Color(.65f,.20f,.12f));im.raycastTarget=false;im.rectTransform.pivot=new Vector2(0,.5f);im.rectTransform.anchoredPosition=from;im.rectTransform.localEulerAngles=new Vector3(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            }
        }
        public void SetZoom(float value){Zoom=Mathf.Clamp(value,.45f,1.35f);Surface.localScale=Vector3.one*Zoom;}
        public void Commit(){var c=S.cards.Find(v=>v.id==Selected);if(c!=null&&title!=null){c.title=title.text;c.text=body.text;c.source=noteSource;}Save();}
        public void Save()=>director.Save();
    }
}
