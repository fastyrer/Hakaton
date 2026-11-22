using System;
using System.Linq;
using System.Reflection;
using Hakaton.Demo.Station.Editor;
using Hakaton.LocationKit;
using Hakaton.LocationKit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class PolishStationStyle
{
    static Type builder=typeof(StationStyleBuilder);
    static BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
    static void Set(string name,object value)=>builder.GetField(name,flags).SetValue(null,value);
    static object Call(string name,params object[] args)=>builder.GetMethod(name,flags).Invoke(null,args);
    public static object Main()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().name!="PoliceStation")throw new Exception("Open station in Edit Mode");
        var location=UnityEngine.Object.FindAnyObjectByType<LocationRoot>();
        var root=GameObject.Find("07 · Обжитой участок · детали").transform;
        var group=new GameObject("08 · Доработки и копировальные аппараты").transform;group.SetParent(root,false);
        Set("location",location);Set("root",root);Set("group",group);Set("font",location.game.hud.font);Set("random",new System.Random(2042));Call("Materials");
        foreach(var table in location.GetComponentsInChildren<LocationPiece>().Where(p=>p.kitId=="table").ToArray())
        {
            Vector3 p=table.transform.position;
            bool work=(p.x>3&&(p.z==11||p.z==16||p.z==21))||(p.x<0&&p.z==11)||(p.x==-7&&(p.z==18||p.z==22));
            if(!work)continue;
            var drawer=(GameObject)Call("Kit","cabinet",p+new Vector3(.55f,0,0),0f);
            drawer.name="Тумба под рабочим столом";drawer.transform.localScale=new Vector3(.55f,.55f,.65f);PrefabUtility.RecordPrefabInstancePropertyModifications(drawer.transform);
        }
        Call("Copier",new Vector3(9.5f,0,13.5f));Call("Copier",new Vector3(14.5f,0,18.5f));
        Call("FixLabels");Call("WeatherCover",location.GetComponent<LocationEnvironment>());
        var board=GameObject.Find("Доска · ОПЕРАТИВНАЯ СВОДКА");board.transform.position=new Vector3(7,1.9f,23.82f);
        var remap=(System.Collections.Generic.Dictionary<string,Material>)builder.GetField("remap",flags).GetValue(null);
        foreach(var renderer in group.GetComponentsInChildren<Renderer>())
        {
            var material=renderer.sharedMaterial;
            if(material!=null&&remap.TryGetValue(material.name,out var next)){renderer.sharedMaterial=next;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
        }
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        return new {drawers=group.GetComponentsInChildren<LocationPiece>().Length,weatherMask=location.GetComponent<LocationEnvironment>().rainSystem.collision.collidesWith.value};
    }
}
