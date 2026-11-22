using System;
using System.Linq;
using Hakaton.Demo.CarIntro;
using Hakaton.Demo.Trailer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Hakaton.Demo.Vehicles.Editor
{
    public static class CrownVictoriaRefresh
    {
        [MenuItem("Hakaton/Интро в машине/Новая Crown Victoria по референсам")]
        public static string Apply()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Остановите Play и сохраните сцену.");
            if(SceneManager.GetActiveScene().name!="CarIntro")EditorSceneManager.OpenScene("Assets/Scenes/CarIntro.unity");
            var s=UnityEngine.Object.FindAnyObjectByType<CarIntroSequence>();var old=s.car;var parent=old.parent;
            var driver=old.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Алексей · водитель").gameObject;driver.transform.SetParent(null,true);
            try
            {
                var prefab=CrownVictoriaBuilder.BuildPrefab();Install(s,parent,driver,prefab);UnityEngine.Object.DestroyImmediate(old.gameObject);
                EditorUtility.SetDirty(s);EditorUtility.SetDirty(s.picture);EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();return "Crown Victoria 2000 installed; seven-shot Deception edit retained";
            }
            catch{EditorSceneManager.OpenScene("Assets/Scenes/CarIntro.unity");throw;}
        }
        public static void Install(CarIntroSequence s,Transform parent,GameObject driver,GameObject prefab=null)
        {
            if(prefab==null)prefab=AssetDatabase.LoadAssetAtPath<GameObject>(CrownVictoriaBuilder.PrefabPath)??CrownVictoriaBuilder.BuildPrefab();
            var car=(GameObject)PrefabUtility.InstantiatePrefab(prefab,SceneManager.GetActiveScene());PrefabUtility.UnpackPrefabInstance(car,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);car.name="Машина Алексея";car.transform.SetParent(parent,false);
            UnityEngine.Object.DestroyImmediate(car.GetComponent<BoxCollider>());UnityEngine.Object.DestroyImmediate(car.GetComponent<DemoInteractable>());
            var rig=car.GetComponent<CrownVictoriaRig>();driver.transform.SetParent(rig.driverSocket,false);driver.transform.localPosition=Vector3.zero;driver.transform.localRotation=Quaternion.identity;driver.transform.localScale=Vector3.one*.82f;driver.name="Алексей · водитель";
            var actor=driver.GetComponent<TrailerActor>();if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);
            foreach(var t in driver.GetComponentsInChildren<Transform>(true))
            {
                if(t.name=="Левая рука"||t.name=="Правая рука"){var p=t.localPosition;p.x=t.name=="Левая рука"?-.24f:.24f;t.localPosition=p;t.localRotation=Quaternion.Euler(-82,0,0);}
                if(t.name=="Левая нога"||t.name=="Правая нога")t.gameObject.SetActive(false);
            }
            var head=driver.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Голова");
            foreach(var renderer in driver.GetComponentsInChildren<Renderer>(true))if(renderer.transform.IsChildOf(head)||new[]{"Туловище","Рубашка","Галстук","Шея","Лацкан"}.Contains(renderer.name))renderer.gameObject.layer=8;
            s.car=car.transform;s.wiperLeft=rig.wiperLeft;s.wiperRight=rig.wiperRight;s.steering=rig.steering;s.wheels=rig.wheels;s.rainGlass=rig.rainWindows;
            if(s.cameraShots!=null&&s.picture.shots!=null)Reframe(s);
            car.transform.SetPositionAndRotation(CarIntroSequence.Road(0),CarIntroSequence.Heading(0));
        }
        public static void Reframe(CarIntroSequence s)
        {
            var a=s.cameraShots[0];a.from=new Vector3(.43f,1.22f,.05f);a.to=new Vector3(.43f,1.21f,-.08f);a.lookFrom=new Vector3(.38f,1.04f,-14);a.lookTo=new Vector3(.33f,1.04f,-16);a.fieldOfView=62;
            a=s.cameraShots[1];a.from=new Vector3(-4.2f,.90f,-7.2f);a.to=new Vector3(-3.5f,1.05f,-6.1f);a.lookFrom=a.lookTo=new Vector3(0,.78f,-.35f);a.fieldOfView=48;
            a=s.cameraShots[2];a.from=new Vector3(5.6f,1.15f,2.4f);a.to=new Vector3(5.2f,1.25f,-.65f);a.lookFrom=a.lookTo=new Vector3(0,.80f,-.1f);a.fieldOfView=50;
            a=s.cameraShots[3];a.from=new Vector3(-5,12,7);a.to=new Vector3(-1,16,9);a.lookFrom=a.lookTo=new Vector3(0,.65f,-1);a.fieldOfView=43;
            a=s.cameraShots[4];a.from=new Vector3(-1.25f,1.28f,-3.1f);a.to=new Vector3(-.95f,1.24f,-2.77f);a.lookFrom=a.lookTo=new Vector3(.43f,1.20f,-.15f);a.fieldOfView=35;
            for(int i=0;i<s.cameraShots.Length;i++)s.picture.shots[i].fieldOfView=s.cameraShots[i].fieldOfView;
        }
    }
}
