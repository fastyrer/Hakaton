using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hakaton.Demo.Editor;
using Hakaton.LocationKit;
using Hakaton.LocationKit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hakaton.Demo.Trailer.Editor
{
    public static class OpeningTrailerBuilder
    {
        public const string ScenePath="Assets/Scenes/OpeningTrailer.unity";
        const string Art="Assets/Demo/Trailer/Art";
        static Transform root,group;
        static Material navy,wood,black,paper,skin,darkSkin,blue,shirt,brown,steel,brass,red,glow,coat;
        static Font font;
        static OpeningTrailer director;
        static readonly List<TrailerActor> actors=new List<TrailerActor>();
        static readonly List<ParticleSystem> effects=new List<ParticleSystem>();
        [MenuItem("Hakaton/Трейлер/Создать вступление")]
        public static void Build()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Остановите Play и сохраните текущую сцену.");
            if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return;}
            EditorSceneManager.OpenScene(PoliceStationBuilder.ScenePath);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),ScenePath);
            actors.Clear();effects.Clear();
            var location=UnityEngine.Object.FindAnyObjectByType<LocationRoot>();
            font=location.game.hud.font??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            location.game.gameObject.SetActive(false);location.enabled=false;
            var env=location.GetComponent<LocationEnvironment>();env.enabled=false;
            foreach(var weather in UnityEngine.Object.FindObjectsByType<DemoWeather>())weather.enabled=false;
            foreach(var npc in UnityEngine.Object.FindObjectsByType<StationNpc>())
            {
                npc.enabled=false;
                var actor=npc.gameObject.AddComponent<TrailerActor>();actor.head=npc.head;actor.leftArm=npc.leftArm;actor.rightArm=npc.rightArm;
                actor.leftLeg=npc.leftLeg;actor.rightLeg=npc.rightLeg;actor.seated=npc.seated;actor.phase=npc.phase;
                actors.Add(actor);
            }
            foreach(var door in UnityEngine.Object.FindObjectsByType<DemoDoor>()){door.SetOpen(true);door.enabled=false;}
            foreach(var text in UnityEngine.Object.FindObjectsByType<TextMesh>())
                if(!text.text.Contains("COUNTY PRECINCT")&&text.text!="ПОЛИЦЕЙСКИЙ УЧАСТОК")text.GetComponent<Renderer>().enabled=false;
            root=new GameObject("ТРЕЙЛЕР · постановка и камера").transform;
            director=root.gameObject.AddComponent<OpeningTrailer>();
            // Awake is only called in Play Mode; all references are authored before then.
            Materials();Smoker();Escort();Surveillance();Wiretap();Cast();CamerasAndUI();Shots();
            director.actors=actors.ToArray();
            TrailerFranksChoiceEdit.Apply(director);
            effects.AddRange(UnityEngine.Object.FindObjectsByType<ParticleSystem>().Where(x=>x.gameObject.activeInHierarchy));
            director.particles=effects.Distinct().ToArray();
            RenderSettings.fog=true;RenderSettings.fogStartDistance=24;RenderSettings.fogEndDistance=90;
            RenderSettings.fogColor=new Color(.12f,.16f,.18f);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.46f,.47f,.42f);RenderSettings.ambientEquatorColor=new Color(.3f,.32f,.29f);RenderSettings.ambientGroundColor=new Color(.15f,.16f,.14f);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            var list=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();list.Insert(list.Any(s=>s.path=="Assets/Scenes/MainMenu.unity")?1:0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=list.ToArray();
            AssetDatabase.SaveAssets();Selection.activeGameObject=root.gameObject;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(-7,1.4f,18),Quaternion.Euler(8,35,0),5);
        }
        [MenuItem("Hakaton/Трейлер/Открыть вступление")]
        public static void Open(){if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Остановите Play и сохраните сцену.");EditorSceneManager.OpenScene(ScenePath);}
        static Material Get(string folder,string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Art/"+folder+"/"+name+".mat");
        static Material Mat(string name,Color c)
        {
            string p=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m!=null)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=c};m.SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(m,p);return m;
        }
        static void Materials()
        {
            navy=Get("Station","Sign_navy");wood=Get("Generated","Dark_walnut");black=Get("Station","Rubber");paper=Get("Station","Archive_paper");
            skin=Get("Station","Skin");blue=Get("Station","Uniform");shirt=Get("Station","Detective_shirt");steel=Get("Station","Filing_steel");brass=Get("Station","Brass");red=Get("Station","Case_red");
            darkSkin=Mat("Detective_skin",new Color(.29f,.16f,.105f));brown=Mat("Worn_coat",new Color(.25f,.19f,.12f));coat=Mat("Surveillance_coat",new Color(.12f,.15f,.16f));
            glow=Mat("Amber_LED",new Color(1,.45f,.09f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.24f,.025f)*2);
        }
        static Transform Group(string name,Transform parent=null)
        {var t=new GameObject(name).transform;t.SetParent(parent??root,false);return t;}
        static GameObject Box(string name,Vector3 pos,Vector3 size,Material mat,Transform parent=null)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent??group,false);o.transform.localPosition=pos;o.transform.localScale=size;
            o.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;
        }
        static void Disc(string name,Vector3 p,float radius,float depth,Material m,Transform parent)
        {
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localRotation=Quaternion.Euler(90,0,0);o.transform.localScale=new Vector3(radius*2,depth,radius*2);
            o.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Demo/Art/Generated/Cylinder8.asset");o.GetComponent<Renderer>().sharedMaterial=m;
        }
        static void Label(string text,Vector3 p,float size=.08f,Transform parent=null)
        {
            var t=Group(text,parent??group);t.localPosition=p;
            var mesh=t.gameObject.AddComponent<TextMesh>();mesh.font=font;mesh.fontSize=48;mesh.characterSize=size;mesh.text=text;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.color=new Color(.8f,.77f,.6f);
            t.gameObject.AddComponent<WorldTextLabel>().depthShader=Shader.Find("Hakaton/World Text");
        }
        static void Light(Vector3 p,Color color,float intensity=3,float range=7,Transform parent=null)
        {
            var l=Group("Постановочный свет",parent??group).gameObject.AddComponent<Light>();l.transform.localPosition=p;l.color=color;l.type=LightType.Point;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;
        }
        static GameObject Piece(string id,Vector3 p,float yaw=0)
        {
            var o=(GameObject)PrefabUtility.InstantiatePrefab(KitLibraryBuilder.Load().Find(id).prefab);o.transform.SetParent(group,false);o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));PrefabUtility.RecordPrefabInstancePropertyModifications(o.transform);return o;
        }
        static TrailerActor Person(string name,Vector3 pos,float yaw,Material clothes,Material complexion,bool seated=false,bool smoker=false)
        {
            var t=Group(name,group);t.position=pos;t.rotation=Quaternion.Euler(0,yaw,0);
            var a=t.gameObject.AddComponent<TrailerActor>();actors.Add(a);a.seated=seated;a.smoking=smoker;a.phase=actors.Count*.8f;
            float hip=seated?.67f:.94f;
            Box("Туловище",new Vector3(0,hip+.29f,0),new Vector3(.5f,.57f,.29f),clothes,t);
            Box("Рубашка",new Vector3(0,hip+.39f,.155f),new Vector3(.16f,.34f,.025f),shirt,t);
            Box("Галстук",new Vector3(0,hip+.30f,.174f),new Vector3(.055f,.34f,.025f),black,t);
            foreach(float side in new[]{-1f,1f}){var lapel=Box("Лацкан",new Vector3(side*.13f,hip+.42f,.167f),new Vector3(.09f,.31f,.03f),clothes,t);lapel.transform.localRotation=Quaternion.Euler(0,0,side*18);}
            Box("Ремень",new Vector3(0,hip+.04f,0),new Vector3(.5f,.065f,.31f),black,t);
            Box("Шея",new Vector3(0,hip+.63f,0),new Vector3(.15f,.15f,.15f),complexion,t);
            a.head=Group("Голова",t);a.head.localPosition=new Vector3(0,hip+.82f,0);
            Disc("Голова",Vector3.zero,.17f,.36f,complexion,a.head);
            // Head cylinder stands vertically; its octagonal silhouette keeps the PS1 style.
            a.head.GetChild(0).localRotation=Quaternion.identity;
            Box("Волосы",new Vector3(0,.17f,-.025f),new Vector3(.31f,.09f,.28f),smoker?black:wood,a.head);
            Box("Нос",new Vector3(0,-.01f,.18f),new Vector3(.055f,.08f,.07f),complexion,a.head);
            foreach(float x in new[]{-.075f,.075f})
            {
                Box("Глаз",new Vector3(x,.025f,.165f),new Vector3(.034f,.023f,.016f),black,a.head);
                Box("Бровь",new Vector3(x,.07f,.16f),new Vector3(.08f,.023f,.018f),black,a.head);
            }
            a.leftArm=Group("Левая рука",t);a.leftArm.localPosition=new Vector3(-.32f,hip+.54f,0);
            a.rightArm=Group("Правая рука",t);a.rightArm.localPosition=new Vector3(.32f,hip+.54f,0);
            foreach(var arm in new[]{a.leftArm,a.rightArm})
            {Box("Рукав",new Vector3(0,-.24f,0),new Vector3(.16f,.46f,.18f),clothes,arm);Box("Кисть",new Vector3(0,-.51f,0),new Vector3(.13f,.16f,.14f),complexion,arm);}
            a.leftLeg=Group("Левая нога",t);a.leftLeg.localPosition=new Vector3(-.14f,hip,0);
            a.rightLeg=Group("Правая нога",t);a.rightLeg.localPosition=new Vector3(.14f,hip,0);
            foreach(var leg in new[]{a.leftLeg,a.rightLeg})
            {
                if(smoker)
                {
                    Box("Нога на столе",new Vector3(0,.23f,.47f),new Vector3(.22f,.22f,.95f),black,leg);
                    Box("Ботинок на столе",new Vector3(0,.25f,1.04f),new Vector3(.25f,.21f,.39f),black,leg);
                }
                else if(seated)
                {
                    Box("Бедро",new Vector3(0,-.08f,.21f),new Vector3(.22f,.22f,.47f),black,leg);
                    Box("Голень",new Vector3(0,-.37f,.4f),new Vector3(.19f,.5f,.21f),black,leg);Box("Ботинок",new Vector3(0,-.61f,.47f),new Vector3(.24f,.15f,.36f),black,leg);
                }
                else{Box("Штанина",new Vector3(0,-.4f,0),new Vector3(.22f,.8f,.24f),black,leg);Box("Ботинок",new Vector3(0,-.87f,.06f),new Vector3(.25f,.14f,.37f),black,leg);}
            }
            return a;
        }
        static void Smoker()
        {
            group=Group("01 · Напарник · сигарета и ноги на столе");
            foreach(var obj in UnityEngine.Object.FindObjectsByType<Transform>().ToArray())
            {
                if(obj==group||obj.IsChildOf(root))continue;
                if(obj.name=="NPC · Следователь"&&Vector3.Distance(obj.position,new Vector3(-7,0,19))<1) obj.gameObject.SetActive(false);
                if(obj.name=="chair"&&Vector3.Distance(obj.position,new Vector3(-7,0,19))<1)obj.gameObject.SetActive(false);
                if((obj.name=="CRT-монитор"||obj.name=="Монохромный экран"||obj.name=="Клавиатура")&&Mathf.Abs(obj.position.x+7)<.6f&&Mathf.Abs(obj.position.z-18)<.7f)obj.gameObject.SetActive(false);
            }
            Piece("chair",new Vector3(-7,0,19),0);
            var a=Person("ДЕТЕКТИВ · напарник Алексея",new Vector3(-7,0,19),180,blue,darkSkin,true,true);
            a.leftArm.localRotation=Quaternion.Euler(-35,0,12);a.rightArm.localRotation=Quaternion.Euler(-110,0,-20);
            // The cigarette is close to the mouth, with a separate glowing tip and rising smoke.
            Box("Сигарета",new Vector3(.18f,1.43f,.28f),new Vector3(.018f,.018f,.18f),paper,a.transform);
            Box("Тлеющий кончик",new Vector3(.18f,1.43f,.375f),new Vector3(.021f,.021f,.025f),glow,a.transform);
            var ps=Group("ДЫМ · сигарета",a.transform).gameObject.AddComponent<ParticleSystem>();ps.transform.localPosition=new Vector3(.18f,1.45f,.39f);ps.transform.rotation=Quaternion.Euler(-90,0,0);
            var main=ps.main;main.playOnAwake=false;main.startLifetime=new ParticleSystem.MinMaxCurve(3,4.8f);main.startSpeed=.12f;main.startSize=new ParticleSystem.MinMaxCurve(.06f,.13f);main.startColor=new Color(.72f,.8f,.8f,.55f);main.maxParticles=100;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission;emission.rateOverTime=10;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=9;shape.radius=.015f;
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=new ParticleSystem.MinMaxCurve(-.055f,.055f);velocity.y=new ParticleSystem.MinMaxCurve(.16f,.25f);velocity.z=new ParticleSystem.MinMaxCurve(-.035f,.035f);
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.3f),new Keyframe(1,3.3f)));
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.6f,.15f),new GradientAlphaKey(0,1)});color.color=gradient;
            ps.useAutoRandomSeed=false;ps.randomSeed=37;
            var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Soft cigarette smoke",wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f;texture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2)));}texture.Apply();AssetDatabase.CreateAsset(texture,Art+"/SmokeTexture.asset");
            var m=new Material(Shader.Find("Hakaton/Trailer Smoke")){name="Cigarette smoke",mainTexture=texture};AssetDatabase.CreateAsset(m,Art+"/Smoke.mat");ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=m;
            Piece("desk_lamp",new Vector3(-8.25f,.88f,18.8f));Light(new Vector3(-6,2.1f,19.3f),new Color(.43f,.67f,.8f),3.8f,5);
            Light(new Vector3(-8.3f,1.9f,17),new Color(1,.65f,.3f),2.1f,5);
        }
        static void Escort()
        {
            group=Group("02 · Конвой в изолятор");
            foreach(var a in actors.Where(x=>x.transform.position.x>-2&&x.transform.position.x<2&&x.transform.position.z>24).ToArray())a.gameObject.SetActive(false);
            var officer=Person("Конвоир",new Vector3(.65f,0,26),0,blue,skin);
            var prisoner=Person("Бездомный · задержан",new Vector3(-.2f,0,25.1f),0,brown,skin);
            foreach(var a in new[]{officer,prisoner}){a.walking=true;a.walkStart=20;a.walkDuration=8.55f;}
            officer.path=new[]{new Vector3(.65f,0,26),new Vector3(.65f,0,30),new Vector3(-6,0,30)};
            prisoner.path=new[]{new Vector3(-.2f,0,25.1f),new Vector3(-.2f,0,30),new Vector3(-5,0,30)};
            Box("Шапка",new Vector3(0,.14f,0),new Vector3(.37f,.22f,.35f),brown,prisoner.head);
            Box("Небритая борода",new Vector3(0,-.105f,.12f),new Vector3(.29f,.16f,.15f),brown,prisoner.head);
            Box("Длинная куртка задержанного",new Vector3(0,.91f,0),new Vector3(.57f,.95f,.36f),brown,prisoner.transform);
            Box("Заплата на пальто",new Vector3(-.13f,1.2f,.16f),new Vector3(.12f,.17f,.025f),shirt,prisoner.transform);
            Label("ИЗОЛЯТОР",new Vector3(-2.70f,2.7f,30),.09f);group.GetChild(group.childCount-1).rotation=Quaternion.Euler(0,-90,0);
        }
        static void Room(Vector3 origin,string name,float width=10,float depth=8)
        {
            group=Group(name);group.position=origin;
            Box("Пол",new Vector3(0,-.1f,depth/2),new Vector3(width,.2f,depth),wood);
            Box("Задняя стена",new Vector3(0,1.6f,depth),new Vector3(width,3.2f,.2f),navy);
            foreach(float x in new[]{-width/2,width/2})Box("Боковая стена",new Vector3(x,1.6f,depth/2),new Vector3(.2f,3.2f,depth),navy);
            Box("Потолок",new Vector3(0,3.2f,depth/2),new Vector3(width,.2f,depth),black);
        }
        static void Glasses(Transform head)
        {
            foreach(float x in new[]{-.085f,.085f})Box("Очки · линзы",new Vector3(x,.028f,.18f),new Vector3(.13f,.075f,.025f),black,head);
            Box("Очки · переносица",new Vector3(0,.03f,.185f),new Vector3(.05f,.022f,.027f),steel,head);
        }
        static void Surveillance()
        {
            Room(new Vector3(62,0,10),"03 · Наблюдение через жалюзи",7,6);
            Box("Письменный стол",new Vector3(0,.8f,3.9f),new Vector3(2,.12f,1),wood);
            var man=Person("Наблюдатель · очки и пальто",new Vector3(62,0,11.8f),180,coat,skin);Glasses(man.head);
            Box("Длинное пальто",new Vector3(0,.6f,0),new Vector3(.56f,.95f,.35f),coat,man.transform);
            foreach(float x in new[]{-1.55f,1.55f})Box("Оконная рама",new Vector3(x,1.6f,0),new Vector3(.14f,2.1f,.15f),wood);
            for(int i=0;i<12;i++)
            {var slat=Box("Жалюзи · полоса",new Vector3(0,.61f+i*.155f,0),new Vector3(3.1f,.068f,.1f),black);slat.transform.localRotation=Quaternion.Euler(16,0,0);}
            foreach(float x in new[]{-1.15f,1.15f})Box("Шнур жалюзи",new Vector3(x,1.5f,-.03f),new Vector3(.012f,1.95f,.012f),paper);
            Light(new Vector3(-1.1f,2.1f,1.2f),new Color(.36f,.57f,.72f),4.5f,6);
            Light(new Vector3(1,1.1f,3.7f),new Color(1,.55f,.22f),2,5);
        }
        static Transform Reel(Transform parent,float x)
        {
            var r=Group("Вращающаяся катушка",parent);r.localPosition=new Vector3(x,.23f,-.2f);
            Disc("Магнитная лента",Vector3.zero,.20f,.045f,black,r);Disc("Металлический обод",new Vector3(0,0,-.03f),.23f,.014f,steel,r);
            for(int j=0;j<3;j++){var spoke=Box("Спица",new Vector3(0,0,-.048f),new Vector3(.04f,.4f,.02f),black,r);spoke.transform.localRotation=Quaternion.Euler(0,0,j*60);}
            Disc("Ступица",new Vector3(0,0,-.065f),.052f,.03f,brass,r);return r;
        }
        static void TapeRecorder(Vector3 p)
        {
            var t=Group("Катушечный магнитофон",group);t.localPosition=p;
            Box("Корпус",new Vector3(0,.15f,0),new Vector3(1.15f,.9f,.38f),steel,t);
            var a=t.gameObject.AddComponent<TrailerActor>();a.reels=new[]{Reel(t,-.29f),Reel(t,.29f)};actors.Add(a);
            for(int i=0;i<5;i++)Box("Клавиша",new Vector3(-.32f+i*.16f,-.12f,-.215f),new Vector3(.11f,.08f,.04f),i==0?red:black,t);
            for(int i=0;i<2;i++){Box("VU-метр",new Vector3(-.2f+i*.4f,-.23f,-.215f),new Vector3(.28f,.13f,.025f),paper,t);Box("Стрелка",new Vector3(-.2f+i*.4f,-.23f,-.235f),new Vector3(.018f,.1f,.012f),black,t);}
            Box("REC",new Vector3(.45f,-.24f,-.24f),new Vector3(.045f,.045f,.025f),glow,t);
        }
        static void Wiretap()
        {
            Room(new Vector3(90,0,0),"04 · Комната прослушки",12,10);
            for(int i=0;i<3;i++)
            {
                float x=-3.7f+i*3.7f;
                Box("Рабочий стол",new Vector3(x,.8f,5),new Vector3(3,.14f,1.1f),wood);
                TapeRecorder(new Vector3(x,1.16f,4.8f));
                var a=Person("Агент прослушки "+(i+1),new Vector3(90+x+.9f,0,6.1f),180,coat,skin,true);
                foreach(float side in new[]{-1f,1f})Box("Наушник",new Vector3(side*.19f,.01f,0),new Vector3(.09f,.18f,.17f),black,a.head);
                Box("Дужка наушников",new Vector3(0,.21f,0),new Vector3(.43f,.055f,.12f),black,a.head);
                Light(new Vector3(x,2.3f,4.4f),new Color(.47f,.72f,.69f),3.2f,6);
            }
            for(int i=0;i<5;i++)
            {
                Box("Стойка аппаратуры",new Vector3(-5.3f,1.15f,2+i*1.5f),new Vector3(.8f,2.3f,1.15f),black);
                for(int j=0;j<7;j++)Box("Индикатор стойки",new Vector3(-4.88f,.35f+j*.24f,2+i*1.5f),new Vector3(.025f,.035f,.2f),j%3==0?glow:steel);
            }
            Label("КАНАЛ 04 / ЗАПИСЬ",new Vector3(0,2.4f,9.85f),.1f);
        }
        static void Cast()
        {
            Room(new Vector3(120,0,0),"05 · Алексей · герой",7,6);
            Person("АЛЕКСЕЙ · главный герой",new Vector3(120,0,3),180,coat,skin);
            Box("Папка расследования",new Vector3(.5f,.92f,2.9f),new Vector3(.46f,.6f,.08f),paper);
            Box("Стол с делом",new Vector3(0,.75f,1.7f),new Vector3(2,.12f,.9f),wood);
            Light(new Vector3(-1,2.2f,1),new Color(.5f,.7f,.77f),4,6);Light(new Vector3(1.3f,1.6f,4.3f),new Color(1,.6f,.28f),3,6);
            Room(new Vector3(140,0,0),"06 · Иван · брат",7,6);
            var ivan=Person("ИВАН · брат Алексея",new Vector3(140,0,3),180,shirt,skin);Glasses(ivan.head);
            Box("Книга в руках",new Vector3(0,1.1f,.42f),new Vector3(.47f,.34f,.09f),brown,ivan.transform);
            Piece("bookcase",new Vector3(142,0,5.5f),180);
            Light(new Vector3(-1,2,1.5f),new Color(1,.73f,.42f),4,6);
            Room(new Vector3(160,0,0),"07 · Дмитрий · брат",7,6);
            var d=Person("ДМИТРИЙ · брат Алексея",new Vector3(160,0,3),165,brown,skin);d.head.localRotation=Quaternion.Euler(-8,0,0);
            Box("Красная майка",new Vector3(0,1.29f,.17f),new Vector3(.17f,.31f,.03f),red,d.transform);
            Box("Сжатая кисть",new Vector3(.34f,.83f,.13f),new Vector3(.17f,.18f,.18f),skin,d.transform);
            Light(new Vector3(-1,2.2f,1),new Color(1,.5f,.25f),4,6);Light(new Vector3(1.5f,2,4),new Color(.4f,.62f,.75f),2.5f,5);
            Room(new Vector3(180,0,0),"08 · Павел · тень",7,6);
            var p=Person("ПАВЕЛ · незваный гость",new Vector3(180,0,3),180,black,skin);
            Box("Пальто",new Vector3(0,.58f,0),new Vector3(.56f,1.02f,.33f),black,p.transform);
            Light(new Vector3(.7f,2,4.3f),new Color(.39f,.6f,.7f),4,6);Light(new Vector3(-1.2f,1.6f,1),new Color(.8f,.54f,.28f),1.1f,5);
            Room(new Vector3(200,0,0),"09 · Фёдор · портрет отца",7,6);
            var father=Person("ФЁДОР · изображение в рамке",new Vector3(200,.1f,4.8f),180,brown,skin);
            Box("Седые волосы",new Vector3(0,.17f,-.02f),new Vector3(.33f,.12f,.29f),paper,father.head);
            // A deep frame around a motionless portrait; the monochrome shot reads as a memory.
            foreach(float x in new[]{-1f,1f})Box("Вертикальная рама",new Vector3(x,1.2f,4.35f),new Vector3(.16f,2.35f,.18f),wood);
            foreach(float y in new[]{.1f,2.3f})Box("Горизонтальная рама",new Vector3(0,y,4.35f),new Vector3(2.1f,.16f,.18f),wood);
            Box("Мемориальная лента",new Vector3(.72f,.47f,4.22f),new Vector3(.14f,.95f,.03f),black).transform.Rotate(0,0,-35);
            Light(new Vector3(-.5f,2.3f,2.6f),new Color(1,.77f,.48f),4,6);father.head=null;
        }
        static RectTransform Rect(string name,Transform parent)
        {var t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.SetParent(parent,false);t.gameObject.layer=5;return t;}
        static void Stretch(RectTransform t){t.anchorMin=Vector2.zero;t.anchorMax=Vector2.one;t.offsetMin=t.offsetMax=Vector2.zero;}
        static Image UIBox(string name,Transform parent,Color c)
        {var r=Rect(name,parent);var i=r.gameObject.AddComponent<Image>();i.color=c;return i;}
        static Text UIText(string name,Transform parent,string text,int size,Vector2 pos,Vector2 box,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var r=Rect(name,parent);r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=pos;r.sizeDelta=box;
            var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=text;t.color=new Color(.91f,.89f,.79f);t.alignment=alignment;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
            return t;
        }
        static void CamerasAndUI()
        {
            group=Group("10 · Камеры и титры");
            var cam=Group("Камера · пролёты",group).gameObject.AddComponent<Camera>();cam.enabled=false;cam.nearClipPlane=.06f;cam.farClipPlane=100;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=RenderSettings.fogColor;cam.cullingMask=~(1<<5);cam.allowHDR=false;cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            director.worldCamera=cam;
            var output=Group("Камера · мастер 16×9",group).gameObject.AddComponent<Camera>();output.clearFlags=CameraClearFlags.SolidColor;output.backgroundColor=Color.black;output.cullingMask=1<<5;output.orthographic=true;output.orthographicSize=5;output.nearClipPlane=.1f;output.farClipPlane=10;output.allowHDR=false;output.gameObject.AddComponent<UniversalAdditionalCameraData>();output.gameObject.AddComponent<AudioListener>();director.outputCamera=output;
            var canvas=Group("Титры и кинематографический кадр",group).gameObject.AddComponent<Canvas>();canvas.gameObject.layer=5;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=output;canvas.planeDistance=1;
            var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;director.canvas=canvas;
            var image=Rect("Мир PS1",canvas.transform).gameObject.AddComponent<RawImage>();Stretch(image.rectTransform);director.picture=image;
            var material=new Material(Shader.Find("Hakaton/Trailer Grade")){name="Detective colour grade"};AssetDatabase.CreateAsset(material,Art+"/TrailerGrade.mat");director.grade=material;
            director.topBar=UIBox("Верхнее каше",canvas.transform,Color.black);director.topBar.rectTransform.anchorMin=new Vector2(0,1);director.topBar.rectTransform.anchorMax=Vector2.one;director.topBar.rectTransform.pivot=new Vector2(.5f,1);director.topBar.rectTransform.sizeDelta=new Vector2(0,77);
            director.bottomBar=UIBox("Нижнее каше",canvas.transform,Color.black);director.bottomBar.rectTransform.anchorMin=Vector2.zero;director.bottomBar.rectTransform.anchorMax=new Vector2(1,0);director.bottomBar.rectTransform.pivot=new Vector2(.5f,0);director.bottomBar.rectTransform.sizeDelta=new Vector2(0,77);
            director.characterName=UIText("Имя",canvas.transform,"",36,new Vector2(72,129),new Vector2(1040,60));
            director.role=UIText("Кто есть кто",canvas.transform,"",17,new Vector2(74,99),new Vector2(1100,30));
            director.chapter=UIText("Номер кадра",canvas.transform,"",13,new Vector2(74,655),new Vector2(1100,25));
            director.title=UIText("Название игры",canvas.transform,"",62,new Vector2(80,315),new Vector2(1120,100),TextAnchor.MiddleCenter);
            director.subtitle=UIText("Финальная строка",canvas.transform,"",17,new Vector2(80,270),new Vector2(1120,45),TextAnchor.MiddleCenter);
            director.accent=UIBox("Линия имени",canvas.transform,new Color(.63f,.48f,.27f));var line=director.accent.rectTransform;line.anchorMin=line.anchorMax=Vector2.zero;line.pivot=Vector2.zero;line.anchoredPosition=new Vector2(72,194);line.sizeDelta=new Vector2(46,2);
            director.controls=UIText("Управление просмотром",canvas.transform,"ПРОБЕЛ · ПАУЗА       R · СНАЧАЛА       ESC · К ИГРЕ",12,new Vector2(72,25),new Vector2(1100,22));director.controls.color=new Color(.42f,.45f,.42f);
            director.fade=UIBox("Затемнение кадра",canvas.transform,Color.black);Stretch(director.fade.rectTransform);
            var sound=Group("Commodo — Living Bones · монтаж",group).gameObject.AddComponent<AudioSource>();sound.clip=PoliceStationBuilder.FindTrack();sound.spatialBlend=0;sound.playOnAwake=false;sound.loop=false;director.music=sound;
        }
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static void Shots()
        {
            var shots=new List<TrailerShot>();float cursor=0;
            Action<string,string,string,float,Vector3,Vector3,Vector3,Vector3,float,bool,bool> add=(label,name,role,duration,from,to,lookFrom,lookTo,fov,title,mono)=>
            {shots.Add(new TrailerShot{label=label,name=name,role=role,start=cursor,duration=duration,from=from,to=to,lookFrom=lookFrom,lookTo=lookTo,fieldOfView=fov,title=title,monochrome=mono});cursor+=duration;};
            float bar=60f/140*4;
            add("НЬЮ-ЙОРК / 2000","ПОЛИЦЕЙСКИЙ УЧАСТОК","НОЧНАЯ СМЕНА",bar*3,V(-10,3.2f,-18),V(-3,2.1f,-9),V(0,1.8f,0),V(0,1.6f,0),49,false,false);
            add("01 / ДЕЖУРНАЯ ЧАСТЬ","","",bar*3,V(-4,1.5f,1.7f),V(.8f,1.55f,5.7f),V(10,1.4f,5),V(10,1.5f,6),54,false,false);
            add("02 / ДЕТЕКТИВНЫЙ ОТДЕЛ","ДЕТЕКТИВ","НАПАРНИК АЛЕКСЕЯ · ПОДОЗРЕВАЕТ ЕГО",bar*4.6666667f,V(-4.8f,1.4f,16.8f),V(-8.7f,1.5f,16.9f),V(-7,1.2f,18.8f),V(-7,1.35f,18.9f),46,false,false);
            add("03 / ИЗОЛЯТОР","ЗАДЕРЖАНИЕ","ПОКА ОДНИ ИЩУТ ОТВЕТЫ, ДРУГИХ ВЕДУТ НА ДОПРОС",bar*5,V(1.9f,1.75f,22.4f),V(1.9f,1.75f,27.1f),V(-.1f,1.1f,27),V(-3.5f,1.1f,30),55,false,false);
            add("04 / СКРЫТОЕ НАБЛЮДЕНИЕ","","",bar*5,V(60.9f,1.78f,8.45f),V(62.4f,1.79f,9.1f),V(62,1.78f,11.8f),V(62,1.78f,11.8f),38,false,false);
            add("05 / КОМНАТА ПРОСЛУШКИ","ЗАПИСЬ ПРОДОЛЖАЕТСЯ","КАТУШКИ ХРАНЯТ ТО, ЧТО ЛЮДИ ПРЕДПОЧИТАЮТ ЗАБЫТЬ",bar*5,V(85.4f,1.95f,.5f),V(93.8f,1.95f,.9f),V(88,1.45f,5.2f),V(92,1.45f,5.2f),58,false,false);
            add("06 / ДЕЙСТВУЮЩИЕ ЛИЦА","АЛЕКСЕЙ","ГЛАВНЫЙ ГЕРОЙ · ВЗЯЛ ОТПУСК, ЧТОБЫ РАССЛЕДОВАТЬ СМЕРТЬ ОТЦА",bar*3,V(118.7f,1.65f,.4f),V(120.9f,1.6f,.9f),V(120,1.45f,3),V(120,1.48f,3),42,false,false);
            add("07 / ДЕЙСТВУЮЩИЕ ЛИЦА","ИВАН","БРАТ АЛЕКСЕЯ · РАЦИОНАЛЬНЫЙ МЫСЛИТЕЛЬ",bar*3,V(138.8f,1.65f,.5f),V(140.7f,1.65f,.9f),V(140,1.45f,3),V(140,1.5f,3),42,false,false);
            add("08 / ДЕЙСТВУЮЩИЕ ЛИЦА","ДМИТРИЙ","БРАТ АЛЕКСЕЯ · ВСПЫЛЬЧИВЫЙ, ЖИВЁТ ЭМОЦИЯМИ",bar*3,V(161.3f,1.6f,.5f),V(159.3f,1.65f,.9f),V(160,1.4f,3),V(160,1.5f,3),42,false,false);
            add("09 / ДЕЙСТВУЮЩИЕ ЛИЦА","ПАВЕЛ","ЧЕЛОВЕК, КОТОРОМУ НЕЛЬЗЯ ДОВЕРЯТЬ",bar*3,V(178.9f,1.65f,.4f),V(180.7f,1.6f,.9f),V(180,1.45f,3),V(180,1.5f,3),42,false,false);
            add("10 / ИЗ СЕМЕЙНОГО АРХИВА","ФЁДОР","ОТЕЦ · ПОГИБ ДО НАЧАЛА ЭТИХ СОБЫТИЙ",bar*3,V(198.9f,1.55f,1.3f),V(200.5f,1.6f,1.7f),V(200,1.3f,4.7f),V(200,1.4f,4.7f),38,false,true);
            add("11 / ЗАКРЫТОЕ ДЕЛО","ВЕЩЕСТВЕННЫЕ ДОКАЗАТЕЛЬСТВА","ТРИ ДОКУМЕНТА. ДВА КАЛИБРА. ОДНО ЗАКРЫТОЕ ДЕЛО.",bar*3,V(6,1.6f,25.7f),V(13.5f,1.55f,25.7f),V(7,1,27),V(13,1,27),46,false,false);
            add("12 / ДЕТЕКТИВНЫЙ ОТДЕЛ","","",bar*3,V(-5.7f,1.65f,17.1f),V(-6.4f,1.65f,17.7f),V(-7,1.52f,19),V(-7,1.5f,19),36,false,false);
            add("ДЕТЕКТИВ / ПСИХОЛОГИЧЕСКИЙ ХОРРОР","","",bar*3,V(0,1.55f,10),V(0,1.55f,18),V(0,1.6f,39),V(0,1.6f,39),49,true,false);
            // Measured transients in the supplied Living Bones recording, rounded to 24 fps.
            float[] cuts={0.000000f,3.708333f,7.750000f,16.000000f,23.958333f,31.750000f,43.708333f,50.083333f,56.208333f,61.916667f,68.250000f,74.166667f,80.083333f,86.000000f,94.000000f};
            for(int i=0;i<shots.Count;i++){shots[i].start=cuts[i];shots[i].duration=cuts[i+1]-cuts[i];}
            director.shots=shots.ToArray();
            foreach(var actor in actors.Where(x=>x.walking))
            {actor.walkStart=shots[3].start;actor.walkDuration=shots[3].duration;}
        }
    }
}
