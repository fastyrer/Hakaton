using UnityEngine;
namespace Hakaton.Demo.FatherHouse
{
    public sealed class HouseDocument : DemoInteractable
    {
        public string heading;
        [TextArea(4,12)]public string body;
        public override void Interact(DemoGame game)=>game.hud.ShowDocument(heading,body);
    }
}
