using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class PolishFatherHouse
{
    public static string Main()
    {
        if(Application.isPlaying)throw new Exception("Stop play");
        foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>())
        {
            if(t.name=="cabinet"&&Vector3.Distance(t.position,new Vector3(-3,0,14.2f))<.05f){t.SetPositionAndRotation(new Vector3(-7.1f,0,10.65f),Quaternion.Euler(0,180,0));PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
            if(t.name=="Электрический щит")t.position=new Vector3(7.82f,-3.4f,21.1f);
            if(t.name=="Загадка · basement_fuses")t.position=new Vector3(7.7f,-1.8f,21.1f);
            if(t.name=="Стеллаж с личным архивом"&&Vector3.Distance(t.position,new Vector3(-5.6f,3.4f,19.5f))<.05f)t.SetPositionAndRotation(new Vector3(-5.6f,3.4f,11.5f),Quaternion.Euler(0,180,0));
            if(t.name=="crate"&&t.parent.name.StartsWith("13 ·")&&t.position.x>-5){t.position+=Vector3.left*1.8f;PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
        }
        foreach(var s in UnityEngine.Object.FindObjectsByType<Hakaton.Demo.FatherHouse.HouseSecretDoor>())
            if(s.name=="Тайник · комната наблюдения"||s.name=="Тайник · шкаф в спальне")s.transform.rotation=Quaternion.Euler(0,-90,0);
        var hud=UnityEngine.Object.FindAnyObjectByType<Hakaton.Demo.DemoHud>();hud.locationCaption="НЬЮ-ДЖЕРСИ / ЛЕСНАЯ ОКРАИНА / 2000";PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
        var basement=UnityEngine.Object.FindObjectsByType<Transform>().First(t=>t.name.StartsWith("05 ·"));
        var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/FatherHouse/Art/Basement_concrete.mat");
        foreach(var r in basement.GetComponentsInChildren<Renderer>())if(r.name=="Стена"||r.name=="Над проёмом")r.sharedMaterial=concrete;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());return "Door landings cleared";
    }
}
