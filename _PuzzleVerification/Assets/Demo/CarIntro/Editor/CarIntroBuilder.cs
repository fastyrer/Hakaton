using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Hakaton.Demo.Trailer;
using Hakaton.Demo.FatherHouse;
using Hakaton.LocationKit;
using Hakaton.LocationKit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Hakaton.Demo.CarIntro.Editor
{
    public static class CarIntroBuilder
    {
        public const string ScenePath="Assets/Scenes/CarIntro.unity";
        const string Art="Assets/Demo/CarIntro/Art";
        static Transform root,group,car;
        static Font font;static Material black,cloth,steel,glow,glass,beam,road;
        static Scene houseSource,castSource;static CarIntroSequence sequence;static OpeningTrailer picture;
        [MenuItem("Hakaton/Интро в машине/Создать или открыть")]
        public static string Build()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Остановите Play и сохраните сцену.");
            if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return ScenePath;}
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            houseSource=EditorSceneManager.OpenPreviewScene("Assets/Scenes/FatherHouse.unity");castSource=EditorSceneManager.OpenPreviewScene("Assets/Scenes/OpeningTrailer.unity");
            try
            {
                var source=houseSource.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LocationRoot>(true)).Single();
                var house=UnityEngine.Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(house,SceneManager.GetActiveScene());house.name="ДЕКОРАЦИЯ · дом и лес";
                var location=house.GetComponent<LocationRoot>();location.game.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(location.game.gameObject);location.enabled=false;
                var environment=house.GetComponent<LocationEnvironment>();environment.enabled=false;
                foreach(var weather in house.GetComponentsInChildren<DemoWeather>(true))weather.enabled=false;
                foreach(var ambience in house.GetComponentsInChildren<HouseAmbience>(true))ambience.enabled=false;
                foreach(var piece in house.GetComponentsInChildren<LocationPiece>(true).Where(p=>p.kitId=="car").ToArray())UnityEngine.Object.DestroyImmediate(piece.gameObject);
                foreach(var t in house.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Размытая лесная дорога").ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
                font=location.game.hud.font??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                root=new GameObject("ИНТРО · дорога к дому").transform;group=root;
                picture=root.gameObject.AddComponent<OpeningTrailer>();picture.externalClock=true;picture.playOnStart=false;picture.nextScene=DemoGame.HomeScene;picture.musicVolume=.74f;picture.musicFadeOut=.8f;
                sequence=root.gameObject.AddComponent<CarIntroSequence>();sequence.picture=picture;
                Materials();RoadAndForest();BuildCar();Cameras();Shots();Hakaton.Demo.Vehicles.Editor.CrownVictoriaRefresh.Reframe(sequence);CarIntroEnding.Configure(sequence);
                picture.actors=Array.Empty<TrailerActor>();picture.particles=new[]{environment.rainSystem};picture.smokeParticles=Array.Empty<ParticleSystem>();
                sequence.rainRoot=environment.rainSystem.transform.parent;
                var rain=environment.rainSystem;var main=rain.main;main.playOnAwake=false;main.maxParticles=4000;main.startLifetime=1.7f;rain.useAutoRandomSeed=false;rain.randomSeed=71;
                var emission=rain.emission;emission.rateOverTime=1400;var shape=rain.shape;shape.scale=new Vector3(62,64,.1f);rain.transform.localPosition=new Vector3(0,18,0);
                var collision=rain.collision;collision.collidesWith=1<<2;collision.enableDynamicColliders=true;collision.maxCollisionShapes=24;collision.lifetimeLoss=1;
                var ground=new GameObject("Дождь · поверхность лесной дороги",typeof(BoxCollider));ground.layer=2;ground.transform.SetParent(root,false);ground.transform.position=new Vector3(0,-.34f,-100);ground.GetComponent<BoxCollider>().size=new Vector3(160,.2f,260);
                RenderSettings.skybox=null;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.065f,.105f,.12f);RenderSettings.fogStartDistance=13;RenderSettings.fogEndDistance=120;
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.30f,.35f,.36f);RenderSettings.ambientEquatorColor=new Color(.20f,.25f,.26f);RenderSettings.ambientGroundColor=new Color(.08f,.10f,.10f);
                environment.mainLight.color=new Color(.45f,.62f,.70f);environment.mainLight.intensity=.52f;
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);
                var scenes=EditorBuildSettings.scenes.ToList();if(!scenes.Any(s=>s.path==ScenePath))scenes.Insert(1,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();
                Selection.activeGameObject=root.gameObject;return ScenePath;
            }
            finally{EditorSceneManager.ClosePreviewScene(houseSource);EditorSceneManager.ClosePreviewScene(castSource);}
        }
        static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent??root,false);return t;}
        static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,Transform parent=null)
        {var b=GameObject.CreatePrimitive(PrimitiveType.Cube);b.name=name;b.transform.SetParent(parent??group,false);b.transform.localPosition=p;b.transform.localScale=size;b.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(b.GetComponent<Collider>());return b;}
        static Material Mat(string name,Color color,string shader="Universal Render Pipeline/Lit")
        {var m=new Material(Shader.Find(shader)){name=name};m.color=color;if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.25f);AssetDatabase.CreateAsset(m,Art+"/"+name+".mat");return m;}
        static void Materials()
        {
            black=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Menu/Art/Ink_and_rubber.mat");cloth=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Trailer/Art/Surveillance_coat.mat");steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Menu/Art/Office_steel.mat");
            glow=Mat("Dashboard_green",new Color(.16f,.45f,.29f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(.12f,.57f,.28f));
            glass=new Material(Shader.Find("Hakaton/Car Rain Glass")){name="Rain_on_windows"};AssetDatabase.CreateAsset(glass,Art+"/Rain_on_windows.mat");
            beam=new Material(Shader.Find("Hakaton/Car Headlight Mist")){name="Headlight_mist"};AssetDatabase.CreateAsset(beam,Art+"/Headlight_mist.mat");
            road=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Art/Arrival/Concrete.mat")){name="Wet_forest_road"};road.color=new Color(.055f,.07f,.075f);road.SetFloat("_Smoothness",.27f);road.SetFloat("_SpecularHighlights",0);road.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");road.mainTextureScale=new Vector2(2,3);AssetDatabase.CreateAsset(road,Art+"/Wet_forest_road.mat");
        }
        static void RoadAndForest()
        {
            group=Group("01 · Лесная дорога");var grass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/FatherHouse/Art/Forest_earth.mat");
            Box("Лесная земля",new Vector3(0,-.18f,-135),new Vector3(130,.25f,150),grass);
            for(int i=0;i<90;i++)
            {
                float u0=i/90f,u1=(i+1)/90f;var a=CarIntroSequence.Road(u0);var b=CarIntroSequence.Road(u1);var n=Vector3.Cross((b-a).normalized,Vector3.up)*3.2f;
                var mesh=new Mesh{name="Wet road strip "+i};mesh.vertices=new[]{a+n+Vector3.down*.035f,a-n+Vector3.down*.035f,b+n+Vector3.down*.035f,b-n+Vector3.down*.035f};mesh.uv=new[]{new Vector2(0,i*.8f),new Vector2(1,i*.8f),new Vector2(0,(i+1)*.8f),new Vector2(1,(i+1)*.8f)};mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,Art+"/Road_"+i.ToString("00")+".asset");
                var o=new GameObject("Мокрая колея",typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(group,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<Renderer>().sharedMaterial=road;
            }
            var random=new System.Random(431);var prefab=KitLibraryBuilder.Load().Find("tree").prefab;
            for(int i=0;i<170;i++)
            {
                float u=(float)random.NextDouble();var p=CarIntroSequence.Road(u);var side=CarIntroSequence.Heading(u)*Vector3.right;float distance=8+(float)random.NextDouble()*26;
                var tree=(GameObject)PrefabUtility.InstantiatePrefab(prefab);tree.transform.SetParent(group,false);tree.transform.position=p+side*distance*(i%2==0?1:-1);tree.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);tree.transform.localScale=Vector3.one*(.85f+(float)random.NextDouble()*.65f);PrefabUtility.RecordPrefabInstancePropertyModifications(tree.transform);
            }
        }
        static void Bar(string name,Vector3 a,Vector3 b,float width,Transform parent)
        {var delta=b-a;var o=Box(name,(a+b)/2,new Vector3(width,width,delta.magnitude),black,parent);o.transform.localRotation=Quaternion.LookRotation(delta);}
        static Renderer Window(string name,Vector3 position,Vector2 size,Quaternion rotation)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Quad);o.name=name;o.transform.SetParent(car,false);o.transform.localPosition=position;o.transform.localRotation=rotation;o.transform.localScale=new Vector3(size.x,size.y,1);o.GetComponent<Renderer>().sharedMaterial=glass;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o.GetComponent<Renderer>();}
        static void Label(string text,Vector3 p,float size,Transform parent,Quaternion rotation)
        {var t=Group(text,parent);t.localPosition=p;t.localRotation=rotation;var m=t.gameObject.AddComponent<TextMesh>();m.font=font;m.fontSize=48;m.characterSize=size;m.anchor=TextAnchor.MiddleCenter;m.text=text;m.color=new Color(.30f,.65f,.43f);t.gameObject.AddComponent<WorldTextLabel>().depthShader=Shader.Find("Hakaton/World Text");}
        static void BuildCar()
        {
            group=Group("02 · Машина · Crown Victoria 2000");
            var source=castSource.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TrailerActor>(true)).Single(a=>a.name.StartsWith("АЛЕКСЕЙ"));
            var driver=UnityEngine.Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(driver,SceneManager.GetActiveScene());
            Hakaton.Demo.Vehicles.Editor.CrownVictoriaRefresh.Install(sequence,group,driver);car=sequence.car;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.gameObject.layer=5;r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=pos;r.sizeDelta=size;return r;}
        static Text Text(string name,Transform parent,int size,Vector2 pos,Vector2 box)
        {var r=Rect(name,parent,pos,box);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=new Color(.84f,.85f,.77f);t.alignment=TextAnchor.MiddleLeft;return t;}
        static Image Image(string name,Transform parent,Vector2 p,Vector2 size,Color color)
        {var r=Rect(name,parent,p,size);var i=r.gameObject.AddComponent<Image>();i.color=color;return i;}
        static void Cameras()
        {
            group=Group("03 · Камеры и кинематографический кадр");
            var world=Group("Камера · дорога",group).gameObject.AddComponent<Camera>();world.nearClipPlane=.045f;world.farClipPlane=260;world.enabled=false;world.allowHDR=false;world.clearFlags=CameraClearFlags.SolidColor;world.backgroundColor=new Color(.065f,.105f,.12f);world.gameObject.AddComponent<UniversalAdditionalCameraData>();picture.worldCamera=world;
            var output=Group("Камера · мастер 16×9",group).gameObject.AddComponent<Camera>();output.orthographic=true;output.orthographicSize=5;output.nearClipPlane=.1f;output.farClipPlane=10;output.allowHDR=false;output.cullingMask=1<<5;output.clearFlags=CameraClearFlags.SolidColor;output.backgroundColor=Color.black;output.gameObject.AddComponent<UniversalAdditionalCameraData>();output.gameObject.AddComponent<AudioListener>();picture.outputCamera=output;
            var canvas=Group("Кадр и название",group).gameObject.AddComponent<Canvas>();canvas.gameObject.layer=5;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=output;canvas.planeDistance=1;var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);picture.canvas=canvas;
            var frame=Rect("Мир 640×360",canvas.transform,Vector2.zero,new Vector2(1280,720)).gameObject.AddComponent<RawImage>();frame.rectTransform.anchorMin=Vector2.zero;frame.rectTransform.anchorMax=Vector2.one;frame.rectTransform.offsetMin=frame.rectTransform.offsetMax=Vector2.zero;picture.picture=frame;
            picture.grade=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Trailer/Art/TrailerGrade.mat")){name="CarIntroGrade"};AssetDatabase.CreateAsset(picture.grade,Art+"/CarIntroGrade.mat");
            picture.topBar=Image("Верхнее каше",canvas.transform,new Vector2(0,643),new Vector2(1280,77),Color.black);picture.bottomBar=Image("Нижнее каше",canvas.transform,Vector2.zero,new Vector2(1280,77),Color.black);
            picture.characterName=Text("Название",canvas.transform,36,new Vector2(72,129),new Vector2(1100,60));picture.role=Text("Подпись",canvas.transform,17,new Vector2(74,99),new Vector2(1100,30));picture.chapter=Text("Номер плана",canvas.transform,13,new Vector2(74,655),new Vector2(1100,25));
            picture.title=Text("Титр",canvas.transform,62,new Vector2(80,315),new Vector2(1120,100));picture.subtitle=Text("Подтитр",canvas.transform,17,new Vector2(80,270),new Vector2(1120,45));picture.accent=Image("Линия титра",canvas.transform,new Vector2(72,194),new Vector2(46,2),new Color(.57f,.64f,.58f));
            picture.controls=Text("Управление",canvas.transform,12,new Vector2(72,25),new Vector2(1100,22));picture.controls.color=new Color(.42f,.45f,.42f);
            picture.fade=Image("Затемнение",canvas.transform,Vector2.zero,new Vector2(1280,720),Color.black);picture.fade.rectTransform.anchorMin=Vector2.zero;picture.fade.rectTransform.anchorMax=Vector2.one;picture.fade.rectTransform.offsetMin=picture.fade.rectTransform.offsetMax=Vector2.zero;
            var music=Group("Dan Wilkie — Deception · начало трека",group).gameObject.AddComponent<AudioSource>();music.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Demo/Audio/Dan Wilkie - Deception.mp3");music.playOnAwake=false;music.loop=false;music.spatialBlend=0;picture.music=music;
            var importer=(AudioImporter)AssetImporter.GetAtPath("Assets/Demo/Audio/Dan Wilkie - Deception.mp3");var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static void Shots()
        {
            var shots=new[]{
                new CarIntroShot{label="Салон · мокрое стекло",start=0,duration=4.62f,followCar=true,insideCar=true,from=V(.48f,1.51f,.35f),to=V(.48f,1.50f,.10f),lookFrom=V(.2f,1.30f,-14),lookTo=V(.1f,1.32f,-16),fieldOfView=58},
                new CarIntroShot{label="Фары · низкий пролёт",start=4.62f,duration=4.58f,followCar=true,from=V(-4.6f,1.1f,-7.5f),to=V(-3.7f,1.35f,-6.3f),lookFrom=V(0,1.1f,-.2f),lookTo=V(0,1.1f,-.4f),fieldOfView=46},
                new CarIntroShot{label="Машина между деревьями",start=9.2f,duration=4.36f,followCar=true,from=V(5.6f,1.45f,2.8f),to=V(5.5f,1.5f,-1),lookFrom=V(.2f,1.1f,-.7f),lookTo=V(0,1.1f,-.4f),fieldOfView=48},
                new CarIntroShot{label="Дорога сверху",start=13.56f,duration=4.76f,followCar=true,from=V(-6,14,8),to=V(-1,19,10),lookFrom=V(0,0,-3),lookTo=V(0,0,-2),fieldOfView=43},
                new CarIntroShot{label="Водитель через стекло",start=18.32f,duration=4.40f,followCar=true,from=V(-1.4f,1.6f,-3.2f),to=V(-1.05f,1.57f,-2.8f),lookFrom=V(.48f,1.55f,0),lookTo=V(.48f,1.55f,0),fieldOfView=37},
                new CarIntroShot{label="Дом появляется из тумана",start=22.72f,duration=3.64f,from=V(3,11,-37),to=V(1.5f,8,-29),lookFrom=V(-1,2.6f,6),lookTo=V(0,2.8f,8),fieldOfView=52},
                new CarIntroShot{label="Прибытие · название",start=26.36f,duration=3.64f,from=V(5,2.8f,-22),to=V(1.5f,2.25f,-16),lookFrom=V(0,1.8f,0),lookTo=V(0,1.8f,0),fieldOfView=49}
            };
            sequence.cameraShots=shots;picture.shots=shots.Select((s,i)=>new TrailerShot{label="",name=i==shots.Length-1?"ДОМ ПОМНИТ":"",role="",start=s.start,duration=s.duration,from=s.from,to=s.to,lookFrom=s.lookFrom,lookTo=s.lookTo,fieldOfView=s.fieldOfView,exterior=true}).ToArray();
        }
    }
}
