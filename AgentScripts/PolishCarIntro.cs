using System;
using System.Linq;
using Hakaton.Demo;
using Hakaton.Demo.CarIntro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class PolishCarIntro
{
    public static string Main()
    {
        var s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();
        if(s.car.GetComponent<Hakaton.Demo.Vehicles.CrownVictoriaRig>()!=null)return "Crown Victoria uses its authored geometry and cabin layout";
        foreach(var t in s.car.GetComponentsInChildren<TextMesh>(true))
        {
            if(t.text.StartsWith("MPH"))t.transform.localRotation=Quaternion.Euler(0,180,0);
            else if(t.text=="POLICE"||t.text.StartsWith("N.J."))t.transform.localRotation=Quaternion.Euler(0,t.transform.localPosition.x<0?90:-90,0);
            else t.transform.localRotation=Quaternion.identity;
            if(t.GetComponent<WorldTextLabel>()==null)t.gameObject.AddComponent<WorldTextLabel>().depthShader=Shader.Find("Hakaton/World Text");
        }
        s.car.Find("Приборы").localPosition=new Vector3(.48f,1.27f,-.73f);
        foreach(var mark in s.car.GetComponentsInChildren<Transform>().Where(t=>t.name=="Отметка спидометра")){var q=mark.localPosition;q.y=1.29f;q.z=-.715f;mark.localPosition=q;}
        var mph=s.car.GetComponentsInChildren<TextMesh>().First(t=>t.text.StartsWith("MPH"));mph.transform.localPosition=new Vector3(.48f,1.225f,-.712f);
        var driver=s.car.Find("Алексей · водитель");driver.localPosition=new Vector3(.48f,-.19f,0);
        var left=driver.Find("Левая рука");var right=driver.Find("Правая рука");var p=left.localPosition;p.x=-.24f;left.localPosition=p;p=right.localPosition;p.x=.24f;right.localPosition=p;
        foreach(var light in s.car.GetComponentsInChildren<Light>())light.intensity=light.name=="Подсветка приборов"?.045f:5;
        var roads=UnityEngine.Object.FindObjectsByType<Renderer>().Where(r=>r.name=="Мокрая колея").ToArray();var material=roads[0].sharedMaterial;if(Application.isPlaying)material=new Material(material);
        material.color=new Color(.055f,.07f,.075f);material.SetFloat("_Smoothness",.27f);material.SetFloat("_SpecularHighlights",0);material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");foreach(var r in roads)r.sharedMaterial=material;
        var a=s.cameraShots[4];a.from=new Vector3(-1.4f,1.6f,-3.2f);a.to=new Vector3(-1.05f,1.57f,-2.8f);a.lookFrom=a.lookTo=new Vector3(.48f,1.55f,0);a.fieldOfView=37;
        a=s.cameraShots[5];a.from=new Vector3(3,11,-37);a.to=new Vector3(1.5f,8,-29);
        if(!Application.isPlaying){EditorUtility.SetDirty(material);EditorUtility.SetDirty(s);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(s.gameObject.scene);}
        return "Dashboard lighting, depth-tested lettering, driver and approach framing polished";
    }
}
