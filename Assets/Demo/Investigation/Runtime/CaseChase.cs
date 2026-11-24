using System.Collections.Generic;
using UnityEngine;
namespace Hakaton.Demo.Investigation
{
    public sealed class CaseChase : MonoBehaviour
    {
        public Transform pursuer;
        public DemoDoor kitchen,study;
        public bool Active{get;private set;}
        InvestigationDirector director;
        readonly List<Vector3> path=new List<Vector3>();
        float repath,grace;
        const float Step=.55f;const int W=45,H=52;
        void Awake(){director=GetComponent<InvestigationDirector>();if(pursuer!=null)pursuer.gameObject.SetActive(false);}
        public void Begin()
        {
            Active=true;director.State.SetFlag("chase_running");director.Save();
            director.Game.player.Teleport(new Vector3(-5,.08f,12),90);kitchen.SetOpen(true);study?.SetOpen(true);
            pursuer.position=new Vector3(-5,0,17);pursuer.gameObject.SetActive(true);grace=3;repath=0;path.Clear();director.Game.hud.Play();director.Game.sound.Stinger();director.Game.hud.Notify("На кухню! Закройте за собой дверь.",7);
        }
        void Update()
        {
            if(!Active||director.Game==null||director.Game.Blocked)return;
            Vector3 player=director.Game.player.transform.position;
            if(player.x>6.5f&&player.z>10.3f&&player.z<17.7f&&Mathf.Abs(player.y)<.5f&&!kitchen.isOpen)
            {
                Active=false;pursuer.gameObject.SetActive(false);director.State.flags.Remove("chase_running");director.State.SetFlag("chase_survived");director.Save();director.Game.hud.Notify("Тихо. Нужно позвонить напарнику — телефон на доске [Tab].",8);return;
            }
            grace-=Time.deltaTime;if(grace>0)return;
            repath-=Time.deltaTime;if(repath<=0){repath=.7f;BuildPath(pursuer.position,player);}
            if(path.Count>0)
            {
                var next=path[0];var delta=next-pursuer.position;
                if(delta.magnitude<.10f)path.RemoveAt(0);
                else{pursuer.position=Vector3.MoveTowards(pursuer.position,next,2.8f*Time.deltaTime);pursuer.rotation=Quaternion.LookRotation(delta.normalized);}
            }
            if(Vector3.Distance(player,pursuer.position)<.9f)
            {
                Active=false;pursuer.gameObject.SetActive(false);director.Game.sound.Stinger();
                director.Show("ПОГОНЯ","Он оказался ближе","Улики сохранены. Бегите из кабинета направо через коридор в кухню. Закройте дверь кухни за собой.",new[]{"ПОВТОРИТЬ"},new System.Action[]{Begin});
            }
        }
        int Cell(Vector3 p){int x=Mathf.Clamp(Mathf.RoundToInt((p.x+12)/Step),0,W-1),z=Mathf.Clamp(Mathf.RoundToInt(p.z/Step),0,H-1);return z*W+x;}
        Vector3 Point(int c)=>new Vector3((c%W)*Step-12,0,(c/W)*Step);
        bool Free(int c)
        {
            Vector3 p=Point(c);if(Physics.CheckCapsule(p+Vector3.up*.29f,p+Vector3.up*1.53f,.23f,~((1<<2)|(1<<8)),QueryTriggerInteraction.Ignore))return false;
            return Physics.Raycast(p+Vector3.up*.25f,Vector3.down,.5f,~(1<<2),QueryTriggerInteraction.Ignore);
        }
        void BuildPath(Vector3 from,Vector3 to)
        {
            int start=Cell(from),goal=Cell(to);var open=new List<int>{start};var previous=new Dictionary<int,int>();var cost=new Dictionary<int,float>{{start,0}};var closed=new HashSet<int>();
            var free=new Dictionary<int,bool>();int nearest=start;float nearestDistance=float.MaxValue;
            while(open.Count>0&&closed.Count<2400)
            {
                int current=open[0];float score=float.MaxValue;
                foreach(int c in open){float f=cost[c]+Vector3.Distance(Point(c),Point(goal));if(f<score){current=c;score=f;}}
                open.Remove(current);closed.Add(current);float distance=Vector3.Distance(Point(current),Point(goal));if(distance<nearestDistance){nearest=current;nearestDistance=distance;}if(current==goal)break;
                foreach(int shift in new[]{-1,1,-W,W})
                {
                    int n=current+shift;if(n<0||n>=W*H||closed.Contains(n)||(Mathf.Abs(shift)==1&&n/W!=current/W))continue;
                    if(!free.TryGetValue(n,out bool pass)){pass=Free(n);free[n]=pass;}if(!pass&&n!=goal)continue;
                    float g=cost[current]+Step;if(cost.TryGetValue(n,out float old)&&g>=old)continue;cost[n]=g;previous[n]=current;if(!open.Contains(n))open.Add(n);
                }
            }
            path.Clear();int cursor=nearest;int safety=0;while(cursor!=start&&previous.ContainsKey(cursor)&&safety++<2400){path.Add(Point(cursor));cursor=previous[cursor];}path.Reverse();
        }
    }
}
