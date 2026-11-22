using System;
using System.IO;
using System.Linq;
using Hakaton.LocationKit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Hakaton.Demo.Menu.Editor
{
    public static class MainMenuBuilder
    {
        public const string ScenePath="Assets/Scenes/MainMenu.unity";
        const string Art="Assets/Demo/Menu/Art";
        static Transform root,group;
        static Scene source;
        static Material wood,wall,trim,ivory,black,brass,steel,paper,red,blue,glow,glass;
        static Font font;
        static Mesh cylinder;
        static MenuAtmosphere atmosphere;
        static MainMenu menu;
        [MenuItem("Hakaton/Главное меню/Создать или открыть")]
        public static void Build()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Остановите Play и сохраните текущую сцену.");
            if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return;}
            if(KitLibraryBuilder.Load()==null)KitLibraryBuilder.Build();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/PoliceStation.unity");
            try
            {
                font=Objects<DemoGame>().First().hud.font??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                root=new GameObject("ГЛАВНОЕ МЕНЮ · кабинет детектива").transform;
                atmosphere=root.gameObject.AddComponent<MenuAtmosphere>();menu=root.gameObject.AddComponent<MainMenu>();
                Materials();Room();Windows();Furniture();Desk();BackOffice();FanAndClock();Hallway();Flag();Lighting();Interface();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);
                var list=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();list.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=list.ToArray();
                AssetDatabase.SaveAssets();Selection.activeGameObject=root.gameObject;
                SceneView.lastActiveSceneView?.LookAt(new Vector3(0,1.5f,1),Quaternion.Euler(7,-10,0),5);
            }
            finally{EditorSceneManager.ClosePreviewScene(source);}
        }
        static T[] Objects<T>()where T:Component=>source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent??root,false);return t;}
        static GameObject Box(string name,Vector3 p,Vector3 size,Material m,Transform parent=null)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent??group,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;
        }
        static GameObject Cylinder(string name,Vector3 p,Vector3 size,Material mat,Transform parent=null)
        {
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent??group,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<MeshFilter>().sharedMesh=cylinder;o.GetComponent<Renderer>().sharedMaterial=mat;return o;
        }
        static Material Mat(string name,Color c,string copy=null)
        {
            string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            var template=copy==null?null:AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Art/"+copy+".mat");
            m=template!=null?new Material(template):new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.color=c;m.SetFloat("_Smoothness",.24f);AssetDatabase.CreateAsset(m,path);return m;
        }
        static void Materials()
        {
            wood=Mat("Desk_walnut",new Color(.43f,.29f,.16f),"Generated/Dark_walnut");wood.mainTextureScale=new Vector2(2,2);
            wall=Mat("Office_plaster",new Color(.55f,.49f,.38f),"Generated/Faded_plaster");wall.mainTextureScale=new Vector2(2,2);
            trim=Mat("Wood_trim",new Color(.16f,.105f,.055f),"Generated/Dark_walnut");ivory=Mat("Aged_ivory",new Color(.78f,.72f,.55f));
            black=Mat("Ink_and_rubber",new Color(.035f,.033f,.028f));brass=Mat("Old_brass",new Color(.42f,.29f,.11f));steel=Mat("Office_steel",new Color(.29f,.32f,.25f));
            paper=Mat("Manila_paper",new Color(.78f,.70f,.49f));red=Mat("Flag_red",new Color(.43f,.08f,.06f));blue=Mat("Flag_blue",new Color(.055f,.095f,.15f));
            glow=Mat("Warm_fixture",new Color(.95f,.88f,.65f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.75f,.4f)*1.6f);
            glass=Mat("Ashtray_glass",new Color(.55f,.6f,.5f,.4f));glass.SetFloat("_Surface",1);glass.SetFloat("_SrcBlend",5);glass.SetFloat("_DstBlend",10);glass.SetFloat("_ZWrite",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
            cylinder=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Demo/Art/Generated/Cylinder8.asset");
        }
        static GameObject Piece(string id,Vector3 p,float yaw=0)
        {
            var prefab=KitLibraryBuilder.Load().Find(id).prefab;var o=(GameObject)PrefabUtility.InstantiatePrefab(prefab);o.transform.SetParent(group,false);o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));PrefabUtility.RecordPrefabInstancePropertyModifications(o.transform);return o;
        }
        static void Text(string text,Vector3 p,float size=.03f,Transform parent=null,Color? color=null)
        {
            var t=Group(text,parent??group);t.localPosition=p;var label=t.gameObject.AddComponent<TextMesh>();label.font=font;label.text=text;label.fontSize=48;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=color??new Color(.74f,.65f,.41f);
            t.gameObject.AddComponent<WorldTextLabel>().depthShader=Shader.Find("Hakaton/World Text");
        }
        static void Point(Vector3 p,Color c,float power,float range,Transform parent=null)
        {
            var l=Group("Локальный свет",parent??group).gameObject.AddComponent<Light>();l.transform.localPosition=p;l.type=LightType.Point;l.color=c;l.intensity=power;l.range=range;l.shadows=LightShadows.None;
        }
        static void Room()
        {
            group=Group("01 · Кабинет");
            Box("Пол",new Vector3(1,-.12f,2),new Vector3(8,.2f,10),wood);
            Box("Стена слева · под окном",new Vector3(-3,.46f,1.5f),new Vector3(.2f,.92f,7),wall);
            Box("Стена слева · над окном",new Vector3(-3,3.02f,1.5f),new Vector3(.2f,.5f,7),wall);
            Box("Правая стена",new Vector3(5,1.6f,2),new Vector3(.2f,3.2f,10),wall);
            Box("Задняя стена",new Vector3(-1,1.6f,5),new Vector3(4,3.2f,.2f),wall);
            Box("Панель под доской",new Vector3(-1,.44f,4.86f),new Vector3(4,.88f,.12f),trim);
            Box("Потолок",new Vector3(1,3.25f,2),new Vector3(8,.16f,10),wall);
            for(int i=0;i<9;i++)Box("Шов потолочной плитки",new Vector3(-3+i,3.155f,2),new Vector3(.02f,.018f,10),steel);
            for(int i=-2;i<8;i++)Box("Шов потолочной плитки",new Vector3(1,3.155f,i),new Vector3(8,.018f,.02f),steel);
            foreach(float x in new[]{-1.1f,3.3f})
            {
                Box("Светильник · корпус",new Vector3(x,3.07f,2.5f),new Vector3(.8f,.11f,1.7f),steel);
                for(int i=0;i<3;i++)Box("Люминесцентная трубка",new Vector3(x-.22f+i*.22f,3,2.5f),new Vector3(.12f,.035f,1.4f),glow);
                Point(new Vector3(x,2.8f,2.5f),new Color(1,.88f,.62f),1,5);
            }
        }
        static void Windows()
        {
            group=Group("02 · Окна и настоящие жалюзи");
            for(int pane=0;pane<3;pane++)
            {
                float z=-.6f+pane*1.8f;
                foreach(float dz in new[]{-.85f,.85f})Box("Деревянная рама",new Vector3(-2.85f,1.86f,z+dz),new Vector3(.16f,1.98f,.1f),trim);
                foreach(float y in new[]{.91f,2.8f})Box("Рама и подоконник",new Vector3(-2.82f,y,z),new Vector3(.35f,.11f,1.8f),wood);
                for(int i=0;i<17;i++)
                {
                    var slat=Box("Ламель жалюзи",new Vector3(-2.76f,1.02f+i*.102f,z),new Vector3(.13f,.044f,1.6f),trim);
                    slat.transform.localRotation=Quaternion.Euler(0,0,-18);
                }
                foreach(float dz in new[]{-.55f,.55f})Box("Шнур жалюзи",new Vector3(-2.65f,1.88f,z+dz),new Vector3(.012f,1.8f,.012f),paper);
            }
            // The city outside is geometry; no reference image or prerendered backdrop is used.
            var building=Mat("City_stone",new Color(.39f,.33f,.27f));var random=new System.Random(410);
            for(int i=0;i<14;i++)
            {
                float height=2+(float)random.NextDouble()*5;float z=-9+i*1.35f;
                Box("Силуэт городского дома",new Vector3(-8-(i%3)*1.3f,height/2-1,z),new Vector3(1.2f,height,1.1f),building);
                for(float y=.4f;y<height-1;y+=.7f)Box("Окна города",new Vector3(-7.37f-(i%3)*1.3f,y,z),new Vector3(.015f,.25f,.55f),ivory);
            }
        }
        static void Furniture()
        {
            group=Group("03 · Наши столы и картотека");
            var desk=Piece("table",new Vector3(0,0,.25f));desk.name="Стол детектива · модуль конструктора";desk.transform.localScale=new Vector3(2.6f,1,3.4f);PrefabUtility.RecordPrefabInstancePropertyModifications(desk.transform);
            foreach(var r in desk.GetComponentsInChildren<Renderer>()){r.sharedMaterial=wood;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            var chair=Piece("chair",new Vector3(-.7f,0,1.8f));chair.name="Кресло за столом";chair.transform.localScale=new Vector3(1.5f,1.2f,1.4f);PrefabUtility.RecordPrefabInstancePropertyModifications(chair.transform);
            foreach(var r in chair.GetComponentsInChildren<Renderer>()){r.sharedMaterial=trim;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            for(int i=0;i<3;i++)
            {
                var cabinet=Piece("cabinet",new Vector3(-2.25f+i*.94f,0,4.37f));cabinet.transform.localScale=new Vector3(.85f,1.17f,1);PrefabUtility.RecordPrefabInstancePropertyModifications(cabinet.transform);
                for(int j=0;j<6;j++)
                {var folder=Box("Папка картотеки",new Vector3(-2.55f+i*.94f+j*.11f,1.81f,4.35f),new Vector3(.08f,.42f,.48f),j%2==0?paper:wood);folder.transform.localRotation=Quaternion.Euler(0,0,(j-2)*3);}
            }
            Piece("table",new Vector3(1,0,4.2f));
            Piece("phone",new Vector3(1.6f,.88f,4.2f));
        }
        static void Desk()
        {
            group=Group("04 · Бумаги, оружие и пепельница");
            var random=new System.Random(91);
            for(int i=0;i<16;i++)
            {
                var sheet=Box("Материалы дела",new Vector3(.12f+(float)random.NextDouble()*.10f,.9f+i*.009f,.3f),new Vector3(1.15f,.009f,.76f),i%4==0?wood:paper);
                sheet.transform.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*16-8,0);
            }
            Box("Скоба папки",new Vector3(.14f,1.07f,-.03f),new Vector3(.08f,.05f,.08f),black);
            var printed=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Demo/Art/Station/Case_document_3.asset");
            var page=Mat("Top_case_page",new Color(.92f,.88f,.73f));page.mainTexture=printed;
            var top=GameObject.CreatePrimitive(PrimitiveType.Quad);top.name="Протокол · существующая текстура дела";top.transform.SetParent(group,false);top.transform.localPosition=new Vector3(.1f,1.047f,.32f);top.transform.localRotation=Quaternion.Euler(90,0,0);top.transform.localScale=new Vector3(.8f,.65f,1);top.GetComponent<Renderer>().sharedMaterial=page;UnityEngine.Object.DestroyImmediate(top.GetComponent<Collider>());
            var gun=Group("Пистолет · наша модель из архива",group);gun.position=new Vector3(-.4f,.98f,-.55f);gun.rotation=Quaternion.Euler(0,-28,0);gun.localScale=Vector3.one*1.8f;
            foreach(var t in Objects<Transform>().Where(t=>t.name=="Пистолет · затвор"||t.name=="Пистолет · рукоять"))
            {
                var copy=UnityEngine.Object.Instantiate(t.gameObject);SceneManager.MoveGameObjectToScene(copy,SceneManager.GetActiveScene());copy.transform.SetParent(gun,false);copy.transform.localPosition=t.position-new Vector3(10,1.005f,27.45f);copy.transform.localRotation=t.rotation;copy.transform.localScale=t.lossyScale;
            }
            Box("Скоба · перед",new Vector3(-.02f,-.025f,-.085f),new Vector3(.014f,.025f,.10f),black,gun);
            Box("Скоба · низ",new Vector3(.035f,-.025f,-.13f),new Vector3(.12f,.025f,.014f),black,gun);
            Box("Спусковой крючок",new Vector3(.025f,-.022f,-.07f),new Vector3(.014f,.025f,.065f),steel,gun);
            for(int i=0;i<5;i++)Box("Насечка затвора",new Vector3(.08f+i*.015f,.045f,-.002f),new Vector3(.004f,.005f,.058f),steel,gun);
            Cylinder("Ствол",new Vector3(-.17f,0,0),new Vector3(.045f,.1f,.045f),steel,gun).transform.localRotation=Quaternion.Euler(0,0,90);
            Lamp();Ashtray();Mug();
            Cylinder("Стакан для карандашей",new Vector3(-1.85f,1.01f,.45f),new Vector3(.18f,.24f,.18f),black);
            for(int i=0;i<5;i++)
            {var pencil=Box("Карандаш",new Vector3(-1.9f+i*.023f,1.22f,.45f+(i%2)*.04f),new Vector3(.016f,.35f,.016f),paper);pencil.transform.Rotate(0,0,(i-2)*7);}
        }
        static Mesh Ring()
        {
            string path=Art+"/AshtrayRing.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing!=null)return existing;
            var vertices=new Vector3[64];var triangles=new System.Collections.Generic.List<int>();
            for(int i=0;i<16;i++)
            {
                float angle=i*Mathf.PI*2/16;Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                vertices[i]=direction*.31f;vertices[16+i]=direction*.31f+Vector3.up*.085f;vertices[32+i]=direction*.25f+Vector3.up*.085f;vertices[48+i]=direction*.25f+Vector3.up*.02f;
            }
            for(int ring=0;ring<3;ring++)for(int i=0;i<16;i++)
            {int a=ring*16+i,b=ring*16+(i+1)%16,c=(ring+1)*16+i,d=(ring+1)*16+(i+1)%16;triangles.AddRange(new[]{a,c,b,b,c,d});}
            var m=new Mesh{name="Low-poly glass ashtray"};m.vertices=vertices;m.triangles=triangles.ToArray();m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,path);return m;
        }
        static void Ashtray()
        {
            var t=Group("Пепельница с тлеющей сигарой",group);t.localPosition=new Vector3(1.25f,.89f,-.2f);
            var ring=Group("Стеклянный обод",t).gameObject;ring.AddComponent<MeshFilter>().sharedMesh=Ring();ring.AddComponent<MeshRenderer>().sharedMaterial=glass;
            Cylinder("Стеклянное дно",new Vector3(0,.015f,0),new Vector3(.59f,.03f,.59f),glass,t);
            for(int i=0;i<10;i++)Box("Пепел",new Vector3(Mathf.Sin(i*8)*.13f,.04f,Mathf.Cos(i*4)*.13f),new Vector3(.035f,.025f,.04f),black,t);
            var cigar=Cylinder("Сигара",new Vector3(.04f,.1f,.02f),new Vector3(.055f,.4f,.055f),wood,t);cigar.transform.localRotation=Quaternion.Euler(82,0,-34);
            var ember=Cylinder("Тлеющий кончик",Vector3.zero,new Vector3(1,.045f,1),glow,cigar.transform);ember.transform.localPosition=new Vector3(0,-.5f,0);ember.transform.localScale=new Vector3(1,.045f,1);
            var smoke=Group("Дым над пепельницей",t).gameObject.AddComponent<ParticleSystem>();smoke.transform.localPosition=cigar.transform.localPosition+cigar.transform.localRotation*new Vector3(0,-.2f,0);smoke.transform.localRotation=Quaternion.Euler(-90,0,0);
            var main=smoke.main;main.startLifetime=new ParticleSystem.MinMaxCurve(3,5);main.startSpeed=.08f;main.startSize=new ParticleSystem.MinMaxCurve(.05f,.10f);main.startColor=new Color(.8f,.81f,.75f,.42f);main.maxParticles=90;main.simulationSpace=ParticleSystemSimulationSpace.World;main.prewarm=true;
            var emission=smoke.emission;emission.rateOverTime=8;var shape=smoke.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.012f;shape.angle=10;
            var velocity=smoke.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=new ParticleSystem.MinMaxCurve(-.045f,.045f);velocity.y=new ParticleSystem.MinMaxCurve(.1f,.18f);velocity.z=new ParticleSystem.MinMaxCurve(-.02f,.02f);
            var size=smoke.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.3f),new Keyframe(1,3)));
            var fade=smoke.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.7f,.15f),new GradientAlphaKey(0,1)});fade.color=gradient;
            smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Trailer/Art/Smoke.mat");
        }
        static void Lamp()
        {
            var t=Group("Настольная лампа",group);t.position=new Vector3(-1.9f,.89f,.05f);
            Cylinder("Основание",new Vector3(0,.035f,0),new Vector3(.35f,.07f,.35f),brass,t);
            var stem=Box("Стойка лампы",new Vector3(0,.36f,0),new Vector3(.045f,.7f,.045f),brass,t);stem.transform.localRotation=Quaternion.Euler(0,0,8);
            var shade=Cylinder("Восьмигранный абажур",new Vector3(.1f,.73f,0),new Vector3(.54f,.19f,.54f),brass,t);
            Cylinder("Тёплая поверхность под абажуром",new Vector3(.1f,.63f,0),new Vector3(.49f,.025f,.49f),glow,t);
            Point(new Vector3(.1f,.59f,0),new Color(1,.65f,.27f),2.2f,3,t);
        }
        static void Mug()
        {
            var t=Group("Кружка POLICE",group);t.localPosition=new Vector3(1.98f,.89f,.1f);
            Cylinder("Кружка",new Vector3(0,.13f,0),new Vector3(.23f,.26f,.23f),ivory,t);Cylinder("Кофе",new Vector3(0,.262f,0),new Vector3(.19f,.008f,.19f),black,t);
            Box("Ручка",new Vector3(.17f,.14f,0),new Vector3(.12f,.17f,.06f),ivory,t);Box("Отверстие ручки",new Vector3(.17f,.14f,-.008f),new Vector3(.06f,.09f,.065f),trim,t);
            Text("POLICE",new Vector3(0,.15f,-.122f),.009f,t,new Color(.12f,.17f,.23f));
        }
        static void BackOffice()
        {
            group=Group("05 · Доски и архивные заметки");
            Box("Пробковая доска",new Vector3(.05f,1.85f,4.85f),new Vector3(2.6f,1.15f,.1f),wood);
            for(int i=0;i<8;i++)
            {
                float x=-1+i*.29f;Box("Заметка на доске",new Vector3(x,1.75f+(i%3)*.15f,4.78f),new Vector3(.23f,.36f,.015f),paper);
                Cylinder("Кнопка",new Vector3(x,1.92f+(i%3)*.15f,4.755f),new Vector3(.025f,.012f,.025f),red).transform.Rotate(90,0,0);
            }
            Box("Карта города · бумага",new Vector3(-1.6f,2.03f,4.82f),new Vector3(.75f,1.15f,.02f),ivory);
            for(int i=0;i<12;i++){var street=Box("Линии карты",new Vector3(-1.6f,1.53f+i*.085f,4.8f),new Vector3(.52f,.009f,.008f),steel);street.transform.Rotate(0,0,22);}
            var river=Box("Река на карте",new Vector3(-1.65f,2.04f,4.77f),new Vector3(.065f,1.05f,.01f),blue);river.transform.Rotate(0,0,-17);
            var machine=Group("Пишущая машинка",group);machine.position=new Vector3(.8f,.89f,4.18f);
            Box("Корпус",new Vector3(0,.08f,0),new Vector3(.63f,.16f,.42f),black,machine);Box("Печатный валик",new Vector3(0,.21f,.12f),new Vector3(.63f,.13f,.12f),steel,machine);Box("Лист в машинке",new Vector3(0,.36f,.15f),new Vector3(.42f,.31f,.018f),ivory,machine);
            for(int row=0;row<3;row++)for(int col=0;col<9;col++)Box("Клавиша",new Vector3(-.25f+col*.06f,.18f,-.16f+row*.055f),new Vector3(.034f,.025f,.034f),ivory,machine);
        }
        static void FanAndClock()
        {
            group=Group("06 · Вентилятор и часы");
            Cylinder("Крепление вентилятора",new Vector3(.2f,3.05f,2.3f),new Vector3(.23f,.17f,.23f),brass);
            Cylinder("Подвес",new Vector3(.2f,2.85f,2.3f),new Vector3(.07f,.37f,.07f),brass);
            atmosphere.fan=Group("Вращающийся потолочный вентилятор",group);atmosphere.fan.position=new Vector3(.2f,2.67f,2.3f);
            Cylinder("Мотор",Vector3.zero,new Vector3(.4f,.2f,.4f),brass,atmosphere.fan);
            for(int i=0;i<5;i++)
            {var blade=Group("Лопасть",atmosphere.fan);blade.localRotation=Quaternion.Euler(0,i*72,0);Box("Деревянная лопасть",new Vector3(.57f,0,0),new Vector3(.95f,.045f,.2f),wood,blade);}
            var clock=Group("Настенные часы",group);clock.position=new Vector3(1.25f,2.55f,4.78f);
            var body=Cylinder("Обод",Vector3.zero,new Vector3(.64f,.075f,.64f),black,clock);body.transform.localRotation=Quaternion.Euler(90,0,0);
            var face=Cylinder("Циферблат",new Vector3(0,0,-.045f),new Vector3(.56f,.02f,.56f),ivory,clock);face.transform.localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<12;i++)
            {float a=i*Mathf.PI/6;var tick=Box("Метка часов",new Vector3(Mathf.Sin(a)*.23f,Mathf.Cos(a)*.23f,-.06f),new Vector3(.015f,.04f,.01f),black,clock);tick.transform.Rotate(0,0,-i*30);}
            var hour=Box("Часовая стрелка",new Vector3(-.04f,.04f,-.074f),new Vector3(.025f,.18f,.012f),black,clock);hour.transform.Rotate(0,0,45);
            var minute=Box("Минутная стрелка",new Vector3(.08f,.025f,-.08f),new Vector3(.22f,.018f,.012f),black,clock);minute.transform.Rotate(0,0,-20);
            atmosphere.secondHand=Group("Секундная стрелка",clock);Box("Стрелка",new Vector3(0,.09f,-.09f),new Vector3(.008f,.23f,.008f),red,atmosphere.secondHand);
        }
        static void Hallway()
        {
            group=Group("07 · Открытая дверь и участок за ней");
            var entry=Piece("doorway",new Vector3(3,0,5));entry.name="Дверь DETECTIVES · модуль участка";
            foreach(var r in entry.GetComponent<Hakaton.LocationKit.LocationPiece>().paintTargets){r.sharedMaterial=wall;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            var door=entry.GetComponentInChildren<DemoDoor>();door.openAngle=-65;door.SetOpen(true);door.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(door);PrefabUtility.RecordPrefabInstancePropertyModifications(door.transform);
            var panel=door.transform.Find("Полотно двери");panel.GetComponent<Renderer>().enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(panel.GetComponent<Renderer>());
            foreach(float x in new[]{.05f,1.65f})Box("Стойка двери",new Vector3(x,1.1f,0),new Vector3(.12f,2.2f,.14f),trim,door.transform);
            Box("Низ двери",new Vector3(.85f,.42f,0),new Vector3(1.6f,.84f,.14f),wood,door.transform);Box("Верх двери",new Vector3(.85f,2.15f,0),new Vector3(1.6f,.12f,.14f),wood,door.transform);
            var pane=Box("Матовое стекло двери",new Vector3(.85f,1.47f,0),new Vector3(1.45f,1.18f,.03f),glass,door.transform);
            Text("DETECTIVES",new Vector3(.85f,1.5f,-.04f),.035f,door.transform);
            Box("Коридор · пол",new Vector3(3.5f,-.1f,9),new Vector3(3,.2f,8),wood);
            foreach(float x in new[]{2f,5f})Box("Коридор · стена",new Vector3(x,1.55f,9),new Vector3(.15f,3.1f,8),wall);
            Box("Коридор · потолок",new Vector3(3.5f,3.15f,9),new Vector3(3,.15f,8),wall);Box("Коридор · дальняя стена",new Vector3(3.5f,1.55f,13),new Vector3(3,3.1f,.15f),wall);
            foreach(float z in new[]{7.2f,10.8f})
            {Box("Свет в коридоре",new Vector3(3.5f,3,z),new Vector3(1.4f,.08f,.38f),glow);Point(new Vector3(3.5f,2.8f,z),new Color(.77f,.86f,.82f),2,5);}
            Piece("cabinet",new Vector3(4.4f,0,11),-90);
            var samples=Objects<StationNpc>().Where(n=>!n.seated).Take(2).ToArray();
            for(int i=0;i<samples.Length;i++)
            {
                var o=UnityEngine.Object.Instantiate(samples[i].gameObject);SceneManager.MoveGameObjectToScene(o,SceneManager.GetActiveScene());o.transform.SetParent(group,false);o.name="Сотрудник в коридоре "+(i+1);o.transform.position=new Vector3(4.3f+i*.15f,0,7.4f+i*2.7f);o.transform.rotation=Quaternion.Euler(0,i==0?180:110,0);o.GetComponent<StationNpc>().route=Array.Empty<Vector3>();
            }
        }
        static void Flag()
        {
            group=Group("08 · Флаг у двери");
            Cylinder("Древко",new Vector3(4.65f,1.4f,4.3f),new Vector3(.04f,2.8f,.04f),brass);Cylinder("Подставка флага",new Vector3(4.65f,.035f,4.3f),new Vector3(.4f,.07f,.4f),black);
            for(int i=0;i<13;i++)Box("Полоса флага",new Vector3(4.2f,2.6f-i*.07f,4.3f),new Vector3(.86f,.07f,.025f),i%2==0?red:ivory);
            Box("Поле звёзд",new Vector3(3.96f,2.4f,4.28f),new Vector3(.39f,.46f,.018f),blue);
            for(int x=0;x<4;x++)for(int y=0;y<5;y++)Box("Звезда",new Vector3(3.83f+x*.085f,2.25f+y*.08f,4.263f),new Vector3(.018f,.018f,.008f),ivory);
        }
        static void Lighting()
        {
            group=Group("09 · Тёплый свет через жалюзи");
            var sun=Group("Вечерний свет · тени от ламелей",group).gameObject.AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(28,65,0);sun.color=new Color(1,.78f,.49f);sun.intensity=1.55f;sun.shadows=LightShadows.Hard;sun.shadowBias=.025f;sun.shadowNormalBias=.025f;sun.gameObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.47f,.4f,.29f);RenderSettings.ambientEquatorColor=new Color(.3f,.25f,.18f);RenderSettings.ambientGroundColor=new Color(.16f,.125f,.08f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.39f,.32f,.23f);RenderSettings.fogStartDistance=16;RenderSettings.fogEndDistance=45;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 p,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=p;r.sizeDelta=size;return r;
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static Text UI(string name,Transform parent,string text,int size,Vector2 p,Vector2 box,Color? color=null)
        {
            var r=Rect(name,parent,p,box);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=text;t.color=color??new Color(.88f,.84f,.74f);t.alignment=TextAnchor.MiddleLeft;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
        }
        static Button Button(string name,Transform parent,string caption,Vector2 p,Vector2 size)
        {
            var r=Rect(name,parent,p,size);var background=r.gameObject.AddComponent<Image>();background.color=Color.white;var button=r.gameObject.AddComponent<Button>();
            var colors=button.colors;colors.normalColor=new Color(.8f,.65f,.4f,.01f);colors.highlightedColor=new Color(.8f,.65f,.4f,.10f);colors.selectedColor=new Color(.8f,.65f,.4f,.12f);colors.pressedColor=new Color(.8f,.65f,.4f,.2f);button.colors=colors;
            var label=UI("Название",r,caption,21,new Vector2(22,0),new Vector2(size.x-26,size.y));
            var marker=Rect("Отметка выбора",r,new Vector2(0,12),new Vector2(2,size.y-24)).gameObject.AddComponent<Image>();marker.color=new Color(.86f,.61f,.3f);
            var feedback=r.gameObject.AddComponent<MenuButtonFeedback>();feedback.label=label;feedback.marker=marker;return button;
        }
        static Slider Slider(string name,Transform parent,Vector2 p,float min,float max)
        {
            var r=Rect(name,parent,p,new Vector2(400,34));var hit=r.gameObject.AddComponent<Image>();hit.color=Color.clear;var slider=r.gameObject.AddComponent<Slider>();slider.minValue=min;slider.maxValue=max;
            var rail=Rect("Шкала",r,new Vector2(0,15),new Vector2(400,3)).gameObject.AddComponent<Image>();rail.color=new Color(.3f,.29f,.24f);
            var fillArea=Rect("Область",r,new Vector2(0,15),new Vector2(400,3));var fill=Rect("Значение",fillArea,Vector2.zero,new Vector2(400,3)).gameObject.AddComponent<Image>();fill.color=new Color(.8f,.61f,.3f);fill.rectTransform.sizeDelta=Vector2.zero;slider.fillRect=fill.rectTransform;
            var handleArea=Rect("Ручка · область",r,new Vector2(8,0),new Vector2(384,34));var handle=Rect("Ручка",handleArea,new Vector2(0,6),new Vector2(8,22)).gameObject.AddComponent<Image>();handle.color=new Color(.9f,.81f,.59f);handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;handle.rectTransform.sizeDelta=new Vector2(8,-12);slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;return slider;
        }
        static void Interface()
        {
            group=Group("10 · Камера и интерфейс");
            var camera=Group("Камера кабинета",group).gameObject.AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(.8f,1.4f,-2.0f);camera.transform.LookAt(new Vector3(-.15f,1.5f,3.2f));camera.fieldOfView=68;camera.nearClipPlane=.05f;camera.farClipPlane=60;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.65f,.53f,.37f);camera.gameObject.AddComponent<AudioListener>();camera.gameObject.AddComponent<UniversalAdditionalCameraData>();atmosphere.cameraRig=camera.transform;
            var canvas=Group("Главное меню · UI",group).gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.gameObject.AddComponent<GraphicRaycaster>();var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var world=Rect("Живой кабинет",canvas.transform,Vector2.zero,Vector2.zero).gameObject.AddComponent<RawImage>();Stretch(world.rectTransform);world.raycastTarget=false;
            var grade=new Material(Shader.Find("Hakaton/Menu Grade")){name="Warm detective menu"};AssetDatabase.CreateAsset(grade,Art+"/MenuGrade.mat");
            var retro=canvas.gameObject.AddComponent<RetroView>();retro.worldCamera=camera;retro.display=world;retro.displayMaterial=grade;retro.verticalResolution=360;
            var texture=new Texture2D(128,1,TextureFormat.RGBA32,false){name="UI readability gradient",wrapMode=TextureWrapMode.Clamp};for(int x=0;x<128;x++){float u=x/127f;texture.SetPixel(x,0,new Color(.025f,.02f,.014f,.78f*(1-Mathf.SmoothStep(0,1,u/.55f))));}texture.Apply();AssetDatabase.CreateAsset(texture,Art+"/MenuScrim.asset");
            var scrim=Rect("Мягкое затемнение под текстом",canvas.transform,Vector2.zero,Vector2.zero).gameObject.AddComponent<RawImage>();Stretch(scrim.rectTransform);scrim.texture=texture;scrim.raycastTarget=false;
            menu.menuPanel=Rect("Навигация",canvas.transform,new Vector2(70,110),new Vector2(390,500));
            UI("Название игры",menu.menuPanel,"ДОМ\nПОМНИТ",58,new Vector2(0,286),new Vector2(380,150));
            UI("Место и время",menu.menuPanel,"НЬЮ-ЙОРК / 2000",13,new Vector2(3,448),new Vector2(350,24),new Color(.77f,.62f,.39f));
            var line=Rect("Тонкая линия",menu.menuPanel,new Vector2(3,267),new Vector2(43,2)).gameObject.AddComponent<Image>();line.color=new Color(.73f,.51f,.25f);
            menu.newGame=Button("New game",menu.menuPanel,"НОВАЯ ИГРА",new Vector2(0,201),new Vector2(340,48));
            menu.introduction=Button("Watch introduction",menu.menuPanel,"ВСТУПЛЕНИЕ",new Vector2(0,145),new Vector2(340,48));
            menu.settings=Button("Settings",menu.menuPanel,"НАСТРОЙКИ",new Vector2(0,89),new Vector2(340,48));
            menu.quit=Button("Quit",menu.menuPanel,"ВЫХОД",new Vector2(0,33),new Vector2(340,48));
            UI("Подпись",canvas.transform,"ПСИХОЛОГИЧЕСКИЙ ДЕТЕКТИВ",11,new Vector2(73,36),new Vector2(500,20),new Color(.65f,.61f,.5f));
            menu.status=UI("Статус",canvas.transform,"",16,new Vector2(70,74),new Vector2(620,32));
            menu.settingsPanel=Rect("Параметры",canvas.transform,new Vector2(60,160),new Vector2(520,420));var shade=menu.settingsPanel.gameObject.AddComponent<Image>();shade.color=new Color(.055f,.047f,.032f,.96f);
            UI("Заголовок",menu.settingsPanel,"НАСТРОЙКИ",29,new Vector2(38,330),new Vector2(450,55));
            UI("Громкость",menu.settingsPanel,"Громкость",19,new Vector2(40,264),new Vector2(330,35));menu.volumeValue=UI("Громкость · число",menu.settingsPanel,"",16,new Vector2(390,264),new Vector2(80,35));menu.volume=Slider("Master volume",menu.settingsPanel,new Vector2(40,224),0,1);
            UI("Чувствительность",menu.settingsPanel,"Чувствительность мыши",19,new Vector2(40,150),new Vector2(350,35));menu.mouseValue=UI("Мышь · число",menu.settingsPanel,"",16,new Vector2(390,150),new Vector2(80,35));menu.mouse=Slider("Mouse sensitivity",menu.settingsPanel,new Vector2(40,110),.03f,.3f);
            menu.back=Button("Close settings",menu.settingsPanel,"НАЗАД [ ESC ]",new Vector2(40,32),new Vector2(400,48));
            menu.transition=Rect("Переход к сцене",canvas.transform,Vector2.zero,Vector2.zero).gameObject.AddComponent<Image>();Stretch(menu.transition.rectTransform);menu.transition.color=new Color(0,0,0,0);menu.transition.raycastTarget=false;
            menu.settingsPanel.gameObject.SetActive(false);
            new GameObject("Ввод меню",typeof(EventSystem),typeof(InputSystemUIInputModule));
        }
    }
}
