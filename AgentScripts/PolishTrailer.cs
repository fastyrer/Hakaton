using System.Linq;
using Hakaton.Demo.Trailer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PolishTrailer
{
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    public static string Main()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="OpeningTrailer")throw new System.Exception("Open trailer in Edit Mode");
        var d=Object.FindAnyObjectByType<OpeningTrailer>();var root=d.transform;
        foreach(var text in Object.FindObjectsByType<TextMesh>())
            if(!text.transform.IsChildOf(root)&&!text.text.Contains("COUNTY PRECINCT")&&text.text!="ПОЛИЦЕЙСКИЙ УЧАСТОК")text.GetComponent<Renderer>().enabled=false;
        var smoker=GameObject.Find("01 · Напарник · сигарета и ноги на столе").transform;
        foreach(Transform t in smoker)
        {
            if(t.name=="chair"){t.rotation=Quaternion.identity;PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
            if(t.name=="desk_lamp"){t.position=V(-8.25f,.88f,18.8f);PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
        }
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            if(t.name=="Жалюзи · полоса")t.localPosition-=Vector3.up*.07f;
            if(t.name.StartsWith("Агент прослушки "))t.position+=Vector3.right*.9f;
        }
        var father=d.actors.First(a=>a.name.StartsWith("ФЁДОР"));father.head=null;
        var s=d.shots[3];s.from=V(1.9f,1.75f,22.4f);s.to=V(1.9f,1.75f,27.1f);s.lookFrom=V(-.1f,1.1f,27);s.lookTo=V(-3.5f,1.1f,30);s.fieldOfView=55;
        s=d.shots[4];s.from=V(60.9f,1.78f,8.45f);s.to=V(62.4f,1.79f,9.1f);s.lookFrom=s.lookTo=V(62,1.78f,11.8f);
        s=d.shots[5];s.from=V(85.4f,1.95f,.5f);s.to=V(93.8f,1.95f,.9f);s.lookFrom=V(88,1.45f,5.2f);s.lookTo=V(92,1.45f,5.2f);s.fieldOfView=58;
        s=d.shots[12];s.from=V(-5.7f,1.65f,17.1f);s.to=V(-6.4f,1.65f,17.7f);
        EditorUtility.SetDirty(d);EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return "Saved polished framing";
    }
}
