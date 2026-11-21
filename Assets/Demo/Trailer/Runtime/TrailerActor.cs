using UnityEngine;
namespace Hakaton.Demo.Trailer
{
    public sealed class TrailerActor : MonoBehaviour
    {
        public Transform head, leftArm, rightArm, leftLeg, rightLeg;
        public Transform[] reels;
        public bool seated, smoking, walking;
        public Vector3 walkFrom, walkTo;
        public Vector3[] path;
        public float walkStart = 20, walkDuration = 12, phase;
        public void Sample(float time)
        {
            float t=time+phase;
            if(walking)
            {
                float f=Mathf.Clamp01((time-walkStart)/walkDuration);
                Vector3 from=walkFrom,to=walkTo;
                if(path!=null&&path.Length>1)
                {
                    float length=0;for(int i=1;i<path.Length;i++)length+=Vector3.Distance(path[i-1],path[i]);
                    float distance=f*length;
                    for(int i=1;i<path.Length;i++)
                    {
                        float segment=Vector3.Distance(path[i-1],path[i]);from=path[i-1];to=path[i];
                        if(distance<=segment||i==path.Length-1){f=segment>.001f?distance/segment:0;break;}
                        distance-=segment;
                    }
                }
                transform.position=Vector3.Lerp(from,to,f);
                var d=to-from;if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(d);
            }
            if(head!=null)head.localRotation=Quaternion.Euler(smoking?4:0,Mathf.Sin(t*.44f)*6,0);
            float swing=walking?Mathf.Sin((time-walkStart)*6)*21:Mathf.Sin(t*.8f)*2;
            if(!smoking)
            {
                if(leftArm!=null)leftArm.localRotation=Quaternion.Euler(seated?-70:swing,0,0);
                if(rightArm!=null)rightArm.localRotation=Quaternion.Euler(seated?-70:-swing,0,0);
            }
            if(walking)
            {
                if(leftLeg!=null)leftLeg.localRotation=Quaternion.Euler(-swing,0,0);
                if(rightLeg!=null)rightLeg.localRotation=Quaternion.Euler(swing,0,0);
            }
            if(reels!=null)foreach(var reel in reels)if(reel!=null)reel.localRotation=Quaternion.Euler(0,0,time*95);
        }
    }
}
