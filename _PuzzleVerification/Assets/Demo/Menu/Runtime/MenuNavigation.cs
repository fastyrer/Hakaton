using UnityEngine;
namespace Hakaton.Demo.Menu
{
    public static class MenuNavigation
    {
        static string trailerDestination;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()=>trailerDestination=null;
        public static void SetTrailerDestination(string scene)=>trailerDestination=scene;
        public static string ConsumeTrailerDestination(){string scene=trailerDestination;trailerDestination=null;return scene;}
    }
}
