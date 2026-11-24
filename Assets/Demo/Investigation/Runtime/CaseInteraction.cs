using UnityEngine;
using Hakaton.Demo.FatherHouse;
namespace Hakaton.Demo.Investigation
{
    public sealed class CaseInteraction : DemoInteractable
    {
        public enum ActionKind{Evidence,Person,Phone,Clock,StudyDoor,Secret,Mechanism,OwnThings,Capsule}
        public ActionKind action;
        public string id;
        public DemoDoor door;
        public HouseSecretDoor secret;
        public override void Interact(DemoGame game)
        {
            var d=game.GetComponent<InvestigationDirector>();if(d==null)return;var s=d.State;
            switch(action)
            {
                case ActionKind.Evidence:
                    if((id=="E07"&&!s.Done(16))||((id=="E10"||id=="E11")&&!s.Flag("vault_open"))||(id=="E12"&&!s.Flag("cctv_open")))
                    {game.hud.Notify("Сначала нужен доступ к этим материалам.");return;}
                    d.Find(id);break;
                case ActionKind.Person:d.Talk(id);break;
                case ActionKind.Phone:d.Phone();break;
                case ActionKind.Clock:if(!s.Flag("diamond_key"))d.Speak(6);else d.ShowHint("Часы","За задней крышкой больше ничего нет.");break;
                case ActionKind.StudyDoor:
                    if(!s.Flag("diamond_key")){d.Speak(4);return;}
                    if(door!=null)door.Interact(game);
                    if(!s.Done(7))d.Speak(7);break;
                case ActionKind.Secret:
                    if(id=="vault"){if(!s.Flag("vault_open")){d.Mechanism();return;}}
                    else if(id=="cctv")
                    {
                        if(!s.figures.Contains("K")){game.hud.Notify("Шкаф не поддаётся. На схеме есть ещё один ключ.");return;}
                        s.SetFlag("cctv_open");d.Save();
                    }
                    if(secret!=null)secret.Interact(game);
                    if(id=="cctv"&&!s.Done(21))d.Speak(21);break;
                case ActionKind.Mechanism:d.Mechanism();break;
                case ActionKind.OwnThings:if(s.Ready(18))d.Speak(18);else game.hud.Notify("Сначала нужно закончить разговор в участке.");break;
                case ActionKind.Capsule:if(s.Done(31))d.Choice();else d.ShowHint("Запечатанная капсула","Пока лучше не трогать.");break;
            }
        }
        public void Refresh(InvestigationDirector director)
        {
            var s=director.State;
            if(action==ActionKind.Secret&&secret!=null)
            {if((id=="vault"&&s.Flag("vault_open"))||(id=="cctv"&&s.Flag("cctv_open")))secret.isOpen=true;}
            if(action==ActionKind.Person&&id=="Pavel")
            {
                // Pavel is present only at his authored beats, never identified as a hallucination.
                bool visible=s.Ready(9)||s.Ready(23)||s.Ready(31);foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=visible;
                foreach(var c in GetComponentsInChildren<Collider>())c.enabled=visible;
            }
            if(action==ActionKind.Evidence&&id=="E07")
            {foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=s.Done(16);var c=GetComponent<Collider>();if(c!=null)c.enabled=s.Done(16);}
        }
    }
}
