using UnityEngine;
namespace Hakaton.Demo.FatherHouse
{
    public sealed class HouseSecretDoor : DemoInteractable
    {
        public Transform panel;
        public Vector3 openOffset=new Vector3(1.95f,0,0);
        public string closedPrompt="Потянуть книгу с затёртым корешком";
        public bool isOpen;
        public float speed=1.35f;
        Vector3 origin;
        void Awake(){origin=panel.localPosition;}
        public override string Prompt=>isOpen?"Вернуть панель на место":closedPrompt;
        public override void Interact(DemoGame game){isOpen=!isOpen;game.sound.Creak(panel.position);}
        void Update(){panel.localPosition=Vector3.MoveTowards(panel.localPosition,origin+(isOpen?openOffset:Vector3.zero),speed*Time.deltaTime);}
    }
}
