using UnityEngine;
namespace Hakaton.Demo.Investigation
{
    public sealed class CaseMemory : MonoBehaviour
    {
        public Transform letterView,murderView,recognitionView,televisionView,surveillanceView;
        public GameObject father,letterRoom,televisionRoom,fatherGun,heroGun;
        Camera cam;Vector3 position;Quaternion rotation;float fov;int playing;
        float shot=-1;Quaternion fatherRotation;
        public void Begin(int n,DemoGame game)
        {
            if(n!=21&&n!=28&&n!=29&&n!=30&&n!=34&&n!=36)return;
            if(cam==null){cam=game.player.view;position=cam.transform.position;rotation=cam.transform.rotation;fov=cam.fieldOfView;}
            playing=n;shot=-1;var target=n==21?surveillanceView:n==28?letterView:n==29?murderView:n==30?recognitionView:televisionView;
            if(target!=null){cam.transform.SetPositionAndRotation(target.position,target.rotation);cam.fieldOfView=n==28?55:65;}
            if(father!=null){fatherRotation=father.transform.rotation;father.SetActive(n==29);}
            if(letterRoom!=null)letterRoom.SetActive(n==28);if(televisionRoom!=null)televisionRoom.SetActive(n==34||n==36);
            if(fatherGun!=null)fatherGun.SetActive(n==29);if(heroGun!=null)heroGun.SetActive(false);
        }
        public void Stage(string text,DemoGame game)
        {
            if(playing!=29)return;
            if(text.StartsWith("Алексей отбирает"))
            {
                if(fatherGun!=null){fatherGun.transform.SetParent(father.transform.parent,true);fatherGun.transform.position=new Vector3(-5.25f,.98f,12.4f);fatherGun.transform.rotation=Quaternion.Euler(0,75,90);}
                if(heroGun!=null){heroGun.transform.SetParent(game.player.view.transform,false);heroGun.transform.localPosition=new Vector3(.23f,-.21f,.45f);heroGun.transform.localRotation=Quaternion.Euler(0,180,0);heroGun.SetActive(true);}
            }
            if(text.StartsWith("Выстрел")){shot=0;game.sound.Stinger();}
        }
        void Update()
        {
            if(shot<0||cam==null||father==null)return;shot+=Time.unscaledDeltaTime;
            father.transform.rotation=Quaternion.Slerp(fatherRotation,Quaternion.Euler(0,35,83),Mathf.SmoothStep(0,1,shot/.8f));
            if(heroGun!=null)heroGun.transform.localPosition=new Vector3(.23f,-.21f,.45f)-Vector3.forward*(Mathf.Sin(Mathf.Clamp01(shot/.2f)*Mathf.PI)*.1f);
        }
        public void End(int n)
        {
            if(playing!=n||cam==null)return;
            cam.transform.SetPositionAndRotation(position,rotation);cam.fieldOfView=fov;cam=null;playing=0;shot=-1;
            if(father!=null){father.transform.rotation=fatherRotation;father.SetActive(false);}
            if(letterRoom!=null)letterRoom.SetActive(false);if(televisionRoom!=null)televisionRoom.SetActive(false);
            if(fatherGun!=null)fatherGun.SetActive(false);if(heroGun!=null)heroGun.SetActive(false);
        }
    }
}
