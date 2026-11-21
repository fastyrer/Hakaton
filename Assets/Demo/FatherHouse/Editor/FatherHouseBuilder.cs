using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hakaton.LocationKit;
using Hakaton.LocationKit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace Hakaton.Demo.FatherHouse.Editor
{
    public static class FatherHouseBuilder
    {
        public const string ScenePath="Assets/Scenes/FatherHouse.unity";
        const string Art="Assets/Demo/FatherHouse/Art";
        static LocationRoot location;
        static Transform group;
        static Scene menuSource;
        static Material wood,dark,plaster,paper,ink,metal,cloth,ivory,rust,glass,screen,red,soil,concrete,siding;
        static Font font;static Mesh cylinder;static System.Random rng;
        static readonly List<Transform> cameras=new List<Transform>();
        [MenuItem("Hakaton/Дом отца/Создать или открыть")]
        public static string Build()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Остановите Play и сохраните текущую сцену.");
            if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return ScenePath;}
            if(KitLibraryBuilder.Load()==null)KitLibraryBuilder.Build();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            location=LocationKitOperations.EnsureRoot();location.locationName="Дом отца";
            location.introduction="За последними домами дорога превращается в лесную колею.\nОтец велел никому не сообщать этот адрес.\n\nОкна заколочены изнутри. На двери — несколько засовов.\nМашина останется у ворот. Нужно найти его кабинет.";
            location.objective="Осмотрите дом отца · E — осмотр и двери · Tab — доска расследования";
            LocationKitOperations.AddPlayerRig();LocationKitOperations.SetSpawn(new Vector3(0,.08f,-14),0);
            location.game.arrivalScene=true;location.game.hud.worldVerticalResolution=360;location.game.hud.locationCaption="НЬЮ-ДЖЕРСИ / ЛЕСНАЯ ОКРАИНА / 2000";
            location.game.player.view.farClipPlane=145;location.game.player.view.fieldOfView=72;location.game.player.flashlight.enabled=false;
            font=location.game.hud.font??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");rng=new System.Random(1967);cameras.Clear();
            menuSource=EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainMenu.unity");
            try
            {
                Materials();Floors();OuterShell();Partitions();Stairs();Exterior();LivingAndDining();StudyAndHidden();KitchenAndWorkshop();BedroomsAndLibrary();Basement();Environment();Dressing();
                var ambience=location.gameObject.AddComponent<HouseAmbience>();ambience.cameras=cameras.ToArray();
                ambience.weakBulb=location.lights.GetComponentsInChildren<Light>().FirstOrDefault(l=>l.name=="Лампа котельной");
                LocationKitOperations.RefreshConnections(location);RecordPrefabOverrides();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);
                var list=EditorBuildSettings.scenes.ToList();if(!list.Any(s=>s.path==ScenePath))list.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=list.ToArray();
                AssetDatabase.SaveAssets();Selection.activeGameObject=location.gameObject;
                SceneView.lastActiveSceneView?.LookAt(new Vector3(0,3,12),Quaternion.Euler(28,-25,0),40);
                return ScenePath;
            }
            finally{EditorSceneManager.ClosePreviewScene(menuSource);}
        }
        static void RecordPrefabOverrides()
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(location.game);PrefabUtility.RecordPrefabInstancePropertyModifications(location.game.hud);
            PrefabUtility.RecordPrefabInstancePropertyModifications(location.game.player);PrefabUtility.RecordPrefabInstancePropertyModifications(location.game.player.view);PrefabUtility.RecordPrefabInstancePropertyModifications(location.game.player.flashlight);
        }
        static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent??location.props,false);return t;}
        static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,bool collision=false,Transform parent=null)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent??group,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collision)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;
        }
        static GameObject Cylinder(string name,Vector3 p,Vector3 size,Material mat,Transform parent=null)
        {var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent??group,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<MeshFilter>().sharedMesh=cylinder;o.GetComponent<Renderer>().sharedMaterial=mat;return o;}
        static Material Mat(string name,Color color,string template=null)
        {
            string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            var source=template==null?null:AssetDatabase.LoadAssetAtPath<Material>(template);m=source==null?new Material(Shader.Find("Universal Render Pipeline/Lit")):new Material(source);m.name=name;m.color=color;m.SetFloat("_Smoothness",.12f);AssetDatabase.CreateAsset(m,path);return m;
        }
        static void Materials()
        {
            wood=Mat("Scratched_boards",new Color(.34f,.23f,.13f),"Assets/Demo/Menu/Art/Desk_walnut.mat");wood.mainTextureScale=new Vector2(2,2);
            dark=Mat("Blackened_timber",new Color(.12f,.09f,.055f),"Assets/Demo/Menu/Art/Wood_trim.mat");
            plaster=Mat("Damp_wallpaper",new Color(.38f,.37f,.27f),"Assets/Demo/Art/Generated/Striped_wallpaper.mat");plaster.mainTextureScale=new Vector2(2,1);
            paper=Mat("Yellowed_paper",new Color(.64f,.57f,.39f));ink=Mat("Ink_and_ashes",new Color(.027f,.029f,.025f));metal=Mat("Dull_steel",new Color(.25f,.28f,.25f));
            cloth=Mat("Old_burgundy",new Color(.25f,.085f,.065f),"Assets/Demo/Art/Generated/Worn_burgundy.mat");ivory=Mat("Dirty_enamel",new Color(.56f,.53f,.41f));rust=Mat("Rust",new Color(.31f,.16f,.07f));
            red=Mat("Warning_red",new Color(.42f,.07f,.035f));red.EnableKeyword("_EMISSION");red.SetColor("_EmissionColor",new Color(.4f,.015f,.005f));
            glass=Mat("Dead_glass",new Color(.055f,.09f,.085f));soil=Mat("Forest_earth",new Color(.12f,.16f,.105f),"Assets/Demo/Art/Arrival/Wet_grass.mat");soil.mainTextureScale=new Vector2(4,4);
            concrete=Mat("Basement_concrete",new Color(.27f,.28f,.24f),"Assets/Demo/Art/Arrival/Concrete.mat");concrete.mainTextureScale=new Vector2(3,3);
            siding=Mat("Mouldy_siding",new Color(.32f,.34f,.28f),"Assets/Demo/Art/Arrival/Weathered_siding.mat");siding.mainTextureScale=new Vector2(3,3);
            screen=Mat("Surveillance_screen",new Color(.18f,.30f,.23f));screen.EnableKeyword("_EMISSION");screen.SetColor("_EmissionColor",new Color(.07f,.15f,.09f));
            var noise=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/ScreenNoise.asset");
            if(noise==null){noise=new Texture2D(64,64,TextureFormat.RGB24,false){filterMode=FilterMode.Point,name="Old surveillance scanlines"};for(int y=0;y<64;y++)for(int x=0;x<64;x++){float v=(float)rng.NextDouble()*.3f+.2f;if(y%3==0)v*=.45f;noise.SetPixel(x,y,new Color(v*.6f,v,v*.7f));}noise.Apply();AssetDatabase.CreateAsset(noise,Art+"/ScreenNoise.asset");}screen.mainTexture=noise;
            cylinder=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Demo/Art/Generated/Cylinder8.asset");
        }
        static GameObject Kit(string id,Vector3 p,float yaw=0,Vector3? scale=null)
        {
            var o=(GameObject)PrefabUtility.InstantiatePrefab(KitLibraryBuilder.Load().Find(id).prefab);o.transform.SetParent(group,false);o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));if(scale.HasValue)o.transform.localScale=scale.Value;
            PrefabUtility.RecordPrefabInstancePropertyModifications(o.transform);return o;
        }
        static GameObject MenuProp(string name,Vector3 p,float yaw=0,float scale=1)
        {
            var t=menuSource.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).First(t=>t.name==name);var o=UnityEngine.Object.Instantiate(t.gameObject);SceneManager.MoveGameObjectToScene(o,SceneManager.GetActiveScene());o.transform.SetParent(group,false);o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));o.transform.localScale=Vector3.one*scale;return o;
        }
        static void Label(string text,Vector3 p,float size=.025f,float yaw=0,Transform parent=null)
        {
            var t=Group(text,parent??group);t.localPosition=p;t.localRotation=Quaternion.Euler(0,yaw,0);var m=t.gameObject.AddComponent<TextMesh>();m.font=font;m.text=text;m.fontSize=48;m.characterSize=size;m.anchor=TextAnchor.MiddleCenter;m.alignment=TextAlignment.Center;m.color=new Color(.69f,.62f,.43f);t.gameObject.AddComponent<WorldTextLabel>().depthShader=Shader.Find("Hakaton/World Text");
        }
        static Light Point(string name,Vector3 p,float power=1.4f,float range=7,Color? color=null)
        {
            var light=Group(name,location.lights).gameObject.AddComponent<Light>();light.transform.position=p;light.type=LightType.Point;light.color=color??new Color(1,.63f,.30f);light.intensity=power;light.range=range;light.shadows=LightShadows.None;return light;
        }
        static void Slab(string name,float x0,float x1,float z0,float z1,float y,Material material,float thickness=.2f)
        {Box(name,new Vector3((x0+x1)/2,y-thickness/2,(z0+z1)/2),new Vector3(x1-x0,thickness,z1-z0),material,true);}
        static void Floors()
        {
            group=Group("01 · Перекрытия и настоящие лестничные проёмы",location.architecture);
            Slab("Первый этаж · запад",-12,2,0,28,0,wood);Slab("Первый этаж · восток",4.8f,12,0,28,0,wood);Slab("Первый этаж · перед подвалом",2,4.8f,0,18,0,wood);Slab("Первый этаж · за подвалом",2,4.8f,24.5f,28,0,wood);
            foreach(float y in new[]{3.18f,3.4f})
            {
                var mat=y<3.3f?plaster:wood;Slab("Перекрытие · запад",-12,2,0,28,y,mat,y<3.3f?.08f:.18f);Slab("Перекрытие · восток",6,12,0,28,y,mat,y<3.3f?.08f:.18f);
                Slab("Перекрытие · юг",2,6,0,5,y,mat,y<3.3f?.08f:.18f);Slab("Перекрытие · север",2,6,11,28,y,mat,y<3.3f?.08f:.18f);
            }
            Slab("Потолок второго этажа",-12,12,0,28,6.62f,plaster,.12f);Slab("Подвал · бетонный пол",2,12,17,28,-3.4f,concrete,.25f);
            Slab("Крыльцо",-4,4,-4,0,0,wood,.22f);
        }
        static void SolidWall(string name,Vector3 center,Vector3 size,Material material=null)
        {Box(name,center,size,material??plaster,true);}
        static void WallX(float x,float z0,float z1,float y,float[] holes=null,string[] names=null)
        {
            holes=holes??Array.Empty<float>();float cursor=z0;
            for(int i=0;i<holes.Length;i++){float lo=holes[i]-.9f,hi=holes[i]+.9f;if(lo>cursor)SolidWall("Стена",new Vector3(x,y+1.6f,(cursor+lo)/2),new Vector3(.2f,3.2f,lo-cursor));SolidWall("Над проёмом",new Vector3(x,y+2.78f,holes[i]),new Vector3(.2f,.84f,1.8f));if(names!=null)Door(new Vector3(x,y,holes[i]),90,names[i]);cursor=hi;}
            if(cursor<z1)SolidWall("Стена",new Vector3(x,y+1.6f,(cursor+z1)/2),new Vector3(.2f,3.2f,z1-cursor));
        }
        static void WallZ(float z,float x0,float x1,float y,float[] holes=null,string[] names=null)
        {
            holes=holes??Array.Empty<float>();float cursor=x0;
            for(int i=0;i<holes.Length;i++){float lo=holes[i]-.9f,hi=holes[i]+.9f;if(lo>cursor)SolidWall("Стена",new Vector3((cursor+lo)/2,y+1.6f,z),new Vector3(lo-cursor,3.2f,.2f));SolidWall("Над проёмом",new Vector3(holes[i],y+2.78f,z),new Vector3(1.8f,.84f,.2f));if(names!=null)Door(new Vector3(holes[i],y,z),0,names[i]);cursor=hi;}
            if(cursor<x1)SolidWall("Стена",new Vector3((cursor+x1)/2,y+1.6f,z),new Vector3(x1-cursor,3.2f,.2f));
        }
        static DemoDoor Door(Vector3 p,float yaw,string name)
        {
            var frame=Group("Дверь · "+name,group);frame.position=p;frame.rotation=Quaternion.Euler(0,yaw,0);
            foreach(float x in new[]{-.86f,.86f})Box("Наличник",new Vector3(x,1.16f,0),new Vector3(.10f,2.32f,.25f),dark,false,frame);Box("Наличник сверху",new Vector3(0,2.31f,0),new Vector3(1.8f,.10f,.25f),dark,false,frame);
            var hinge=Group("Петля",frame);hinge.localPosition=new Vector3(-.75f,0,0);var door=hinge.gameObject.AddComponent<DemoDoor>();door.label=name;door.openAngle=-102;
            Box("Полотно",new Vector3(.75f,1.12f,0),new Vector3(1.46f,2.24f,.12f),wood,true,hinge);
            foreach(float z in new[]{-.10f,.10f})Box("Ручка",new Vector3(1.30f,1.02f,z),new Vector3(.12f,.045f,.10f),metal,false,hinge);
            return door;
        }
        static void Window(Vector3 p,float yaw,bool boarded)
        {
            var t=Group("Окно · закрыто изнутри",group);t.position=p;t.rotation=Quaternion.Euler(0,yaw,0);
            Box("Тёмное стекло",new Vector3(0,1.8f,0),new Vector3(1.75f,1.38f,.035f),glass,true,t);
            foreach(float x in new[]{-.91f,0,.91f})Box("Рама",new Vector3(x,1.8f,-.03f),new Vector3(.07f,1.5f,.13f),dark,false,t);
            foreach(float y in new[]{1.08f,2.53f})Box("Рама",new Vector3(0,y,-.03f),new Vector3(1.9f,.10f,.22f),wood,false,t);
            if(boarded)for(int i=0;i<3;i++){var b=Box("Доска на окне",new Vector3(0,1.32f+i*.43f,-.13f),new Vector3(2.04f,.19f,.06f),wood,false,t);b.transform.Rotate(0,0,i%2==0?13:-9);for(int j=0;j<2;j++)Box("Гвоздь",new Vector3(j==0?-.75f:.75f,1.32f+i*.43f,-.17f),new Vector3(.018f,.018f,.008f),metal,false,t);}
            else for(int i=0;i<11;i++)Box("Пыльная жалюзи",new Vector3(0,1.18f+i*.12f,-.1f),new Vector3(1.7f,.034f,.07f),dark,false,t);
        }
        static void OuterShell()
        {
            group=Group("02 · Корпус дома и заколоченные окна",location.architecture);
            foreach(float y in new[]{0f,3.4f})
            {
                WallZ(0,-12,12,y,y==0?new[]{0f}:null,y==0?new[]{"Входная дверь"}:null);WallZ(28,-12,12,y,new[]{0f},new[]{y==0?"Задняя дверь":"Выход на задний балкон"});WallX(-12,0,28,y);WallX(12,0,28,y);
                // Windows are framed in the thick exterior wall and blocked by boards, as the owner left them.
                for(int z=3;z<28;z+=5){Window(new Vector3(-11.85f,y,z),-90,true);Window(new Vector3(11.85f,y,z),90,z%2==0);}
                foreach(float x in new[]{-8f,-4f,4f,8f}){Window(new Vector3(x,y,-.24f),0,true);Window(new Vector3(x,y,.15f),180,true);}
                for(int z=3;z<28;z+=5){Window(new Vector3(-12.24f,y,z),90,true);Window(new Vector3(12.24f,y,z),-90,true);}
            }
            for(int z=2;z<28;z+=2)foreach(float x in new[]{-12.14f,12.14f})Box("Наружная обшивка",new Vector3(x,3.25f,z),new Vector3(.07f,6.5f,1.93f),siding);
            foreach(float z in new[]{-.13f,28.13f})for(int x=-11;x<=11;x+=2)Box("Наружная обшивка",new Vector3(x,3.25f,z),new Vector3(1.93f,6.5f,.06f),siding);
            // Keep the front doorway clear in the outer siding.
            foreach(var r in group.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Наружная обшивка"&&Mathf.Abs(r.transform.position.x)<2.1f))UnityEngine.Object.DestroyImmediate(r.gameObject);
            foreach(float x in new[]{-12.2f,12.2f})Box("Угловая стойка",new Vector3(x,3.25f,0),new Vector3(.18f,6.6f,.18f),dark);
            for(int x=-10;x<=10;x+=4)Box("Пояс между этажами",new Vector3(x,3.33f,-.18f),new Vector3(3.9f,.22f,.15f),dark);
            // Gabled roof, modelled as two pitched slabs.
            float angle=Mathf.Atan2(2.5f,12.7f)*Mathf.Rad2Deg;float length=Mathf.Sqrt(12.7f*12.7f+2.5f*2.5f);
            for(int side=-1;side<=1;side+=2){var roof=Box("Скат крыши",new Vector3(side*6.35f,7.88f,14),new Vector3(length,.22f,30),dark,true);roof.transform.Rotate(0,0,-side*angle);}
            Box("Конёк",new Vector3(0,9.15f,14),new Vector3(.25f,.22f,30),metal);
            for(int x=-3;x<=3;x+=6)Box("Стойка крыльца",new Vector3(x,1.6f,-3.6f),new Vector3(.22f,3.2f,.22f),dark,true);
            Box("Навес крыльца",new Vector3(0,3.22f,-2),new Vector3(8.5f,.2f,4.6f),dark,true);
            Box("Дымоход",new Vector3(-7,7.6f,22),new Vector3(.9f,3.0f,.9f),concrete,true);
        }
        static void Partitions()
        {
            group=Group("03 · Первый этаж · планировка",location.architecture);
            WallX(-2,0,28,0,new[]{5f,14.5f,23.5f},new[]{"Гостиная","Кабинет отца","Мастерская"});
            // Entrance to the stair gallery is a wide unframed opening.
            WallX(2,0,3.5f,0);WallX(2,7,28,0,new[]{14f,26f},new[]{"Кухонный проход","Кладовая"});
            WallZ(10,-12,-2,0);WallZ(19,-12,-2,0,new[]{-5f},new[]{"Дверь в мастерскую"});WallX(-8,10,19,0,new[]{15f});
            WallX(6,0,18,0,new[]{2.5f,14f},new[]{"Столовая","Кухня"});WallZ(10,6,12,0,new[]{9f},new[]{"Из столовой в кухню"});WallZ(18,6,12,0,new[]{9f},new[]{"Кухонная кладовая"});
            Door(new Vector3(3.4f,0,18),0,"Спуск в подвал");
            group=Group("04 · Второй этаж · планировка",location.architecture);
            WallX(-2,0,28,3.4f,new[]{5f,15.5f,24f},new[]{"Спальня отца","Библиотека","Семейный архив"});WallX(-9,0,20,3.4f,new[]{5f});
            WallZ(11,-9,-2,3.4f);WallZ(20,-12,-2,3.4f,new[]{-10.5f,-5.5f});Door(new Vector3(-5.5f,3.4f,20),0,"Дверь архива");
            WallX(2,0,3.5f,3.4f);WallX(2,5,28,3.4f,new[]{14f,21f,26f},new[]{"Верхний восточный коридор","Детская комната","Комната карт"});
            WallX(6,0,28,3.4f,new[]{2.5f,14f,20.5f,26f},new[]{"Гостевая спальня","Ванная","Комната сыновей","Внутренняя дверь комнаты карт"});
            WallZ(11,6,12,3.4f);WallZ(17,6,12,3.4f);WallZ(24,6,12,3.4f);
            group=Group("05 · Подвал · планировка",location.architecture);
            WallX(2,17,28,-3.4f);WallX(12,17,28,-3.4f);WallZ(17,2,12,-3.4f);WallZ(28,2,12,-3.4f);
            WallX(8,17,28,-3.4f,new[]{19.5f,25f});Door(new Vector3(8,-3.4f,19.5f),90,"Котельная");WallZ(22,8,12,-3.4f);
            foreach(var wall in group.GetComponentsInChildren<Renderer>())if(wall.name=="Стена"||wall.name=="Над проёмом")wall.sharedMaterial=concrete;
        }
        static void Rail(Vector3 from,Vector3 to)
        {
            var delta=to-from;var bar=Box("Поручень",(from+to)/2+Vector3.up*.88f,new Vector3(.07f,.07f,delta.magnitude+.08f),dark,true);bar.transform.rotation=Quaternion.LookRotation(delta);
            for(int i=0;i<=4;i++)Box("Балясина",Vector3.Lerp(from,to,i/4f)+Vector3.up*.43f,new Vector3(.06f,.86f,.06f),wood);
        }
        static void Stairs()
        {
            group=Group("06 · Лестницы без телепортации",location.architecture);
            for(int i=0;i<10;i++)
            {
                float top=(i+1)*.17f;Box("Главная лестница · марш 1 · "+i,new Vector3(3,top/2,5+(i+.5f)*.42f),new Vector3(1.5f,top,.42f),wood,true);
                float upper=1.7f+(i+1)*.17f;Box("Главная лестница · марш 2 · "+i,new Vector3(5,(1.7f+upper)/2,9.2f-(i+.5f)*.42f),new Vector3(1.5f,upper-1.7f,.42f),wood,true);
            }
            Slab("Промежуточная площадка",2.25f,5.75f,9.2f,11,1.7f,wood,.2f);
            Rail(new Vector3(2.2f,0,5),new Vector3(2.2f,1.7f,9.2f));Rail(new Vector3(3.8f,0,5),new Vector3(3.8f,1.7f,9.2f));
            Rail(new Vector3(4.2f,1.7f,9.2f),new Vector3(4.2f,3.4f,5));Rail(new Vector3(5.8f,1.7f,9.2f),new Vector3(5.8f,3.4f,5));
            Rail(new Vector3(2,3.4f,5),new Vector3(2,3.4f,11));Rail(new Vector3(6,3.4f,5),new Vector3(6,3.4f,11));Rail(new Vector3(2,3.4f,11),new Vector3(6,3.4f,11));Rail(new Vector3(2,3.4f,5),new Vector3(4,3.4f,5));
            for(int i=0;i<20;i++){float top=-(i+1)*.17f;if(top>-3.4f+.001f)Box("Подвальная лестница · "+i,new Vector3(3.4f,(-3.4f+top)/2,18+(i+.5f)*.3f),new Vector3(1.6f,top+3.4f,.3f),dark,true);}
            Rail(new Vector3(2.1f,0,18),new Vector3(2.1f,0,24.5f));Rail(new Vector3(4.7f,0,18),new Vector3(4.7f,0,24.5f));
        }
        public static string DressExisting()
        {
            location=UnityEngine.Object.FindAnyObjectByType<LocationRoot>();rng=new System.Random(1967);font=location.game.hud.font??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");Materials();
            if(!location.props.Cast<Transform>().Any(t=>t.name=="21 · Отделка, светильники и следы обитателя"))Dressing();
            var covers=location.gameplay.GetComponentsInChildren<LocationShelter>();
            var sizes=new[]{new Vector3(23.8f,6.6f,27.8f),new Vector3(9.8f,3.4f,10.8f),new Vector3(7.7f,3.2f,3.7f)};
            for(int n=0;n<covers.Length;n++){var c=covers[n].GetComponent<BoxCollider>();c.center=Vector3.zero;c.size=sizes[n];}
            LocationKitOperations.RefreshConnections(location);RecordPrefabOverrides();EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);AssetDatabase.SaveAssets();return ScenePath;
        }
        static void Dressing()
        {
            group=Group("21 · Отделка, светильники и следы обитателя");
            foreach(var wall in location.architecture.GetComponentsInChildren<Transform>().Where(t=>t.name=="Стена").ToArray())
            {
                var size=wall.localScale;float floor=wall.position.y-1.6f;
                Box("Потёртый плинтус",new Vector3(wall.position.x,floor+.09f,wall.position.z),new Vector3(size.x+.028f,.18f,size.z+.028f),dark);
                Box("Потолочный карниз",new Vector3(wall.position.x,floor+3.08f,wall.position.z),new Vector3(size.x+.04f,.13f,size.z+.04f),wood);
            }
            foreach(var l in location.lights.GetComponentsInChildren<Light>().Where(l=>l.type==LightType.Point).ToArray())
            {
                l.intensity*=1.35f;
                if(l.name=="Экраны скрытой комнаты")continue;
                var p=l.transform.position;Cylinder("Патрон тусклой лампы",p+Vector3.up*.16f,new Vector3(.16f,.15f,.16f),dark);Cylinder("Запылённый плафон",p+Vector3.up*.06f,new Vector3(.38f,.06f,.38f),metal);Cylinder("Лампочка",p,new Vector3(.12f,.17f,.12f),ivory);
            }
            // Water damage, loose wallpaper and tape occur on walls, not as walking obstacles.
            foreach(var pair in new[]{new Vector3(-1.87f,0,9),new Vector3(-1.87f,0,20),new Vector3(-1.87f,3.4f,10),new Vector3(-1.87f,3.4f,19),new Vector3(6.14f,3.4f,16)})
            {
                Box("Пятно сырости",pair+Vector3.up*.9f,new Vector3(.016f,1.7f,.6f),dark);
                for(int i=0;i<5;i++)Box("Обрывки обоев",pair+new Vector3(.015f,.4f+i*.22f,.15f),new Vector3(.015f,.18f,.48f-i*.04f),paper);
            }
            for(int i=0;i<7;i++)Box("Наружный кабель камер",new Vector3(-11.7f,2.7f,2+i*3.8f),new Vector3(.025f,.025f,3.8f),ink);
            for(int i=0;i<4;i++)Box("Кабель вдоль потолка коридора",new Vector3(-1.75f+i*.055f,2.95f,14),new Vector3(.022f,.022f,27.5f),rust);
            foreach(float y in new[]{0f,3.4f})
            {
                for(int i=0;i<6;i++){var f=LocalProp("Криво висящая семейная фотография",new Vector3(-1.87f,y+1.8f,7.6f+i*3),90);f.Rotate(0,0,i%2==0?6:-4);Box("Рамка",Vector3.zero,new Vector3(.42f,.54f,.07f),dark,false,f);Box("Выцветшая фотография",new Vector3(0,0,-.042f),new Vector3(.33f,.44f,.012f),paper,false,f);Box("Лицо на снимке зачёркнуто",new Vector3(0,.02f,-.052f),new Vector3(.13f,.14f,.01f),ink,false,f);}
            }
            for(int i=0;i<7;i++)Box("Заклеенная щель окна",new Vector3(-5,1.5f+i*.1f,.14f),new Vector3(.45f,.024f,.015f),paper);
            // A radio aerial and bundles of cables distinguish the roof line.
            Box("Антенна на крыше",new Vector3(8,9.2f,20),new Vector3(.06f,3,.06f),metal);
            for(int i=0;i<5;i++)Box("Ус антенны",new Vector3(8,9+i*.3f,20),new Vector3(2.7f-i*.4f,.04f,.04f),metal);
            Box("Водосток",new Vector3(-12.3f,3.2f,1),new Vector3(.12f,6.4f,.12f),rust);
            for(int i=0;i<18;i++){float x=(i%2==0?-1:1)*Rand(18,30),z=Rand(-9,42);Kit("tree",new Vector3(x,-.06f,z),Rand(0,360),Vector3.one*Rand(.8f,1.2f));}
            for(int i=0;i<24;i++){float x=Rand(-14,14),z=Rand(-6,-.5f);if(Mathf.Abs(x)<4.3f)continue;for(int k=0;k<3;k++)Box("Сухая трава у фундамента",new Vector3(x+k*.06f,.16f,z),new Vector3(.035f,Rand(.15f,.4f),.035f),dark).transform.Rotate(0,0,Rand(-20,20));}
            for(int i=0;i<5;i++){Box("Брошенная газета на крыльце",new Vector3(2.6f+i*.09f,.012f,-2.6f+i*.2f),new Vector3(.6f,.008f,.4f),paper).transform.Rotate(0,-20+i*8,0);}
            var doormat=Box("Затоптанный коврик",new Vector3(0,.012f,-.85f),new Vector3(1.4f,.018f,.7f),dark);Label("ВЫТРИ НОГИ. ЗАКРОЙ ДВЕРЬ.",new Vector3(0,2.72f,-.23f),.017f);
        }
        static float Rand(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
        static Transform LocalProp(string name,Vector3 p,float yaw=0){var t=Group(name,group);t.position=p;t.rotation=Quaternion.Euler(0,yaw,0);return t;}
        static void Papers(Vector3 p,int count=7,float radius=.5f)
        {
            for(int i=0;i<count;i++)
            {
                var sheet=Box("Брошенный лист",p+new Vector3(Rand(-radius,radius),.004f+i*.001f,Rand(-radius,radius)),new Vector3(.24f,.004f,.33f),paper);sheet.transform.Rotate(0,Rand(-75,75),0);
                for(int line=0;line<3;line++)Box("Машинописная строка",new Vector3(0,.56f,-.25f+line*.21f),new Vector3(.65f,.3f,.014f),ink,false,sheet.transform);
            }
        }
        static void Stack(Vector3 p,int count=5)
        {
            for(int i=0;i<count;i++){var a=Box("Папка с вырезками",p+Vector3.up*(i*.035f),new Vector3(.35f,.03f,.46f),i%3==0?cloth:paper);a.transform.Rotate(0,Rand(-10,10),0);}
        }
        static void Mess(float x0,float x1,float z0,float z1,float y,int count)
        {
            for(int i=0;i<count;i++)
            {
                var p=new Vector3(Rand(x0,x1),y+.008f,Rand(z0,z1));
                if(i%4==0){var o=Box("Комок бумаги",p+Vector3.up*.045f,new Vector3(.12f,.09f,.12f),paper);o.transform.Rotate(Rand(0,40),Rand(0,180),24);}
                else if(i%5==0)Cylinder("Пустая консервная банка",p+Vector3.up*.07f,new Vector3(.13f,.13f,.13f),metal);
                else Papers(p,1,.05f);
            }
            for(int i=0;i<4;i++){var stain=Box("Сырость и грязный след на полу",new Vector3(Rand(x0,x1),y+.003f,Rand(z0,z1)),new Vector3(Rand(.4f,1.3f),.002f,Rand(.3f,.7f)),dark);stain.transform.Rotate(0,Rand(0,180),0);}
        }
        static void Books(Vector3 p,int count,Transform parent=null)
        {
            for(int i=0;i<count;i++)Box("Потрёпанный корешок",p+Vector3.right*(i*.095f),new Vector3(.075f,Rand(.22f,.36f),.20f),i%4==0?cloth:i%3==0?paper:dark,false,parent??group);
        }
        static Transform Shelf(Vector3 p,float yaw=0,float width=1.7f)
        {
            var t=LocalProp("Стеллаж с личным архивом",p,yaw);
            Box("Задняя стенка",new Vector3(0,1.1f,.19f),new Vector3(width,2.2f,.06f),dark,true,t);
            foreach(float x in new[]{-width/2,width/2})Box("Боковина",new Vector3(x,1.1f,0),new Vector3(.09f,2.2f,.45f),wood,true,t);
            for(int k=0;k<5;k++){float y=.12f+k*.44f;Box("Полка",new Vector3(0,y,0),new Vector3(width,.06f,.45f),wood,true,t);Books(new Vector3(-width/2+.13f,y+.19f,0),Mathf.FloorToInt((width-.3f)/.095f),t);}
            return t;
        }
        static void Note(string name,string text,Vector3 p,float yaw=0)
        {
            var t=LocalProp("Записка · "+name,p,yaw);var note=t.gameObject.AddComponent<HouseDocument>();note.kind=DemoInteractable.Interaction.Custom;note.evidenceId=-1;note.heading=name;note.label="Прочитать: "+name;note.body=text;
            Box("Лист записки",Vector3.zero,new Vector3(.4f,.012f,.5f),paper,true,t);
            for(int i=0;i<7;i++)Box("Почерк",new Vector3(0,.008f,-.18f+i*.05f),new Vector3(i==6?.16f:.30f,.003f,.009f),ink,false,t);
        }
        static void Anchor(string id,string design,Vector3 p)
        {var t=LocalProp("Загадка · "+id,p);var a=t.gameObject.AddComponent<HousePuzzleAnchor>();a.puzzleId=id;a.design=design;}
        static Transform Secret(string name,Vector3 p,float yaw,string prompt,bool wardrobe=false)
        {
            var frame=LocalProp(name,p,yaw);var door=frame.gameObject.AddComponent<HouseSecretDoor>();door.kind=DemoInteractable.Interaction.Custom;door.evidenceId=-1;door.label=prompt;door.closedPrompt=prompt;
            var panel=Group("Подвижная панель",frame);door.panel=panel;door.openOffset=new Vector3(1.95f,0,0);
            Box("Несущая панель",new Vector3(0,1.12f,0),new Vector3(1.68f,2.24f,.32f),dark,true,panel);
            if(wardrobe)
            {
                foreach(float x in new[]{-.4f,.4f}){Box("Дверца шкафа",new Vector3(x,1.15f,-.2f),new Vector3(.76f,2.1f,.06f),wood,false,panel);Box("Ручка шкафа",new Vector3(x*.2f,1.13f,-.25f),new Vector3(.035f,.15f,.05f),metal,false,panel);}
            }
            else for(int k=0;k<5;k++){float y=.12f+k*.43f;Box("Полка",new Vector3(0,y,-.22f),new Vector3(1.7f,.06f,.28f),wood,false,panel);Books(new Vector3(-.7f,y+.18f,-.23f),15,panel);}
            return frame;
        }
        static void Board(Vector3 p,float yaw,string title,int documents=14)
        {
            var b=LocalProp("Доска · "+title,p,yaw);Box("Пробка",new Vector3(0,1.7f,0),new Vector3(2.65f,1.65f,.07f),wood,false,b);
            foreach(float x in new[]{-1.36f,1.36f})Box("Рамка",new Vector3(x,1.7f,0),new Vector3(.08f,1.8f,.12f),dark,false,b);
            var spots=new List<Vector3>();
            for(int i=0;i<documents;i++)
            {
                var at=new Vector3(Rand(-1.1f,1.1f),Rand(1.12f,2.32f),-.055f);spots.Add(at);var page=Box("Вырезка и фотография",at,new Vector3(Rand(.23f,.4f),Rand(.28f,.43f),.012f),i%4==0?glass:paper,false,b);page.transform.Rotate(0,0,Rand(-13,13));
                Box("Булавка",at+new Vector3(0,.12f,-.02f),Vector3.one*.025f,red,false,b);
            }
            for(int i=1;i<spots.Count;i++){var delta=spots[i]-spots[i-1];var thread=Box("Красная нить",(spots[i]+spots[i-1])/2+Vector3.back*.03f,new Vector3(.008f,.008f,delta.magnitude),red,false,b);thread.transform.localRotation=Quaternion.LookRotation(delta);}
            Label(title,new Vector3(0,2.75f,-.10f),.025f,0,b);
        }
        static void Desk(Vector3 p,float yaw,bool lamp=true)
        {
            Kit("table",p,yaw);var rotation=Quaternion.Euler(0,yaw,0);Func<Vector3,Vector3> at=v=>p+rotation*v;
            Kit("chair",at(new Vector3(0,0,.95f)),yaw);Papers(at(new Vector3(.1f,.91f,0)),7,.32f);Stack(at(new Vector3(-.55f,.92f,.15f)));
            if(lamp)MenuProp("Настольная лампа",at(new Vector3(.6f,.9f,.25f)),yaw,.7f);
            MenuProp("Кружка POLICE",at(new Vector3(-.50f,.91f,-.23f)),yaw,.65f);
        }
        static void Bed(Vector3 p,float yaw,bool doubleBed=false)
        {
            var t=LocalProp("Несобранная кровать",p,yaw);float w=doubleBed?1.8f:1.05f;
            Box("Каркас",new Vector3(0,.24f,0),new Vector3(w,.4f,2.3f),dark,true,t);Box("Продавленный матрас",new Vector3(0,.49f,0),new Vector3(w-.07f,.18f,2.17f),ivory,true,t);
            Box("Изголовье",new Vector3(0,.65f,1.12f),new Vector3(w+.1f,1.1f,.12f),wood,true,t);
            Box("Сбитое одеяло",new Vector3(.1f,.62f,-.35f),new Vector3(w*.86f,.12f,1.15f),cloth,false,t).transform.Rotate(0,12,0);
            Box("Подушка",new Vector3(0,.65f,.78f),new Vector3(w*.65f,.14f,.44f),paper,false,t);
            for(int i=0;i<6;i++)Box("Складка одеяла",new Vector3(.1f,.7f,-.85f+i*.18f),new Vector3(w*.82f,.018f,.027f),dark,false,t);
        }
        static void CameraProp(Vector3 p,float yaw)
        {
            var t=LocalProp("Самодельная камера наблюдения",p,yaw);Box("Кронштейн",new Vector3(0,0,.25f),new Vector3(.08f,.15f,.5f),rust,false,t);
            var pivot=Group("Поворотная головка",t);Box("Кожух камеры",Vector3.zero,new Vector3(.22f,.18f,.45f),ivory,false,pivot);Box("Объектив",new Vector3(0,0,-.25f),new Vector3(.09f,.09f,.08f),ink,false,pivot);Box("Индикатор записи",new Vector3(.08f,.06f,-.25f),Vector3.one*.018f,red,false,pivot);cameras.Add(pivot);
        }
        static void Exterior()
        {
            group=Group("07 · Глушь · лес и заросший двор");
            Slab("Земля · юг",-90,90,-90,0,-.06f,soil,.3f);Slab("Земля · север",-90,90,28,100,-.06f,soil,.3f);Slab("Земля · запад",-90,-12,0,28,-.06f,soil,.3f);Slab("Земля · восток",12,90,0,28,-.06f,soil,.3f);
            Box("Размытая лесная дорога",new Vector3(0,-.045f,-43),new Vector3(5,.02f,78),concrete);
            for(int i=0;i<105;i++)
            {
                float x=Rand(-67,67),z=Rand(-65,90);if((Mathf.Abs(x)<17&&z>-24&&z<35)||(Mathf.Abs(x)<4&&z<0)){i--;continue;}
                Kit("tree",new Vector3(x,-.06f,z),Rand(0,360),Vector3.one*Rand(.8f,1.65f));
            }
            Kit("car",new Vector3(-4.7f,.02f,-14),10); // Existing car keeps the native travel interaction.
            for(int i=0;i<30;i++)
            {
                float x=Rand(-27,27),z=Rand(-18,42);if(Mathf.Abs(x)<13&&z>-6&&z<29)continue;
                var log=Box("Поваленная ветка",new Vector3(x,.05f,z),new Vector3(Rand(.4f,1.7f),.09f,.10f),dark);log.transform.Rotate(0,Rand(0,360),0);
            }
            foreach(float x in new[]{-16f,16f})for(int z=-8;z<37;z+=4)Kit("fence",new Vector3(x,0,z),90);
            for(int x=-14;x<=14;x+=4){Kit("fence",new Vector3(x,0,36),0);if(Mathf.Abs(x)>5)Kit("fence",new Vector3(x,0,-8),0);}
            Box("Стойка предупреждения",new Vector3(5,1,-8),new Vector3(.12f,2,.12f),wood,true);Box("Табличка ЧАСТНАЯ ТЕРРИТОРИЯ",new Vector3(5,1.9f,-8),new Vector3(2.2f,.65f,.06f),dark);
            Label("ЧАСТНАЯ ТЕРРИТОРИЯ\nНЕ ПРИБЛИЖАТЬСЯ",new Vector3(5,1.9f,-8.05f),.021f);
            for(int i=0;i<3;i++)Box("Усиленный засов двери",new Vector3(.20f,.55f+i*.5f,-.16f),new Vector3(.65f,.055f,.06f),metal);
            // Bolts are attached to the door leaf so that they cannot obstruct the opening visually.
            var entrance=location.architecture.GetComponentsInChildren<DemoDoor>().First(d=>d.label=="Входная дверь");
            foreach(var bolt in group.GetComponentsInChildren<Transform>().Where(t=>t.name=="Усиленный засов двери").ToArray())bolt.SetParent(entrance.transform,true);
            CameraProp(new Vector3(-2,2.65f,-.4f),-20);CameraProp(new Vector3(11.6f,5.8f,27.5f),130);CameraProp(new Vector3(-11.6f,5.8f,1),-70);
            Point("Фонарь над крыльцом",new Vector3(.2f,2.8f,-1.2f),1.5f,8);Cylinder("Светильник крыльца",new Vector3(.2f,2.8f,-1.2f),new Vector3(.25f,.12f,.25f),ivory);
            for(int i=0;i<8;i++)Kit("crate",new Vector3(-3+i%2*.8f,.02f,-1-i/2*.65f),i%2*12);
            Box("Дровница",new Vector3(13.5f,.85f,23),new Vector3(2,.15f,5),dark,true);for(int i=0;i<25;i++)Cylinder("Сырые дрова",new Vector3(13.1f+i%3*.4f,.15f+i/12*.28f,21.3f+i%4*.85f),new Vector3(.22f,.8f,.22f),wood).transform.Rotate(90,0,0);
            Slab("Задний балкон",-3,3,28,31,3.4f,wood);Rail(new Vector3(-3,3.4f,28),new Vector3(-3,3.4f,31));Rail(new Vector3(3,3.4f,28),new Vector3(3,3.4f,31));Rail(new Vector3(-3,3.4f,31),new Vector3(3,3.4f,31));
            var shed=LocalProp("Сарай с глухими дверями",new Vector3(-24,0,24),8);Box("Корпус сарая",new Vector3(0,1.5f,0),new Vector3(5,3,5),siding,true,shed);Box("Крыша сарая",new Vector3(0,3.1f,0),new Vector3(5.5f,.22f,5.5f),dark,false,shed);Box("Заколоченная дверь",new Vector3(0,1.2f,-2.55f),new Vector3(1.6f,2.4f,.08f),wood,false,shed);
        }
        static void LivingAndDining()
        {
            group=Group("08 · Гостиная · жизнь среди бумаг");
            Kit("sofa",new Vector3(-9.7f,0,6),90);Kit("rug",new Vector3(-7,.012f,5),0,new Vector3(1.35f,1,1.6f));
            Desk(new Vector3(-7,0,6.8f),180);Kit("crt",new Vector3(-10.3f,.8f,2.2f),0);Box("Тумба под телевизор",new Vector3(-10.3f,.4f,2.2f),new Vector3(1.6f,.8f,.85f),dark,true);
            var fire=LocalProp("Неиспользуемый камин",new Vector3(-11.65f,0,8.5f),90);Box("Камин",new Vector3(0,.65f,0),new Vector3(2.1f,1.3f,.6f),concrete,true,fire);Box("Зола",new Vector3(0,.3f,-.32f),new Vector3(1.45f,.4f,.04f),ink,false,fire);Box("Каминная полка",new Vector3(0,1.34f,0),new Vector3(2.4f,.15f,.9f),wood,true,fire);
            MenuProp("Настенные часы",new Vector3(-7,2.45f,.2f),180,.85f);MenuProp("Пепельница с тлеющей сигарой",new Vector3(-6.7f,.93f,6.6f),180,.8f);
            Shelf(new Vector3(-4.2f,0,9.5f),0,2);Kit("clue_0",new Vector3(-7.4f,.94f,6.7f),15);
            Note("Не открывать окна","Камеры не видят полосу под яблонями.\n\nНе открывать окна после девяти. Не отвечать, если назовут меня по имени с улицы.\n\nСегодня снова сдвинут стул. Я оставлял его спинкой к двери.",new Vector3(-6.7f,.94f,6.8f));
            Mess(-11,-3,1,9,0,45);Point("Свет гостиной",new Vector3(-7,2.7f,5),2.0f,9);
            group=Group("09 · Прихожая и столовая");
            Kit("rug",new Vector3(0,.01f,3),0,new Vector3(.7f,1,1.5f));Kit("cabinet",new Vector3(-1.2f,0,1.4f),90);
            for(int i=0;i<5;i++){Box("Крючок",new Vector3(-1.82f,1.75f,2+i*.3f),new Vector3(.11f,.07f,.05f),metal);if(i%2==0){Box("Старое пальто",new Vector3(-1.69f,1.25f,2+i*.3f),new Vector3(.22f,.85f,.32f),dark);}}
            Label("КЛЮЧИ НЕ ОСТАВЛЯТЬ У ДВЕРИ",new Vector3(0,2.7f,.14f),.019f,180);
            Point("Прихожая",new Vector3(0,2.7f,3),1.5f,7);Point("Дальний коридор",new Vector3(0,2.7f,15),1.6f,9);Point("Задний коридор",new Vector3(0,2.7f,25),1.4f,8);
            Desk(new Vector3(9,0,5),90);Kit("chair",new Vector3(9,0,6.5f),8);Kit("chair",new Vector3(8.6f,0,3.7f),35);Kit("cabinet",new Vector3(11.1f,0,8),-90);
            for(int i=0;i<7;i++){Cylinder("Грязная тарелка",new Vector3(9.1f,.94f+i*.013f,5),new Vector3(.34f,.013f,.34f),ivory);}
            for(int i=0;i<5;i++)Cylinder("Бутылка на столе",new Vector3(8.6f+i*.13f,1.03f,5.2f),new Vector3(.07f,.24f,.07f),glass);
            Board(new Vector3(9,0,9.82f),0,"НЕ САДИТЬСЯ СПИНОЙ К ОКНУ",8);Mess(6.6f,11.4f,1,9,0,23);Point("Люстра столовой",new Vector3(9,2.7f,5),2,8);
        }
        static void StudyAndHidden()
        {
            group=Group("10 · Кабинет · навязчивое расследование");
            Desk(new Vector3(-5.5f,0,12.5f),180);MenuProp("Пишущая машинка",new Vector3(-5.6f,.93f,12.55f),180,.75f);Kit("phone",new Vector3(-5,.94f,12.7f));
            Kit("clue_1",new Vector3(-5.8f,.95f,12.2f),0);Note("Если я не вернусь","Сыновья, простите меня. Я слишком поздно понял, что бумаги из участка важнее любого оружия.\n\nОригиналы не должны оказаться у тех, кто назвал это самоубийством. Не доверяйте аккуратным копиям. Ищите то, что не совпадает.\n\nКнигу с потёртым красным корешком я больше не переставляю.",new Vector3(-5.1f,.96f,12.2f));
            Board(new Vector3(-5,0,10.2f),180,"ОДИН И ТОТ ЖЕ СЕДАН / ТРЕТЬЮ НОЧЬ");
            for(int i=0;i<4;i++)Kit("cabinet",(i==2?new Vector3(-7.1f,0,10.65f):new Vector3(-3.0f,0,11+i*1.6f)),i==2?180:-90);Shelf(new Vector3(-6.8f,0,18.5f),0);
            Secret("Тайник · комната наблюдения",new Vector3(-8,0,15),-90,"Потянуть книгу с красным корешком");
            Anchor("study_safe","Сейф в кабинете. Будущая комбинация связана со временем на часах и датами писем.",new Vector3(-3.1f,.7f,17.6f));
            var safe=LocalProp("Сейф с четырьмя дисками",new Vector3(-3.1f,0,17.6f),-90);Box("Корпус сейфа",new Vector3(0,.55f,0),new Vector3(.9f,1.1f,.8f),metal,true,safe);for(int i=0;i<4;i++)Box("Цифровой диск",new Vector3(-.27f+i*.18f,.67f,-.42f),new Vector3(.10f,.15f,.04f),ink,false,safe);Label("НЕ ОСТАВЛЯТЬ ОТКРЫТЫМ",new Vector3(0,.94f,-.43f),.009f,0,safe);
            Mess(-7.5f,-2.8f,10.7f,18.2f,0,42);Point("Кабинет отца",new Vector3(-5,2.7f,14),2,8);
            group=Group("11 · Тайная комната наблюдения");
            for(int i=0;i<3;i++)
            {
                Kit("table",new Vector3(-10.5f,0,11.4f+i*2.8f),90);
                for(int j=0;j<2;j++){var p=new Vector3(-10.8f,.92f+j*.65f,11.4f+i*2.8f);Kit("crt",p,90,new Vector3(.55f,.7f,.65f));Box("Зелёный экран наблюдения",p+new Vector3(.38f,.24f,0),new Vector3(.012f,.28f,.42f),screen);}
            }
            Kit("chair",new Vector3(-9.2f,0,13),90);for(int i=0;i<12;i++){Box("Видеокассета с датой",new Vector3(-11.3f+(i%3)*.24f,.1f+(i/3)*.08f,18.2f),new Vector3(.22f,.06f,.34f),ink);}
            Board(new Vector3(-10,0,10.2f),180,"ДВОР / ЛЕС / ДОРОГА / ЧЁРНЫЙ ХОД",6);
            Note("Журнал наблюдения","02:14 — свет у сарая.\n02:17 — камера 3 потеряла сигнал.\n02:18 — шаги НАВЕРХУ, хотя я один.\n02:21 — машина у развилки. Номер тот же.\n\nНе удалять запись. Питание камер и свет в доме разведены отдельно.",new Vector3(-10.3f,.96f,11.4f));
            Anchor("surveillance_power","Резервное питание камер; место под загадку с маршрутом кабелей и кассетами.",new Vector3(-10.9f,.6f,17.2f));
            for(int i=0;i<4;i++){Box("Аккумулятор",new Vector3(-11.3f,.35f,16.7f+i*.4f),new Vector3(.45f,.65f,.32f),ink,true);Box("Кабель",new Vector3(-11.6f,1.5f,12+i*1.6f),new Vector3(.025f,2.7f,.025f),rust);}
            Point("Экраны скрытой комнаты",new Vector3(-10.6f,1.8f,14),1.2f,6,new Color(.36f,.64f,.47f));Mess(-11.5f,-8.6f,10.5f,18.5f,0,18);
        }
        static void KitchenAndWorkshop()
        {
            group=Group("12 · Кухня и кладовая · запасы на осаду");
            Kit("fridge",new Vector3(10.9f,0,16.5f),180);
            for(int i=0;i<4;i++){Box("Кухонная тумба",new Vector3(11.35f,.43f,11.3f+i*1.12f),new Vector3(1,.86f,1.05f),wood,true);Box("Грязная столешница",new Vector3(11.3f,.90f,11.3f+i*1.12f),new Vector3(1.15f,.10f,1.1f),ivory,true);}
            Box("Раковина",new Vector3(11.25f,.96f,13.5f),new Vector3(.72f,.04f,.70f),metal);Box("Грязная вода",new Vector3(11.25f,.99f,13.5f),new Vector3(.50f,.012f,.48f),glass);
            Cylinder("Кран",new Vector3(11.65f,1.2f,13.5f),new Vector3(.045f,.5f,.045f),metal);
            for(int i=0;i<4;i++)Cylinder("Конфорка",new Vector3(11.05f+i%2*.45f,.98f,11.1f+i/2*.4f),new Vector3(.28f,.025f,.28f),ink);
            Desk(new Vector3(8,0,16.4f),0,false);Note("Порядок на случай осады","Вода — 18 канистр.\nКонсервы — 46 банок.\nЛекарства — в верхней ванной.\nНе включать плиту одновременно с обогревателем.\n\nЕсли погаснет свет — сначала проверить подвал. НЕ ВЫХОДИТЬ НА УЛИЦУ.",new Vector3(8,.96f,16.3f));
            for(int i=0;i<5;i++)Box("Навесной шкаф",new Vector3(11.7f,2.12f,11.1f+i*1.1f),new Vector3(.5f,.85f,1.02f),dark,true);
            Mess(6.7f,10.5f,11,17.1f,0,28);Point("Кухонная лампа",new Vector3(8.8f,2.75f,14.5f),2.1f,8);
            for(int row=0;row<3;row++)
            {
                var shelf=Shelf(new Vector3(10.8f,0,20+row*2.7f),-90,2);
                foreach(var book in shelf.GetComponentsInChildren<Transform>().Where(t=>t.name=="Потрёпанный корешок").ToArray())UnityEngine.Object.DestroyImmediate(book.gameObject);
                for(int k=0;k<4;k++)for(int j=0;j<6;j++)Cylinder("Запас консервов",new Vector3(-.73f+j*.28f,.29f+k*.44f,0),new Vector3(.2f,.25f,.2f),j%2==0?ivory:rust,shelf);
            }
            for(int i=0;i<8;i++)Box("Канистра воды",new Vector3(6+(i%2)*.8f,.32f,19+(i/2)*1.4f),new Vector3(.5f,.64f,.4f),ivory,true);
            Anchor("pantry_weight","Ряд банок с несоответствующими этикетками: место для будущей загадки с весами.",new Vector3(9,.9f,25));Point("Кладовая",new Vector3(8,2.7f,24),1.3f,7);
            group=Group("13 · Мастерская · разобранные приборы");
            Desk(new Vector3(-7,0,25.8f),0);Box("Верстак",new Vector3(-10.8f,.82f,23),new Vector3(1.2f,.14f,5.5f),wood,true);
            for(int i=0;i<16;i++){Box("Деталь разобранного приёмника",new Vector3(-10.8f+Rand(-.35f,.35f),.93f,20.8f+i*.26f),new Vector3(.18f,Rand(.05f,.2f),.17f),i%2==0?metal:ink);}
            Board(new Vector3(-7,0,27.8f),0,"КАБЕЛИ НЕ ПЕРЕРЕЗАТЬ",9);
            for(int i=0;i<6;i++)Kit("crate",new Vector3(-6f+i%2*.8f,0,20.8f+i/2*1.1f),Rand(-6,6));
            Note("Неисправности","Радио ловит голоса только после полуночи.\nСетевая частота — помеха, не сообщение.\n\nПовторить проверку экранировки. Перенести резервный блок питания вниз.\n\nЗа стеной спальни снова слышно движение. Проверить отметки на досках.",new Vector3(-7,.96f,25.7f));
            Anchor("radio_frequencies","Приёмник и журнал частот; будущая настройка сигнала.",new Vector3(-10.7f,1,24));Kit("crt",new Vector3(-10.8f,.93f,24),90,new Vector3(.6f,.6f,.6f));
            Mess(-11.3f,-2.7f,19.6f,27,0,37);Point("Мастерская",new Vector3(-7,2.7f,23),2,8);
        }
        static void BedroomsAndLibrary()
        {
            group=Group("14 · Спальня отца и ход за стенами");
            Bed(new Vector3(-5.8f,3.4f,8.5f),0,true);Kit("cabinet",new Vector3(-3.1f,3.4f,8.3f),-90);Desk(new Vector3(-5.7f,3.4f,2.2f),180);
            Secret("Тайник · шкаф в спальне",new Vector3(-9,3.4f,5),-90,"Отодвинуть тяжёлый шкаф",true);
            Note("Ночная запись","Я не спал третью ночь. В гостевой кто-то передвинул чемодан.\n\nНельзя оставлять свет в библиотеке: из леса видно силуэт.\n\nПроход за шкафом свободен. Доска у северной стены скрипит — наступать ближе к краю.",new Vector3(-5.7f,4.36f,2.2f));
            for(int i=0;i<10;i++){var c=Box("Брошенная одежда",new Vector3(Rand(-8.3f,-3),3.43f,Rand(1,9.8f)),new Vector3(.5f,.07f,.6f),i%2==0?cloth:dark);c.transform.Rotate(0,Rand(0,180),0);}
            Board(new Vector3(-5,3.4f,10.8f),0,"НЕ ЗАСЫПАТЬ ПРИ ОТКРЫТОЙ ДВЕРИ",7);Mess(-8.4f,-2.8f,1,10,3.4f,28);Point("Спальня отца",new Vector3(-5.5f,6.1f,6),1.6f,8);
            group=Group("15 · Скрытая галерея за стенами второго этажа");
            for(int i=0;i<5;i++)Box("Панель прослушивания комнат",new Vector3(-9.15f,4.8f,2+i*3.5f),new Vector3(.07f,.3f,.5f),metal);
            for(int i=0;i<5;i++)Box("Провод за стеной",new Vector3(-9.3f+i*.05f,5.9f,10),new Vector3(.02f,.02f,18.7f),rust);
            for(int i=0;i<3;i++){Stack(new Vector3(-11.1f,3.43f,8+i*3));Kit("crate",new Vector3(-11.1f,3.4f,15+i*1.2f),12);}
            Note("За стеной","Вентиляционные решётки дают слышать оба коридора.\n\nНе оставлять здесь фонарь. Свет проходит сквозь щель у шкафа.\n\nЕсли дверь кабинета уже открыта — вернуться через семейный архив.",new Vector3(-10.5f,3.46f,7));
            Secret("Тайник · выход из скрытой галереи",new Vector3(-10.5f,3.4f,20),0,"Отодвинуть архивную панель");
            Point("Лампа за стеной",new Vector3(-10.6f,5.8f,11),.75f,9,new Color(.8f,.45f,.18f));
            group=Group("16 · Библиотека и семейный архив");
            for(int i=0;i<3;i++)Shelf(new Vector3(-8.5f,3.4f,12.5f+i*2.7f),90,2.1f);
            Shelf(new Vector3(-5.6f,3.4f,11.5f),180,2.4f);Desk(new Vector3(-5.3f,3.4f,16.8f),180);Kit("clue_2",new Vector3(-5.5f,4.36f,16.6f),0);
            MenuProp("Настенные часы",new Vector3(-5,5.9f,11.2f),180,.7f);Anchor("library_clock","Часы и книжные даты: место под будущую последовательность книг.",new Vector3(-5,5.9f,11.2f));
            Note("Список книг","История округа — полка II.\nТелефонные справочники — полка IV.\nСтарые дела — в семейном архиве.\n\nКрасную книгу не уносить из кабинета. Корешок повреждён намеренно.",new Vector3(-5,4.36f,16.8f));
            Mess(-8.4f,-2.7f,11.7f,19.2f,3.4f,27);Point("Библиотека",new Vector3(-5.5f,6.1f,15.7f),1.9f,8);
            for(int i=0;i<4;i++)Shelf(new Vector3(-11.4f,3.4f,22+i*1.5f),90,1.3f);
            for(int i=0;i<12;i++){var p=new Vector3(-7.6f+(i%3)*1.2f,3.4f+(i/6)*.65f,22.4f+(i/3%2)*1.5f);Kit("crate",p,Rand(-8,8));Stack(p+Vector3.up*.68f,4);}
            Board(new Vector3(-6,3.4f,27.8f),0,"СЕМЬЯ / ОРИГИНАЛЫ НЕ ВЫДАВАТЬ",10);
            Anchor("family_archive","Коробки с семейными датами и оригиналами документов; хронологическая загадка.",new Vector3(-6,4.2f,25));
            Note("Оригиналы","Никому не отдавать оригиналы.\n\nВ папке за октябрь изменена дата, но чернила на копии свежие. Я помню другую подпись.\n\nФотографии оставить сыновьям. Всё остальное — только после сверки с архивом участка.",new Vector3(-4,3.46f,26));
            Mess(-9,-3,21,27,3.4f,24);Point("Семейный архив",new Vector3(-6,6.1f,24),1.5f,9);
            group=Group("17 · Гостевая, ванная и детская");
            Bed(new Vector3(10,3.4f,8.6f),0);Kit("cabinet",new Vector3(11,3.4f,3),-90);Kit("crate",new Vector3(8,3.4f,8),18);Desk(new Vector3(8.6f,3.4f,3),180);
            Note("Для гостя","Постель чистая. Не двигай чемодан.\n\nЕсли услышишь стук в окно, не отвечай. В этой комнате окна не открываются.",new Vector3(8.6f,4.36f,3));Mess(6.8f,11.2f,1.2f,9.8f,3.4f,18);Point("Гостевая",new Vector3(9,6.1f,6),1.6f,7);
            Box("Эмалированная ванна",new Vector3(11,3.72f,14),new Vector3(1.4f,.64f,2.1f),ivory,true);Box("Стоячая вода в ванне",new Vector3(11,4.05f,14),new Vector3(1.1f,.018f,1.8f),glass);
            Box("Тумба раковины",new Vector3(8.2f,3.8f,16.4f),new Vector3(1.25f,.8f,.6f),ivory,true);Box("Зеркало заклеено бумагой",new Vector3(8.2f,5.1f,16.82f),new Vector3(1.3f,1.1f,.03f),paper);
            for(int i=0;i<9;i++)Cylinder("Флакон с лекарством",new Vector3(7.7f+(i%5)*.2f,4.3f,16.3f-(i/5)*.2f),new Vector3(.08f,.2f,.08f),i%2==0?glass:ivory);
            Note("Зеркало","Убрать все отражающие поверхности из спальни.\n\nЛекарства после еды. Врач говорит, что это от недосыпа. Врач не видел записи.",new Vector3(8.2f,4.24f,16.3f));Point("Ванная",new Vector3(8.7f,6.1f,14),1.3f,6,new Color(.58f,.70f,.62f));
            Bed(new Vector3(10.6f,3.4f,22.5f),0);Bed(new Vector3(7.5f,3.4f,22.5f),0);Desk(new Vector3(10.4f,3.4f,18.3f),180,false);
            for(int i=0;i<7;i++){var toy=Box("Детский кубик",new Vector3(8+Rand(-.7f,.7f),3.49f,19+Rand(-.5f,.5f)),Vector3.one*.16f,i%2==0?cloth:ivory);toy.transform.Rotate(0,Rand(0,180),0);}
            Box("Выцветший рисунок на стене",new Vector3(9,5.2f,23.86f),new Vector3(.6f,.42f,.015f),paper);Label("ПАПА И МЫ",new Vector3(9,5.2f,23.84f),.014f);
            Anchor("childhood_figures","Четыре вырезанные фигурки и детский рисунок; место для будущей загадки на порядок.",new Vector3(10.4f,4.3f,18.3f));for(int i=0;i<4;i++)Box("Деревянная фигурка",new Vector3(10+i*.22f,4.47f,18.3f),new Vector3(.1f,.3f,.1f),wood);
            Mess(6.8f,11.3f,17.8f,23,3.4f,18);Point("Старая детская",new Vector3(9,6.1f,20.5f),1.3f,7);
            group=Group("18 · Комната карт и верхние коридоры");
            Board(new Vector3(9,3.4f,27.8f),0,"ЛЕС / СТАРАЯ ДОРОГА / КОЛОДЕЦ",18);Desk(new Vector3(10.2f,3.4f,25.2f),0);Note("Карта обходов","Обычную дорогу видно с развилки.\nСеверная тропа ведёт к старому колодцу.\nСарай проверять только днём.\n\nТри отметки на карте не совпадают с планом округа. Кто-то стёр старое строение.",new Vector3(10.2f,4.36f,25.1f));
            Anchor("forest_map","Три несовпадающих ориентира на карте: задел для дальнейшего исследования леса.",new Vector3(9,5.1f,27.7f));Point("Комната карт",new Vector3(9,6.1f,26),1.5f,6);
            foreach(float z in new[]{3f,14f,25f}){Point("Свет верхнего коридора",new Vector3(0,6.1f,z),1.4f,8);}
            Point("Лестничный свет",new Vector3(4,6.15f,10),1.5f,8);Point("Восточная галерея",new Vector3(4,6.1f,21),1.2f,10);
            for(int i=0;i<5;i++){Box("Настенная рамка",new Vector3(1.87f,5.15f,12+i*3),new Vector3(.08f,.6f,.45f),dark);Box("Фото заклеено",new Vector3(1.82f,5.15f,12+i*3),new Vector3(.018f,.48f,.34f),paper);}
        }
        static void Basement()
        {
            group=Group("19 · Подвал · щитовая и запертые запасы");
            Point("Лампа котельной",new Vector3(10,-.8f,19.5f),.85f,6);Point("Подвальная лампа",new Vector3(6,-.8f,25),1.2f,8);
            var fuse=LocalProp("Электрический щит",new Vector3(7.82f,-3.4f,21.1f),90);Box("Шкаф автоматики",new Vector3(0,1.5f,0),new Vector3(.65f,.9f,.14f),metal,true,fuse);for(int i=0;i<6;i++)Box("Плавкий предохранитель",new Vector3(-.2f+(i%3)*.2f,1.3f+(i/3)*.32f,-.1f),new Vector3(.08f,.18f,.08f),ivory,false,fuse);
            Label("ДОМ   СВЕТ   КАМЕРЫ",new Vector3(0,2.08f,-.12f),.011f,0,fuse);Anchor("basement_fuses","Три цепи питания и сменные предохранители; место для полноценной электрической загадки.",new Vector3(7.7f,-1.8f,21.1f));
            for(int i=0;i<5;i++)Box("Труба вдоль потолка подвала",new Vector3(5+i*.6f,-.37f,22.5f),new Vector3(.08f,.08f,10),rust);
            Cylinder("Старый бойлер",new Vector3(10.5f,-2.25f,18.5f),new Vector3(1.1f,2.3f,1.1f),rust);Box("Бойлер · коллизия",new Vector3(10.5f,-2.25f,18.5f),new Vector3(1,2.3f,1),rust,true);Cylinder("Манометр",new Vector3(10.5f,-1.5f,17.9f),new Vector3(.2f,.04f,.2f),ivory).transform.Rotate(90,0,0);
            Shelf(new Vector3(6.8f,-3.4f,27.4f),0);for(int i=0;i<4;i++)Kit("crate",new Vector3(5.3f+(i%2)*.9f,-3.4f,18.5f+(i/2)*1.2f),8);
            Secret("Тайник · комната за подвальным стеллажом",new Vector3(8,-3.4f,25),90,"Сдвинуть пыльный стеллаж");
            Note("Питание","Белая линия — дом.\nКрасная линия — наружные камеры.\nЧёрная линия — резерв.\n\nПредохранители не взаимозаменяемы. Я подписал их, но кто-то меняет порядок.",new Vector3(6.1f,-3.34f,22));
            Mess(4.8f,7.5f,21,27,-3.4f,23);
            group=Group("20 · Скрытое подвальное хранилище");
            Desk(new Vector3(10.3f,-3.4f,26.5f),0);Board(new Vector3(10.1f,-3.4f,27.8f),0,"ЕСЛИ СО МНОЙ ЧТО-ТО СЛУЧИТСЯ",10);
            for(int i=0;i<6;i++){Box("Запечатанный архивный ящик",new Vector3(10.8f+(i%2)*.45f,-3.1f+(i/4)*.6f,22.8f+(i/2%2)*.7f),new Vector3(.4f,.6f,.6f),dark,true);}
            Note("Последний конверт","Здесь нет оружия. Здесь то, ради чего за мной следят.\n\nЯ оставил слишком много версий одного события. Сверьте дату, оружие и заключение эксперта. Бумага не умеет помнить, но сохраняет чужую ошибку.\n\nНе верьте первой версии.",new Vector3(10.3f,-2.44f,26.4f));
            Point("Тайное хранилище",new Vector3(10,-.9f,25),1.0f,6,new Color(.8f,.57f,.3f));
        }
        static void WeatherCollider(string name,Vector3 p,Vector3 size)
        {var t=Group(name,location.architecture);t.position=p;t.gameObject.layer=2;var c=t.gameObject.AddComponent<BoxCollider>();c.size=size;}
        static void Shelter(Vector3 p,Vector3 size)
        {var t=Group("Укрытие от дождя",location.gameplay);t.position=p;var c=t.gameObject.AddComponent<BoxCollider>();t.gameObject.AddComponent<LocationShelter>();c.isTrigger=true;c.center=Vector3.zero;c.size=size;}
        static void Environment()
        {
            LocationKitOperations.EnsureEnvironment(location);var e=location.GetComponent<LocationEnvironment>();
            e.fog=true;e.fogColor=new Color(.105f,.135f,.13f);e.fogStart=14;e.fogEnd=90;e.ambientColor=new Color(.28f,.30f,.25f);e.mainLightColor=new Color(.51f,.65f,.67f);e.mainLightIntensity=.35f;e.rain=true;e.rainArea=new Vector2(130,145);e.rainHeight=16;e.rainRate=1800;e.Apply();
            WeatherCollider("Крыша · защита от дождя",new Vector3(0,6.87f,14),new Vector3(25,.25f,29));WeatherCollider("Крыльцо · защита от дождя",new Vector3(0,3.4f,-2),new Vector3(8.5f,.22f,4.6f));
            WeatherCollider("Земля · дождь · юг",new Vector3(0,-.42f,-45),new Vector3(180,.2f,90));WeatherCollider("Земля · дождь · север",new Vector3(0,-.42f,64),new Vector3(180,.2f,72));WeatherCollider("Земля · дождь · запад",new Vector3(-51,-.42f,14),new Vector3(78,.2f,28));WeatherCollider("Земля · дождь · восток",new Vector3(51,-.42f,14),new Vector3(78,.2f,28));
            var collision=e.rainSystem.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;collision.collidesWith=1<<2;collision.enableDynamicColliders=false;collision.maxCollisionShapes=16;collision.quality=ParticleSystemCollisionQuality.High;collision.lifetimeLoss=1;
            Shelter(new Vector3(0,3.25f,14),new Vector3(23.8f,6.6f,27.8f));Shelter(new Vector3(7,-1.75f,22.5f),new Vector3(9.8f,3.4f,10.8f));Shelter(new Vector3(0,1.5f,-2),new Vector3(7.7f,3.2f,3.7f));
            location.game.hud.retroMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Menu/Art/MenuGrade.mat");
            EditorUtility.SetDirty(e);
        }
    }
}
