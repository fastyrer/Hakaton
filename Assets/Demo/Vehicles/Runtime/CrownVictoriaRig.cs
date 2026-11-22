using UnityEngine;
namespace Hakaton.Demo.Vehicles
{
    public sealed class CrownVictoriaRig : MonoBehaviour
    {
        public const float BodyLength=5.3848f,BodyWidth=1.98628f,RoofHeight=1.44272f,Wheelbase=2.91338f;
        public Transform[] wheels;
        public Transform steering,wiperLeft,wiperRight,driverSocket;
        public Renderer[] rainWindows;
        public Renderer bodyShell;
    }
}
