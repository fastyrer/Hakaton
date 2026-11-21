using UnityEngine;
namespace Hakaton.Demo.Trailer
{
    [System.Serializable]
    public sealed class TrailerShot
    {
        public string label, name, role;
        public float start, duration=6, fieldOfView=45;
        public Vector3 from, to, lookFrom, lookTo;
        public bool title, monochrome, exterior;
    }
}
