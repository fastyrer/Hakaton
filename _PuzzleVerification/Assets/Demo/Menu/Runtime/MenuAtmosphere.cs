using UnityEngine;
namespace Hakaton.Demo.Menu
{
    public sealed class MenuAtmosphere : MonoBehaviour
    {
        public Transform fan, cameraRig, secondHand;
        public float fanSpeed=48;
        Quaternion cameraRotation;
        Vector3 cameraPosition;
        AudioClip tone;
        void Start()
        {
            cameraPosition=cameraRig.position;cameraRotation=cameraRig.rotation;
            const int rate=22050;float[] data=new float[rate*5];var random=new System.Random(217);float low=0;
            for(int i=0;i<data.Length;i++){low=Mathf.Lerp(low,(float)random.NextDouble()*2-1,.015f);data[i]=low*.07f+Mathf.Sin(i*2*Mathf.PI*55/rate)*.006f;}
            tone=AudioClip.Create("Вентиляция кабинета",data.Length,1,rate,false);tone.SetData(data,0);
            var source=gameObject.AddComponent<AudioSource>();source.clip=tone;source.loop=true;source.spatialBlend=0;source.volume=.35f;source.Play();
        }
        void Update()
        {
            fan.Rotate(0,fanSpeed*Time.unscaledDeltaTime,0,Space.Self);
            float t=Time.unscaledTime;
            cameraRig.position=cameraPosition+new Vector3(Mathf.Sin(t*.13f)*.012f,Mathf.Sin(t*.17f)*.008f,0);
            cameraRig.rotation=cameraRotation*Quaternion.Euler(Mathf.Sin(t*.11f)*.06f,Mathf.Sin(t*.09f)*.12f,0);
            if(secondHand!=null)secondHand.localRotation=Quaternion.Euler(0,0,-t*6);
        }
        void OnDestroy(){if(tone!=null)Destroy(tone);}
    }
}
