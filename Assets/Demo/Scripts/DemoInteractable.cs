using UnityEngine;

namespace Hakaton.Demo
{
    public class DemoInteractable : MonoBehaviour
    {
        public enum Interaction { Evidence, Brother, FuseBox, FrontDoor, Car }
        public Interaction kind;
        public int evidenceId;
        public string label;
        public Texture2D illustration;
        public virtual string Prompt => label;
        public virtual void Interact(DemoGame game)
        {
            switch (kind)
            {
                case Interaction.Evidence: game.Inspect(this); break;
                case Interaction.Brother: game.Talk(); break;
                case Interaction.FuseBox: game.UseFuseBox(); break;
                case Interaction.FrontDoor: game.hud.Notify("Сначала нужно поговорить с Антоном и осмотреть вещи отца."); break;
                case Interaction.Car: game.OpenTravelMenu(); break;
            }
        }
    }
}
