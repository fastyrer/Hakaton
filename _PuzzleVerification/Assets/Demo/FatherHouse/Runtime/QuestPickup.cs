using UnityEngine;

namespace Hakaton.Demo.FatherHouse
{
    public sealed class QuestPickup : DemoInteractable
    {
        public string itemId;
        [TextArea] public string successMessage = "Предмет получен.";
        public override string Prompt => "Взять · " + label;

        public override void Interact(DemoGame game)
        {
            if (!game.GrantItem(itemId))
            {
                game.hud.Notify("Этот предмет уже у вас.");
                gameObject.SetActive(false);
                return;
            }
            game.sound.Creak(transform.position);
            game.hud.Notify(successMessage, 4);
            gameObject.SetActive(false);
        }
    }
}
