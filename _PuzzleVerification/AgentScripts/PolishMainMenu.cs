using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Hakaton.Demo.Menu;
public static class PolishMainMenu
{
    public static string Main()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Stop Play first");
        var table=GameObject.Find("Стол детектива · модуль конструктора").transform;table.localScale=new Vector3(2.6f,1,3.4f);PrefabUtility.RecordPrefabInstancePropertyModifications(table);
        var gun=GameObject.Find("Пистолет · наша модель из архива").transform;gun.position=new Vector3(-.4f,.98f,-.55f);
        var old=gun.Find("Спусковая скоба");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var black=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Menu/Art/Ink_and_rubber.mat");var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Menu/Art/Office_steel.mat");
        Box(gun,"Скоба · перед",new Vector3(-.02f,-.025f,-.085f),new Vector3(.014f,.025f,.10f),black);
        Box(gun,"Скоба · низ",new Vector3(.035f,-.025f,-.13f),new Vector3(.12f,.025f,.014f),black);
        Box(gun,"Спусковой крючок",new Vector3(.025f,-.022f,-.07f),new Vector3(.014f,.025f,.065f),steel);
        for(int i=0;i<5;i++)Box(gun,"Насечка затвора",new Vector3(.08f+i*.015f,.045f,-.002f),new Vector3(.004f,.005f,.058f),steel);
        for(int i=0;i<2;i++)GameObject.Find("Сотрудник в коридоре "+(i+1)).transform.position=new Vector3(4.3f+i*.15f,0,7.4f+i*2.7f);
        var menu=Object.FindAnyObjectByType<MainMenu>();
        foreach(var slider in new[]{menu.volume,menu.mouse})
        {
            if(slider.GetComponent<UnityEngine.UI.Image>()==null)slider.gameObject.AddComponent<UnityEngine.UI.Image>().color=Color.clear;
            slider.fillRect.sizeDelta=Vector2.zero;slider.handleRect.pivot=new Vector2(.5f,.5f);slider.handleRect.anchoredPosition=Vector2.zero;slider.handleRect.sizeDelta=new Vector2(8,-12);
        }
        menu.settingsPanel.gameObject.SetActive(false);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());return "Saved foreground composition";
    }
    static void Box(Transform parent,string name,Vector3 p,Vector3 scale,Material material)
    {
        if(parent.Find(name)!=null)return;
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(o.GetComponent<Collider>());
    }
}
