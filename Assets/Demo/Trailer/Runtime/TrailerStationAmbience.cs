using UnityEngine;
namespace Hakaton.Demo.Trailer
{
    // Driven by the edit clock, so pause, seeking and offline export agree.
    public sealed class TrailerStationAmbience : MonoBehaviour
    {
        public Transform[] fans=System.Array.Empty<Transform>();
        public Quaternion[] fanRest=System.Array.Empty<Quaternion>();
        public Transform[] clockHands=System.Array.Empty<Transform>();
        public void Sample(float seconds)
        {
            for(int i=0;i<fans.Length;i++)if(fans[i]!=null)
                fans[i].localRotation=fanRest[i]*Quaternion.Euler(0,seconds*(37+i%3*5),0);
            foreach(var hand in clockHands)if(hand!=null)hand.localRotation=Quaternion.Euler(0,0,-seconds*6);
        }
    }
}
