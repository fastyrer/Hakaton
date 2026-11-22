using UnityEngine;
namespace Hakaton.Demo.Station
{
    public sealed class StationAmbience : MonoBehaviour
    {
        public Transform[] fans=System.Array.Empty<Transform>();
        public Transform[] clockHands=System.Array.Empty<Transform>();
        void Update()
        {
            for(int i=0;i<fans.Length;i++)if(fans[i]!=null)fans[i].Rotate(0,(37+i%3*5)*Time.deltaTime,0,Space.Self);
            foreach(var hand in clockHands)if(hand!=null)hand.localRotation=Quaternion.Euler(0,0,-Time.time*6);
        }
    }
}
