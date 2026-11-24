using System;
using System.Linq;
using Hakaton.Demo.FatherHouse;
using Hakaton.LocationKit;
using Hakaton.LocationKit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Hakaton.Demo.Investigation.Editor
{
    public static class InvestigationSetup
    {
        const string Base="Assets/Demo/Investigation";
        static Transform root;static Material paper,metal,red,wood,cloth;static Scene source;static FigurineSolution solution;
        static T[] All<T>() where T:Component=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        static Material Mat(string name,Color c)
        {string path=Base+"/Resources/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=c};m.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(m,path);}return m;}
        [MenuItem("Hakaton/Расследование/Подключить полное ТЗ")]
        public static string Build()
        {
            if(Application.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Сохраните сцену и остановите Play перед подключением расследования.");
            solution=AssetDatabase.LoadAssetAtPath<FigurineSolution>(Base+"/Resources/FigurineSolution.asset");
            if(solution==null){solution=ScriptableObject.CreateInstance<FigurineSolution>();AssetDatabase.CreateAsset(solution,Base+"/Resources/FigurineSolution.asset");}
            paper=Mat("Case_paper",new Color(.77f,.70f,.52f));metal=Mat("Case_steel",new Color(.17f,.21f,.20f));red=Mat("Case_ink",new Color(.25f,.055f,.035f));wood=Mat("Figure_wood",new Color(.39f,.24f,.10f));cloth=Mat("Old_coat",new Color(.13f,.13f,.10f));
            source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/PoliceStation.unity");
            try
            {
                PatchFather();PatchStation();
                SmallLocation("IvanHome","Дом Ивана","Ivan");SmallLocation("DmitryHome","Дом Дмитрия","Dmitry");SmallLocation("AlexeyHome","Дом Алексея",null);SmallLocation("Church","Церковь","Priest");
                var list=EditorBuildSettings.scenes.ToList();foreach(string s in new[]{"IvanHome","DmitryHome","AlexeyHome","Church"})if(!list.Any(v=>v.path=="Assets/Scenes/"+s+".unity"))list.Add(new EditorBuildSettingsScene("Assets/Scenes/"+s+".unity",true));EditorBuildSettings.scenes=list.ToArray();
                AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/FatherHouse.unity");return "Расследование подключено: 6 локаций, 13 улик, 6 выводов, 36 сцен диалогов.";
            }
            finally{EditorSceneManager.ClosePreviewScene(source);}
        }
        static InvestigationDirector StartPatch(string scene)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");
            if(All<InvestigationDirector>().Length>0)throw new InvalidOperationException("Расследование уже подключено: "+scene+". Повторная сборка запрещена, чтобы сохранить ручные правки.");
            root=new GameObject("Расследование · предметы и сюжет").transform;
            var g=All<DemoGame>().Single();var d=g.gameObject.AddComponent<InvestigationDirector>();d.solution=solution;
            g.gameObject.AddComponent<CaseMemory>();g.locationObjective="";
            foreach(var old in All<DemoInteractable>().Where(v=>v.kind==DemoInteractable.Interaction.Evidence||v.kind==DemoInteractable.Interaction.Brother).ToArray())
            {if(old is DemoDoor)continue;UnityEngine.Object.DestroyImmediate(old);}
            return d;
        }
        static void Save(){LocationKitOperations.CurrentRoot?.Connect();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());}
        static void PatchFather()
        {
            var d=StartPatch("FatherHouse");
            var location=All<LocationRoot>().First();location.introduction="Отца не стало три года назад. Наконец дали отпуск. Пора разобрать его вещи.\n\nКлюч от входной двери у меня. С кабинетом наверняка сложнее.";
            location.game.arrivalScene=true;
            // Correct the old prototype's suicide note without discarding house dressing.
            foreach(var note in All<HouseDocument>())
            {
                if(note.heading=="Если я не вернусь")note.body="Красную книгу не переставлять. Ключ от кабинета в часах. Если оставляешь следы — значит, за тобой можно идти.";
                if(note.transform.position.y<0)note.body="Не выносить папки. Доступ к записям — только через механизм с четырьмя фигурками.";
                EditorUtility.SetDirty(note);
            }
            Evidence("E01",new Vector3(-5.8f,.982f,12.2f));Evidence("E02",new Vector3(-4.2f,.04f,15.6f));Evidence("E03",new Vector3(-5.2f,1.1f,13.0f));Evidence("E13",new Vector3(-7,.99f,6.7f));
            Evidence("E12",new Vector3(-10.3f,1.00f,11.4f));Evidence("E10",new Vector3(10.0f,-2.39f,26.35f));Evidence("E11",new Vector3(10.9f,-2.38f,26.35f));
            // Put the key interaction on the imported clock, preserving its authored geometry.
            AddClock();
            var study=All<DemoDoor>().First(t=>t.label=="Кабинет отца");
            foreach(var c in All<DemoDoor>().Where(v=>v.label=="Кабинет отца"||v.label=="Дверь в мастерскую").SelectMany(v=>v.GetComponentsInChildren<Collider>())){var i=Add(c.gameObject,CaseInteraction.ActionKind.StudyDoor,"study","Кабинет · бубновый замок");i.door=c.GetComponentInParent<DemoDoor>();}
            foreach(var secret in All<HouseSecretDoor>())
            {
                string id=secret.transform.position.y<0?"vault":secret.transform.position.y<1?"cctv":null;if(id==null)continue;
                foreach(var c in secret.GetComponentsInChildren<Collider>()){var i=Add(c.gameObject,CaseInteraction.ActionKind.Secret,id,id=="vault"?"Хранилище · механизм фигурок":"Потянуть красную книгу");i.secret=secret;}
            }
            var machine=Box("Механизм четырёх фигурок",new Vector3(6.95f,-2.45f,24.0f),new Vector3(.90f,.12f,.72f),wood,true);
            Add(machine,CaseInteraction.ActionKind.Mechanism,"mechanism","Расставить фигурки X, Y, Z, K");
            for(int i=0;i<4;i++)Box("Гнездо "+(i+1),new Vector3(6.65f+i*.2f,-2.375f,24),new Vector3(.15f,.025f,.38f),metal,false);
            var capsule=Cylinder("Запечатанная капсула",new Vector3(10.3f,-2.32f,25.8f),new Vector3(.11f,.10f,.11f),red);Add(capsule,CaseInteraction.ActionKind.Capsule,"capsule","Осмотреть капсулу");
            Npc("Pavel",new Vector3(-.8f,0,2),180);var phone=Kit("phone",new Vector3(-3.2f,.96f,12.5f));Add(phone,CaseInteraction.ActionKind.Phone,"phone","Позвонить");
            var chase=d.gameObject.AddComponent<CaseChase>();chase.kitchen=All<DemoDoor>().First(t=>t.label=="Кухня");chase.study=study;
            chase.pursuer=NpcModel("Незнакомец",new Vector3(-5,0,17),180);chase.pursuer.gameObject.SetActive(false);foreach(var c in chase.pursuer.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            SetupMemory(d.GetComponent<CaseMemory>());Save();
        }
        public static string RepairClock()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play");
            EditorSceneManager.OpenScene("Assets/Scenes/FatherHouse.unity");root=All<InvestigationDirector>().Single().gameObject.scene.GetRootGameObjects().First(o=>o.name.StartsWith("Расследование")).transform;
            foreach(var c in All<CaseInteraction>().Where(v=>v.action==CaseInteraction.ActionKind.Clock).ToArray())
            {if(c.gameObject.name=="cabinet")UnityEngine.Object.DestroyImmediate(c.gameObject);else UnityEngine.Object.DestroyImmediate(c);}
            AddClock();Save();return "Imported cuckoo clock connected";
        }
        static void AddClock()
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Demo/Art/cuckoo clock by Anonymous/model.obj");
            if(model==null)throw new InvalidOperationException("Imported cuckoo clock missing");
            var clock=(GameObject)PrefabUtility.InstantiatePrefab(model);clock.name="Часы с кукушкой · бубновый ключ";clock.transform.SetParent(root,true);clock.transform.position=Vector3.zero;
            var renderers=clock.GetComponentsInChildren<Renderer>();var bounds=renderers.Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});clock.transform.localScale*=1.0f/Mathf.Max(.01f,bounds.size.y);
            bounds=renderers.Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});clock.transform.position+=new Vector3(-6.5f,1.85f,9.68f)-bounds.center;
            bounds=renderers.Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            var collider=new GameObject("Крышка часов · ключ");collider.transform.SetParent(root);collider.transform.position=bounds.center;var box=collider.AddComponent<BoxCollider>();box.size=Vector3.Max(bounds.size,new Vector3(.3f,.3f,.15f));
            Add(collider,CaseInteraction.ActionKind.Clock,"clock","Открыть заднюю крышку часов");PrefabUtility.RecordPrefabInstancePropertyModifications(clock.transform);
        }
        static void PatchStation()
        {
            StartPatch("PoliceStation");
            foreach(var pair in new[]{("E04",new Vector3(7,1.00f,27)),("E05",new Vector3(10,1.00f,27.3f)),("E06",new Vector3(13,1.00f,27)),("E07",new Vector3(16.6f,1.00f,27.5f))})Evidence(pair.Item1,pair.Item2);
            var partner=All<StationNpc>().OrderBy(n=>Vector3.Distance(n.transform.position,new Vector3(-7,0,19))).First();BindNpc(partner.gameObject,"Partner","Поговорить с напарником");
            var archivist=All<StationNpc>().First(n=>n.role=="Архивариус");BindNpc(archivist.gameObject,"Archivist","Поговорить с архивариусом");ExtraStaging(All<CaseMemory>().Single());Save();
        }
        static void SmallLocation(string scene,string name,string person)
        {
            string path="Assets/Scenes/"+scene+".unity";if(System.IO.File.Exists(path))return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var location=LocationKitOperations.EnsureRoot();location.locationName=name;location.introduction=name+". Осмотрите вещи и поговорите с хозяином. Машина ждёт перед входом.";LocationKitOperations.AddPlayerRig();LocationKitOperations.SetSpawn(new Vector3(6,.08f,-3),0);
            LocationKitOperations.CreateRoom(new LocationKitOperations.RoomOptions{width=3,depth=3,doorSection=2,ceiling=true,roof=true});
            root=new GameObject("Расследование · предметы и сюжет").transform;
            var d=location.game.gameObject.AddComponent<InvestigationDirector>();d.solution=solution;
            location.game.arrivalScene=true;location.game.hud.worldVerticalResolution=360;
            LocationKitOperations.SetEnvironment(1);
            Box("Двор",new Vector3(6,-.13f,-6),new Vector3(24,.2f,12),wood,true);Kit("car",new Vector3(0,0,-6));
            Kit("rug",new Vector3(6,.03f,6));Kit("bookcase",new Vector3(1.2f,0,10.8f));Kit("cabinet",new Vector3(10.5f,0,9.8f));Kit("table",new Vector3(6,0,8));Kit("chair",new Vector3(6,0,9.2f),180);Kit("desk_lamp",new Vector3(6.5f,.9f,8));
            if(scene=="Church")
            {
                for(int row=0;row<3;row++){Kit("sofa",new Vector3(3,0,2.5f+row*1.7f));Kit("sofa",new Vector3(9,0,2.5f+row*1.7f));}
                Box("Крест",new Vector3(6,2.0f,11.6f),new Vector3(.13f,1.6f,.16f),wood,false);Box("Крест",new Vector3(6,2.25f,11.6f),new Vector3(.9f,.12f,.16f),wood,false);
            }
            else{Kit("sofa",new Vector3(2.5f,0,3.5f),90);Kit("crt",new Vector3(10.5f,.9f,9.8f),-45);Kit("fridge",new Vector3(10.7f,0,1.3f));}
            Kit("floor_lamp",new Vector3(2,0,8));Kit("warm_light",new Vector3(6,2.6f,6));Kit("warm_light",new Vector3(6,2.6f,10));
            if(person!=null)Npc(person,new Vector3(6,0,7),180);
            if(scene=="AlexeyHome")
            {
                var things=Box("Коробка со старыми вещами",new Vector3(2.8f,.25f,7.8f),new Vector3(.9f,.5f,.7f),wood,true);Add(things,CaseInteraction.ActionKind.OwnThings,"Z","Перебрать старые вещи · фигурка Z");
                var jacket=Box("Старая куртка",new Vector3(4.4f,.95f,8),new Vector3(.8f,.08f,.45f),cloth,false);Evidence("E08",new Vector3(4.4f,1.02f,8));Evidence("E09",new Vector3(6,.96f,8));
                for(int i=0;i<8;i++)Box("Разобранные бумаги",new Vector3(6.4f+(i%3)*.15f,.92f+i*.003f,8.1f),new Vector3(.28f,.003f,.22f),paper,false);
            }
            LocationKitOperations.RefreshConnections(location);EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),path);
        }
        static CaseInteraction Add(GameObject o,CaseInteraction.ActionKind action,string id,string label)
        {var c=o.AddComponent<CaseInteraction>();c.kind=DemoInteractable.Interaction.Custom;c.action=action;c.id=id;c.label=label;return c;}
        static GameObject Box(string name,Vector3 p,Vector3 size,Material m,bool collision)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(root,false);o.transform.position=p;o.transform.localScale=size;if(m!=null)o.GetComponent<Renderer>().sharedMaterial=m;if(!collision)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
        static GameObject Cylinder(string name,Vector3 p,Vector3 size,Material m)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=name;o.transform.SetParent(root,false);o.transform.position=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;return o;}
        static GameObject Kit(string id,Vector3 p,float yaw=0)
        {var o=(GameObject)PrefabUtility.InstantiatePrefab(KitLibraryBuilder.Load().Find(id).prefab);o.transform.SetParent(root,true);o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));PrefabUtility.RecordPrefabInstancePropertyModifications(o.transform);return o;}
        static void Evidence(string id,Vector3 p)
        {
            GameObject o;
            if(id=="E03")o=Cylinder(CaseCatalog.Title(id),p,new Vector3(.12f,.25f,.12f),metal);
            else if(id=="E02")
            {o=Box("Отпечаток подошвы",p,new Vector3(.24f,.02f,.45f),cloth,true);Box("Второй след",p+new Vector3(.35f,.001f,.55f),new Vector3(.24f,.02f,.45f),cloth,false);}
            else if(id=="E08")o=Cylinder("Гильза",p,new Vector3(.08f,.04f,.08f),wood);
            else o=Box(CaseCatalog.Title(id),p,new Vector3(.38f,.035f,.28f),paper,true);
            Add(o,CaseInteraction.ActionKind.Evidence,id,"Осмотреть · "+CaseCatalog.Title(id));
            if(id!="E02"&&id!="E03")Label(id+"\n"+CaseCatalog.Title(id),p+new Vector3(0,.022f,0),90,.0055f);
        }
        static void Label(string text,Vector3 p,float pitch=0,float size=.016f)
        {var o=new GameObject(text);o.transform.SetParent(root,false);o.transform.position=p;o.transform.rotation=Quaternion.Euler(pitch,180,0);var t=o.AddComponent<TextMesh>();t.text=text;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=36;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.09f,.08f,.055f);o.AddComponent<WorldTextLabel>().depthShader=Shader.Find("Hakaton/World Text");}
        static Transform NpcModel(string role,Vector3 p,float yaw)
        {
            var original=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StationNpc>()).First(n=>!n.seated&&n.role=="Архивариус");
            var o=UnityEngine.Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(o,SceneManager.GetActiveScene());o.name=role;o.transform.SetParent(root,true);o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));var n=o.GetComponent<StationNpc>();n.role=role;n.route=null;Dress(n);return o.transform;
        }
        static GameObject Npc(string id,Vector3 p,float yaw)
        {var o=NpcModel(id,p,yaw).gameObject;BindNpc(o,id,id=="Pavel"?"Поговорить с Павлом":id=="Ivan"?"Поговорить с Иваном":id=="Dmitry"?"Поговорить с Дмитрием":"Поговорить со священником");return o;}
        static void BindNpc(GameObject o,string id,string label)
        {
            foreach(var collider in o.GetComponentsInChildren<Collider>())Add(collider.gameObject,CaseInteraction.ActionKind.Person,id,label);
            if(o.GetComponentsInChildren<Collider>().Length==0){var box=o.AddComponent<BoxCollider>();box.center=Vector3.up*.9f;box.size=new Vector3(.65f,1.8f,.5f);Add(o,CaseInteraction.ActionKind.Person,id,label);}
        }
        static Transform View(string name,Vector3 position,Vector3 look)
        {var o=new GameObject(name);o.transform.SetParent(root,true);o.transform.position=position;o.transform.LookAt(look);return o.transform;}
        public static string UpgradeStaging()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play");
            EditorSceneManager.OpenScene("Assets/Scenes/FatherHouse.unity");root=All<InvestigationDirector>().Single().gameObject.scene.GetRootGameObjects().First(o=>o.name.StartsWith("Расследование")).transform;
            paper=AssetDatabase.LoadAssetAtPath<Material>(Base+"/Resources/Case_paper.mat");metal=AssetDatabase.LoadAssetAtPath<Material>(Base+"/Resources/Case_steel.mat");wood=AssetDatabase.LoadAssetAtPath<Material>(Base+"/Resources/Figure_wood.mat");cloth=AssetDatabase.LoadAssetAtPath<Material>(Base+"/Resources/Old_coat.mat");
            ExtraStaging(All<CaseMemory>().Single());Save();
            EditorSceneManager.OpenScene("Assets/Scenes/PoliceStation.unity");root=All<InvestigationDirector>().Single().gameObject.scene.GetRootGameObjects().First(o=>o.name.StartsWith("Расследование")).transform;ExtraStaging(All<CaseMemory>().Single());Save();EditorSceneManager.OpenScene("Assets/Scenes/FatherHouse.unity");return "Flashback weapons and television added";
        }
        public static string PolishActors()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play");
            foreach(string scene in new[]{"FatherHouse","IvanHome","DmitryHome","Church"})
            {
                EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");root=All<InvestigationDirector>().Single().gameObject.scene.GetRootGameObjects().First(o=>o.name.StartsWith("Расследование")).transform;
                foreach(var actor in All<StationNpc>().Where(n=>new[]{"Pavel","Ivan","Dmitry","Priest","Фёдор","Незнакомец"}.Contains(n.role)))Dress(actor);
                if(scene=="FatherHouse")
                {
                    foreach(var door in All<DemoDoor>().Where(v=>v.label=="Дверь в мастерскую"))foreach(var c in door.GetComponentsInChildren<Collider>())if(c.GetComponent<CaseInteraction>()==null){var i=Add(c.gameObject,CaseInteraction.ActionKind.StudyDoor,"study_secondary","Кабинет · бубновый замок");i.door=door;}
                    var memory=All<CaseMemory>().Single();if(memory.surveillanceView==null)memory.surveillanceView=View("Кадр · наблюдение",new Vector3(-9.2f,1.6f,13.2f),new Vector3(-11.8f,1.6f,11.5f));
                }
                Save();
            }
            EditorSceneManager.OpenScene("Assets/Scenes/FatherHouse.unity");return "Actors dressed, secondary study entrance guarded";
        }
        static void Dress(StationNpc actor)
        {
            string role=actor.role;Color color=role=="Ivan"?new Color(.24f,.29f,.24f):role=="Dmitry"?new Color(.35f,.12f,.075f):role=="Фёдор"?new Color(.35f,.29f,.19f):new Color(.055f,.06f,.055f);var material=Mat("Clothes_"+role,color);
            foreach(var r in actor.GetComponentsInChildren<Renderer>())if(r.name=="Туловище"||r.name=="Рукав")r.sharedMaterial=material;
            foreach(var t in actor.GetComponentsInChildren<Transform>().Where(t=>t.name=="Жетон").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            if(role=="Priest"&&!actor.GetComponentsInChildren<Transform>().Any(t=>t.name=="Сутана"))
            {
                var old=root;root=actor.transform;var coat=Box("Сутана",Vector3.zero,new Vector3(.52f,.9f,.31f),material,false);coat.transform.localPosition=new Vector3(0,.55f,0);
                var collar=Box("Белый воротник",Vector3.zero,new Vector3(.16f,.06f,.02f),Mat("Priest_collar",new Color(.8f,.77f,.66f)),false);collar.transform.localPosition=new Vector3(0,1.55f,.15f);root=old;
            }
        }
        static GameObject Gun(string name)
        {
            var o=new GameObject(name);o.transform.SetParent(root,false);var old=root;root=o.transform;
            Box("Затвор",new Vector3(0,.03f,0),new Vector3(.045f,.055f,.22f),metal,false);Box("Рукоять",new Vector3(0,-.055f,-.06f),new Vector3(.045f,.13f,.07f),cloth,false);Box("Ствол",new Vector3(0,.035f,.12f),new Vector3(.026f,.026f,.045f),metal,false);root=old;return o;
        }
        static void ExtraStaging(CaseMemory memory)
        {
            if(memory.father!=null&&memory.fatherGun==null)
            {
                memory.fatherGun=Gun("Пистолет Фёдора · .45");memory.fatherGun.transform.SetParent(memory.father.transform,false);memory.fatherGun.transform.localPosition=new Vector3(.3f,1.0f,.25f);memory.fatherGun.SetActive(false);
                memory.heroGun=Gun("Пистолет Алексея · 9 мм");memory.heroGun.SetActive(false);
            }
            if(memory.televisionRoom==null)
            {
                var old=root;var room=new GameObject("Финал · телевизор").transform;room.SetParent(root);root=room;
                Box("Пол",new Vector3(220,-.1f,0),new Vector3(10,.2f,10),wood,true);Box("Стена",new Vector3(220,1.5f,3),new Vector3(10,3,.2f),cloth,true);Kit("table",new Vector3(220,0,1));Kit("crt",new Vector3(220,.9f,1));Kit("sofa",new Vector3(220,0,-2));Kit("floor_lamp",new Vector3(222,0,-1));Kit("warm_light",new Vector3(220,2.6f,0));
                memory.televisionView=View("Кадр · репортаж",new Vector3(220,1.2f,-1.5f),new Vector3(220,1.35f,1));memory.televisionRoom=room.gameObject;room.gameObject.SetActive(false);root=old;
            }
        }
        static void SetupMemory(CaseMemory memory)
        {
            var old=root;var room=new GameObject("Флешбек · квартира Алексея").transform;room.SetParent(root);root=room;
            Box("Пол",new Vector3(200,-.1f,0),new Vector3(10,.2f,10),wood,true);Box("Стена",new Vector3(200,1.5f,3),new Vector3(10,3,.2f),cloth,true);Kit("table",new Vector3(200,0,0));Kit("chair",new Vector3(200,0,-1.2f));Kit("desk_lamp",new Vector3(200.5f,.9f,0));Box("Письмо от имени отца",new Vector3(200,.91f,-.05f),new Vector3(.3f,.008f,.22f),paper,false);Kit("warm_light",new Vector3(200,2.2f,-.4f));
            memory.letterView=View("Кадр · письмо",new Vector3(200,1.45f,-1.25f),new Vector3(200,.9f,0));memory.letterRoom=room.gameObject;root=old;
            memory.surveillanceView=View("Кадр · наблюдение",new Vector3(-9.2f,1.6f,13.2f),new Vector3(-11.8f,1.6f,11.5f));
            memory.murderView=View("Кадр · кабинет",new Vector3(-4.2f,1.65f,14.5f),new Vector3(-5.5f,1.4f,12.7f));memory.recognitionView=View("Кадр · настоящее",new Vector3(-4.3f,.65f,14.4f),new Vector3(-5.8f,.85f,12.7f));
            memory.father=NpcModel("Фёдор",new Vector3(-5.5f,0,12.7f),35).gameObject;memory.father.SetActive(false);room.gameObject.SetActive(false);ExtraStaging(memory);
        }
    }
}
