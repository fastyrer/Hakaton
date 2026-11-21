using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hakaton.Demo.Station;
using Hakaton.LocationKit;
using Hakaton.LocationKit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Hakaton.Demo.Trailer.Editor
{
    public static class TrailerStationRefresh
    {
        public const string BaselinePath="Assets/Demo/Trailer/Art/PreservedStationEdit.json";
        const string StationPath="Assets/Scenes/PoliceStation.unity";
        const string SetMarker="11 · Новый участок · оформление постановочных комнат";
        [Serializable]public sealed class Snapshot
        {
            public TrailerShot[] shots;public string music;public float duration,musicVolume,musicFadeIn,musicFadeOut;
            public string[] stagedActorNames;
        }
        sealed class NpcPose
        {
            public string name;public Vector3 position;public bool active,seated,smoking,walking;public float phase,walkStart,walkDuration;public Vector3 walkFrom,walkTo;public Vector3[] path;
        }
        static Material plaster,wood,trim,paper,vinyl,tile,steel,ivory;
        static Transform dress;static Scene source;static TrailerStationAmbience animation;
        [MenuItem("Hakaton/Трейлер/Обновить декорации из нынешнего участка")]
        public static string Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Остановите Play Mode.");
            if(SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Сохраните текущую сцену перед обновлением декораций.");
            if(SceneManager.GetActiveScene().name!="OpeningTrailer")EditorSceneManager.OpenScene(OpeningTrailerBuilder.ScenePath);
            var d=UnityEngine.Object.FindAnyObjectByType<OpeningTrailer>();
            var snapshot=new Snapshot{shots=d.shots,music=AssetDatabase.GetAssetPath(d.music.clip),duration=d.Duration,musicVolume=d.musicVolume,musicFadeIn=d.musicFadeIn,musicFadeOut=d.musicFadeOut,stagedActorNames=d.actors.Where(a=>a.transform.IsChildOf(d.transform)).Select(a=>a.name).ToArray()};
            string edit=JsonUtility.ToJson(snapshot,true);
            if(!File.Exists(BaselinePath)){File.WriteAllText(BaselinePath,edit);AssetDatabase.ImportAsset(BaselinePath);}
            var baseline=JsonUtility.FromJson<Snapshot>(File.ReadAllText(BaselinePath));
            if(JsonUtility.ToJson(new Snapshot{shots=d.shots})!=JsonUtility.ToJson(new Snapshot{shots=baseline.shots}))throw new InvalidOperationException("Монтаж отличается от сохранённого: обновление не будет менять его автоматически.");
            var old=UnityEngine.Object.FindAnyObjectByType<LocationRoot>();
            var poses=old.GetComponentsInChildren<TrailerActor>(true).Where(a=>a.GetComponent<StationNpc>()!=null).Select(a=>new NpcPose{name=a.name,position=a.transform.position,active=a.gameObject.activeSelf,seated=a.seated,smoking=a.smoking,walking=a.walking,phase=a.phase,walkStart=a.walkStart,walkDuration=a.walkDuration,walkFrom=a.walkFrom,walkTo=a.walkTo,path=a.path}).ToArray();
            var staged=d.actors.Where(a=>!a.transform.IsChildOf(old.transform)).ToArray();
            source=EditorSceneManager.OpenPreviewScene(StationPath);
            try
            {
                var original=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LocationRoot>(true)).Single();
                var replacement=UnityEngine.Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(replacement,SceneManager.GetActiveScene());replacement.name="ЛОКАЦИЯ · нынешний полицейский участок";
                var location=replacement.GetComponent<LocationRoot>();
                location.game.gameObject.SetActive(false);location.enabled=false;location.GetComponent<LocationEnvironment>().enabled=false;
                foreach(var weather in replacement.GetComponentsInChildren<DemoWeather>(true))weather.enabled=false;
                var actors=new List<TrailerActor>();
                foreach(var npc in replacement.GetComponentsInChildren<StationNpc>(true))
                {
                    npc.enabled=false;var a=npc.gameObject.AddComponent<TrailerActor>();a.head=npc.head;a.leftArm=npc.leftArm;a.rightArm=npc.rightArm;a.leftLeg=npc.leftLeg;a.rightLeg=npc.rightLeg;a.seated=npc.seated;a.phase=npc.phase;
                    var pose=poses.FirstOrDefault(p=>p.name==npc.name&&Vector3.Distance(p.position,npc.transform.position)<.1f);
                    if(pose!=null){a.seated=pose.seated;a.smoking=pose.smoking;a.walking=pose.walking;a.phase=pose.phase;a.walkStart=pose.walkStart;a.walkDuration=pose.walkDuration;a.walkFrom=pose.walkFrom;a.walkTo=pose.walkTo;a.path=pose.path;npc.gameObject.SetActive(pose.active);}
                    actors.Add(a);
                }
                foreach(var door in replacement.GetComponentsInChildren<DemoDoor>(true)){door.SetOpen(true);door.enabled=false;}
                // The original smoker occupies this workstation. Keep its actor, chair and free tabletop.
                foreach(var t in replacement.GetComponentsInChildren<Transform>(true))
                {
                    if(t.name=="NPC · Следователь"&&Vector3.Distance(t.position,new Vector3(-7,0,19))<1)t.gameObject.SetActive(false);
                    if(t.name=="chair"&&Vector3.Distance(t.position,new Vector3(-7,0,19))<1)t.gameObject.SetActive(false);
                    if((t.name=="CRT-монитор"||t.name=="Монохромный экран"||t.name=="Клавиатура")&&Mathf.Abs(t.position.x+7)<.6f&&Mathf.Abs(t.position.z-18)<.7f)t.gameObject.SetActive(false);
                }
                foreach(var label in replacement.GetComponentsInChildren<TextMesh>(true))
                    if(!label.text.Contains("COUNTY PRECINCT")&&label.text!="ПОЛИЦЕЙСКИЙ УЧАСТОК")label.GetComponent<Renderer>().enabled=false;
                var oldAnimation=d.stationAmbience;if(oldAnimation!=null)UnityEngine.Object.DestroyImmediate(oldAnimation);
                animation=d.gameObject.AddComponent<TrailerStationAmbience>();d.stationAmbience=animation;
                var ambient=replacement.GetComponentInChildren<StationAmbience>(true);ambient.enabled=false;
                animation.fans=ambient.fans;animation.clockHands=ambient.clockHands;
                d.actors=actors.Concat(staged).ToArray();
                UnityEngine.Object.DestroyImmediate(old.gameObject);
                Materials();DressSets(d);
                animation.fanRest=animation.fans.Select(f=>f.localRotation).ToArray();
                d.particles=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ParticleSystem>(true)).Where(p=>p.gameObject.activeInHierarchy).ToArray();
                d.smokeParticles=d.particles.Where(p=>!p.name.Contains("Rain")).ToArray();
                uint seed=41;foreach(var effect in d.particles){effect.useAutoRandomSeed=false;effect.randomSeed=seed++;var main=effect.main;main.playOnAwake=false;PrefabUtility.RecordPrefabInstancePropertyModifications(effect);}
                foreach(var t in replacement.GetComponentsInChildren<Transform>(true))
                {
                    if(!PrefabUtility.IsPartOfPrefabInstance(t))continue;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                    foreach(var component in t.GetComponents<Behaviour>())PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    var renderer=t.GetComponent<Renderer>();if(renderer!=null)PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                if(!staged.Select(a=>a.name).SequenceEqual(baseline.stagedActorNames))throw new InvalidOperationException("Изменилась постановка персонажей.");
                if(JsonUtility.ToJson(d.shots[0])!=JsonUtility.ToJson(baseline.shots[0])||JsonUtility.ToJson(new Snapshot{shots=d.shots})!=JsonUtility.ToJson(new Snapshot{shots=baseline.shots}))throw new InvalidOperationException("Изменился монтаж.");
                EditorUtility.SetDirty(d);EditorSceneManager.MarkSceneDirty(d.gameObject.scene);EditorSceneManager.SaveScene(d.gameObject.scene);AssetDatabase.SaveAssets();Selection.activeGameObject=d.gameObject;
                return "Обновлён участок; сохранены 30 пролётов, тайминг, музыка и 15 постановочных актёров.";
            }
            catch{EditorSceneManager.OpenScene(OpeningTrailerBuilder.ScenePath);throw;}
            finally{EditorSceneManager.ClosePreviewScene(source);}
        }
        static Material Load(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Station/Art/"+name+".mat");
        static void Materials(){plaster=Load("Warm_plaster");wood=Load("Wood_wainscot");trim=Load("Wood_trim");paper=Load("Case_paper");vinyl=Load("Worn_vinyl");tile=Load("Acoustic_ceiling");steel=Load("Filing_enamel");ivory=Load("Old_ivory");}
        static GameObject Box(string name,Vector3 p,Vector3 size,Material material)
        {var b=GameObject.CreatePrimitive(PrimitiveType.Cube);b.name=name;b.transform.SetParent(dress,false);b.transform.position=p;b.transform.localScale=size;b.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(b.GetComponent<Collider>());return b;}
        static GameObject Clone(string name,Vector3 p,float yaw=0,float scale=1)
        {
            var original=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).First(t=>t.name.Replace("(Clone)","").Trim()==name);
            var c=UnityEngine.Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(c,SceneManager.GetActiveScene());c.name=name;c.transform.SetParent(dress,false);c.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));c.transform.localScale=Vector3.one*scale;return c;
        }
        static void Stack(Vector3 p,int count=5)
        {for(int i=0;i<count;i++){var b=Box("Материалы дела",p+Vector3.up*(i*.022f),new Vector3(.34f,.018f,.26f),i%3==0?wood:paper);b.transform.Rotate(0,i*7-12,0);}}
        static void Cabinet(Vector3 p,float yaw=0)
        {
            var c=(GameObject)PrefabUtility.InstantiatePrefab(KitLibraryBuilder.Load().Find("cabinet").prefab);c.name="Архивный шкаф";c.transform.SetParent(dress,false);c.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
            foreach(var r in c.GetComponentsInChildren<Renderer>())r.sharedMaterial=steel;PrefabUtility.RecordPrefabInstancePropertyModifications(c.transform);foreach(var r in c.GetComponentsInChildren<Renderer>())PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
        static void DressSets(OpeningTrailer d)
        {
            var previous=d.transform.Find(SetMarker);if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            dress=new GameObject(SetMarker).transform;dress.SetParent(d.transform,false);
            foreach(var room in d.transform.Cast<Transform>().Where(t=>t.name.StartsWith("03 ·")||t.name.StartsWith("04 ·")||t.name.StartsWith("05 ·")||t.name.StartsWith("06 ·")||t.name.StartsWith("07 ·")||t.name.StartsWith("08 ·")||t.name.StartsWith("09 ·")))
            {
                var floor=room.Find("Пол");if(floor==null)continue;
                float width=floor.localScale.x,depth=floor.localScale.z;var origin=room.position;
                foreach(var r in room.GetComponentsInChildren<Renderer>(true))
                {
                    switch(r.name){case "Пол":r.sharedMaterial=vinyl;break;case "Задняя стена":case "Боковая стена":r.sharedMaterial=plaster;break;case "Потолок":r.sharedMaterial=tile;break;case "Письменный стол":case "Рабочий стол":case "Стол с делом":r.sharedMaterial=wood;break;}
                }
                Box("Деревянная панель · задняя стена",origin+new Vector3(0,.58f,depth-.13f),new Vector3(width-.2f,1.14f,.07f),wood);
                Box("Молдинг · задняя стена",origin+new Vector3(0,1.2f,depth-.17f),new Vector3(width-.2f,.09f,.12f),trim);
                foreach(float side in new[]{-1f,1f})
                {
                    Box("Деревянная панель · боковая стена",origin+new Vector3(side*(width/2-.13f),.58f,depth/2),new Vector3(.07f,1.14f,depth-.1f),wood);
                    Box("Молдинг · боковая стена",origin+new Vector3(side*(width/2-.17f),1.2f,depth/2),new Vector3(.12f,.09f,depth-.1f),trim);
                }
            }
            // Existing actors, blinds, desks and reels retain their exact positions.
            Cabinet(new Vector3(64.6f,0,14.8f),-90);Stack(new Vector3(64.6f,1.37f,14.8f),6);Clone("Настольная лампа",new Vector3(62.6f,.87f,14),180,.65f);Stack(new Vector3(61.5f,.87f,13.8f));
            for(int i=0;i<3;i++)
            {
                float x=86.3f+i*3.7f;Stack(new Vector3(x-1,.88f,5.1f),4+i);Clone("Кружка POLICE",new Vector3(x+1.12f,.88f,5.1f),i*30,.7f);
                Clone("Настольная лампа",new Vector3(x+1.05f,.88f,4.8f),-15,.6f);
                Box("Коробки с магнитными лентами",new Vector3(x,1.38f,8.9f),new Vector3(.85f,.18f,.55f),paper);
            }
            Cabinet(new Vector3(85,0,8.8f),0);Cabinet(new Vector3(95,0,8.8f),0);
            var fan=Clone("Потолочный вентилятор",new Vector3(90,2.62f,6.8f));animation.fans=animation.fans.Concat(new[]{fan.transform}).ToArray();
            var clock=Clone("Настенные часы",new Vector3(94,2.4f,9.78f));animation.clockHands=animation.clockHands.Concat(new[]{clock.transform.Find("Секундная стрелка")}).ToArray();
            Stack(new Vector3(119.5f,.83f,1.75f),5);Cabinet(new Vector3(122.3f,0,5.2f),0);Stack(new Vector3(122.3f,1.37f,5.2f),7);
            Cabinet(new Vector3(162.5f,0,5.2f),0);Box("Архивная коробка у стены",new Vector3(162.5f,1.5f,5.2f),new Vector3(.55f,.25f,.4f),paper);
        }
    }
}
