using UnityEngine;
namespace Hakaton.Demo.FatherHouse
{
    public sealed class HouseAmbience : MonoBehaviour
    {
        public Light weakBulb;
        public Transform[] cameras=System.Array.Empty<Transform>();
        Quaternion[] initial;
        void Start(){initial=new Quaternion[cameras.Length];for(int i=0;i<cameras.Length;i++)initial[i]=cameras[i].localRotation;}
        void Update()
        {
            for(int i=0;i<cameras.Length;i++)cameras[i].localRotation=initial[i]*Quaternion.Euler(0,Mathf.Sin(Time.time*.15f+i)*9,0);
            if(weakBulb!=null)weakBulb.intensity=.7f+Mathf.Sin(Time.time*1.5f)*.08f;
        }
    }
}
