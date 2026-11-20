using System;
using System.IO;
using System.Linq;
using Hakaton.LocationKit;
using Hakaton.LocationKit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Hakaton.Demo.Editor
{
    public static class PoliceStationBuilder
    {
        public const string ScenePath = "Assets/Scenes/PoliceStation.unity";
        const string Art = "Assets/Demo/Art/Station";
        static LocationRoot root;
        static Transform group;
        static Font font;
        static Material plaster, green, navy, steel, wood, paper, black, brass, skin, uniform, shirt, trousers, glow, red;
        static Mesh cylinder;
        static int npcCount;

        [MenuItem("Hakaton/Создать полицейский участок")]
        public static string Build()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Остановите Play и сохраните текущую сцену.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Участок уже существует: откройте PoliceStation. Генератор не перезаписывает вашу работу.");
            if (KitLibraryBuilder.Load() == null) KitLibraryBuilder.Build();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            root = LocationKitOperations.EnsureRoot(); root.locationName = "Полицейский участок";
            root.introduction = "Три года назад дело отца закрыли как самоубийство.\nСегодня вы впервые можете прочитать оригиналы.\n\nАрхив — в конце коридора справа. Найдите дело В-97/041.\nСопоставьте протокол, опись оружия и заключение эксперта.";
            root.objective = "Архив → дело В-97/041 · соберите 3 документа · сравните калибры на доске [Tab]";
            LocationKitOperations.AddPlayerRig();
            root.game.arrivalScene = true;
            root.game.player.view.farClipPlane = 110;
            root.game.player.view.fieldOfView = 76;
            root.game.player.flashlight.enabled = false;
            LocationKitOperations.SetSpawn(new Vector3(0, 0.08f, -8), 0);
            font = root.game.hud.font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Materials();
            Shell(); Interior(); Archive(); Exterior(); People();
            var audio = new GameObject("Наушники · Commodo — Living Bones", typeof(AudioSource), typeof(ArrivalHeadphones));
            audio.transform.SetParent(root.game.player.transform, false);
            var headphones = audio.GetComponent<ArrivalHeadphones>(); headphones.game = root.game;
            headphones.track = FindTrack();
            var env = LocationKitOperations.EnsureEnvironment(root);
            env.fog = true; env.fogStart = 22; env.fogEnd = 94;
            env.fogColor = new Color(.14f,.19f,.22f); env.ambientColor = new Color(.47f,.49f,.43f);
            env.mainLightColor = new Color(.56f,.68f,.8f); env.mainLightIntensity = .3f;
            env.rain = true; env.rainArea = new Vector2(88, 104); env.rainHeight = 15; env.rainRate = 1600;
            env.rainSystem.transform.parent.position = new Vector3(0,0,15);
            env.Apply(); LocationKitOperations.RefreshConnections(root);
            EditorUtility.SetDirty(root.game);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.game.player);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.game.player.view);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.game.player.flashlight);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root.gameObject;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(0,1,18), Quaternion.Euler(55,-35,0), 50);
            Debug.Log("PoliceStation: 40×40 m, archive, 3 case documents, " + npcCount + " NPCs. Music: " + (headphones.track != null ? "connected" : "waiting for local audio file"));
            return ScenePath;
        }

        public static AudioClip FindTrack()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Demo/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Replace('_',' ').Replace('-',' ');
                if (name.Contains("living") && name.Contains("bones")) return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            return null;
        }
        [MenuItem("Hakaton/Подключить Living Bones из Assets/Demo/Audio")]
        public static void ConnectTrack()
        {
            var track = FindTrack();
            if (track == null) throw new InvalidOperationException("Положите аудиофайл с Living Bones в названии в Assets/Demo/Audio.");
            var headphones = UnityEngine.Object.FindAnyObjectByType<ArrivalHeadphones>();
            if (headphones == null) throw new InvalidOperationException("Откройте сцену PoliceStation.");
            Undo.RecordObject(headphones, "Музыка по прибытии"); headphones.track = track;
            EditorSceneManager.MarkSceneDirty(headphones.gameObject.scene);
        }
        static void Materials()
        {
            Directory.CreateDirectory(Art); AssetDatabase.Refresh();
            plaster = Mat("Institutional_plaster", new Color(.65f,.65f,.52f), true);
            green = Mat("Police_green", new Color(.25f,.36f,.32f), true);
            navy = Mat("Sign_navy", new Color(.055f,.12f,.18f));
            steel = Mat("Filing_steel", new Color(.35f,.40f,.38f));
            wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Art/Generated/Dark_walnut.mat");
            paper = Mat("Archive_paper", new Color(.76f,.70f,.49f));
            black = Mat("Rubber", new Color(.035f,.043f,.046f));
            brass = Mat("Brass", new Color(.68f,.51f,.23f));
            skin = Mat("Skin", new Color(.65f,.46f,.33f));
            uniform = Mat("Uniform", new Color(.11f,.19f,.25f));
            shirt = Mat("Detective_shirt", new Color(.64f,.62f,.48f));
            trousers = Mat("Trousers", new Color(.13f,.14f,.14f));
            glow = Mat("Fluorescent", new Color(.79f,.88f,.71f));
            glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(.6f,.8f,.61f) * 1.5f);
            red = Mat("Case_red", new Color(.45f,.13f,.09f));
            cylinder = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Demo/Art/Generated/Cylinder8.asset");
        }
        static Material Mat(string name, Color color, bool texture = false)
        {
            string path = Art + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            m.SetFloat("_Smoothness", .12f);
            if (texture)
            {
                string texturePath = Art + "/" + name + ".asset";
                var t = new Texture2D(32,32,TextureFormat.RGB24,false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
                var random = new System.Random(97);
                for (int y=0; y<32; y++) for (int x=0; x<32; x++)
                { float v = .8f + (float)random.NextDouble() * .2f; if (y % 16 == 0) v *= .8f; t.SetPixel(x,y,new Color(v,v,v)); }
                t.Apply(); AssetDatabase.CreateAsset(t, texturePath); m.mainTexture = t; m.mainTextureScale = new Vector2(2,2);
            }
            AssetDatabase.CreateAsset(m,path); return m;
        }
        static Transform Group(string name, Transform parent = null)
        { var o = new GameObject(name); o.transform.SetParent(parent ?? root.props,false); return o.transform; }
        static GameObject Box(string name, Vector3 p, Vector3 scale, Material material, bool collision = true, Transform parent = null)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = name;
            o.transform.SetParent(parent ?? group,false); o.transform.localPosition=p; o.transform.localScale=scale;
            o.GetComponent<Renderer>().sharedMaterial=material;
            if (!collision) UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
            return o;
        }
        static GameObject Piece(string id, Vector3 p, float yaw=0, Transform parent=null)
        {
            var item=KitLibraryBuilder.Load().Find(id);
            var o=(GameObject)PrefabUtility.InstantiatePrefab(item.prefab);
            o.transform.SetParent(parent ?? group,false); o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
            PrefabUtility.RecordPrefabInstancePropertyModifications(o.transform); return o;
        }
        static void Text(string text, Vector3 p, float size=.13f, float yaw=0, Color? color=null, Transform parent=null)
        {
            var o = new GameObject(text); o.transform.SetParent(parent ?? group,false);
            o.transform.localPosition=p; o.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var t=o.AddComponent<TextMesh>(); t.text=text; t.font=font; t.fontSize=48; t.characterSize=size;
            t.anchor=TextAnchor.MiddleCenter; t.alignment=TextAlignment.Center; t.color=color ?? new Color(.86f,.85f,.65f);
            o.GetComponent<Renderer>().sharedMaterial=font.material;
            o.AddComponent<WorldTextLabel>().depthShader=Shader.Find("Hakaton/World Text");
        }
        static void Sign(string text, Vector3 p, float width=3, float yaw=0)
        {
            var sign=Group("Табличка · " + text, group); sign.localPosition=p; sign.localRotation=Quaternion.Euler(0,yaw,0);
            Box("Синяя эмаль",Vector3.zero,new Vector3(width,.55f,.06f),navy,false,sign);
            Text(text,new Vector3(0,0,-.04f),.11f,0,null,sign);
        }
        static void Wall(float x,float z,float length,bool alongZ=false)
        {
            var p=new Vector3(x,1.5f,z); var scale=alongZ?new Vector3(.22f,3,length):new Vector3(length,3,.22f);
            Box("Штукатурка · стена",p,scale,plaster,true);
            scale.y=1.12f; p.y=.56f; if(alongZ)scale.x=.25f;else scale.z=.25f;
            Box("Зелёная панель",p,scale,green,true);
            scale.y=.08f; p.y=1.15f; Box("Деревянный пояс",p,scale,wood,false);
        }
        static void PartitionX(float x,float z0,float z1,float doorZ,string name)
        {
            Wall(x,(z0+doorZ-2)/2,doorZ-2-z0,true);
            Wall(x,(doorZ+2+z1)/2,z1-doorZ-2,true);
            var o=Piece("doorway",new Vector3(x,0,doorZ),90);
            o.name="Дверь · " + name;
            var door=o.GetComponentInChildren<DemoDoor>(); door.label=name;
            PrefabUtility.RecordPrefabInstancePropertyModifications(door);
            Sign(name,new Vector3(x<0?x+.16f:x-.16f,2.65f,doorZ),3,x<0?90:-90);
        }
        static void PartitionZ(float z,float x0,float x1,float doorX,string name)
        {
            Wall((x0+doorX-2)/2,z,doorX-2-x0);
            Wall((doorX+2+x1)/2,z,x1-doorX-2);
            var o=Piece("doorway",new Vector3(doorX,0,z)); o.name="Дверь · "+name;
            var door=o.GetComponentInChildren<DemoDoor>(); door.label=name; PrefabUtility.RecordPrefabInstancePropertyModifications(door);
            Sign(name,new Vector3(doorX,2.65f,z-.16f));
        }
        static void Tube(Vector3 p, bool warm=false)
        {
            Box("Корпус светильника",p,new Vector3(1.9f,.08f,.5f),steel,false);
            foreach(float z in new[]{-.13f,.13f})Box("Люминесцентная трубка",p+new Vector3(0,-.055f,z),new Vector3(1.7f,.06f,.08f),glow,false);
            var l=new GameObject("Свет · люминесцентный").AddComponent<Light>(); l.transform.SetParent(group,false); l.transform.localPosition=p-Vector3.up*.18f;
            l.type=LightType.Point; l.color=warm?new Color(1,.79f,.46f):new Color(.76f,.9f,.78f); l.intensity=2.1f; l.range=7.8f; l.shadows=LightShadows.None;
        }
        static void Shell()
        {
            group=Group("01 · Корпус участка 40×40",root.architecture);
            for(int x=-18;x<20;x+=4)for(int z=2;z<40;z+=4)
            {
                Piece("floor_tile",new Vector3(x,0,z)); Piece("ceiling",new Vector3(x,0,z));
            }
            Wall(-20,20,40,true);Wall(20,20,40,true);Wall(0,40,40);
            Wall(-11,0,18);Wall(11,0,18);
            var entrance=Piece("doorway",Vector3.zero); entrance.name="Главный вход";
            var door=entrance.GetComponentInChildren<DemoDoor>();door.label="Главный вход"; PrefabUtility.RecordPrefabInstancePropertyModifications(door);
            PartitionX(-3,8,24,14,"ДЕТЕКТИВЫ"); PartitionX(3,8,24,14,"ОПЕРАТИВНЫЙ ЗАЛ");
            PartitionX(-3,24,40,30,"ХРАНИЛИЩЕ / КАМЕРЫ"); PartitionX(3,24,40,30,"АРХИВ · 1990—1999");
            PartitionZ(8,-20,-3,-12,"ДЕТЕКТИВНЫЙ ОТДЕЛ");PartitionZ(8,3,20,12,"СЛУЖЕБНЫЙ ВХОД");
            PartitionZ(24,-20,-3,-12,"ДЕЖУРНАЯ ЧАСТЬ");PartitionZ(24,3,20,12,"АРХИВ");
            PartitionX(-12,24,40,30,"КАМЕРЫ");
            PartitionZ(16,-20,-12,-16,"ДОПРОСНАЯ"); PartitionX(-12,16,24,20,"ДОПРОСНАЯ");
            var shelter=Piece("shelter",new Vector3(0,0,20)); shelter.name="Весь участок · укрытие от дождя";
            var volume=shelter.GetComponent<BoxCollider>();volume.center=new Vector3(0,1.5f,0);volume.size=new Vector3(39.7f,3,39.7f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(volume);
            // Open, well-lit central circulation spine.
            for(int z=5;z<40;z+=7)Tube(new Vector3(0,2.85f,z));
            for(int z=3;z<40;z+=7)for(int x=-15;x<20;x+=10)Tube(new Vector3(x,2.85f,z),z>26);
            Sign("← ДЕТЕКТИВЫ      АРХИВ →",new Vector3(0,2.5f,23),5);
            Sign("АРХИВ  →",new Vector3(0,2.5f,36.8f),3);
            // Skirting lines keep a readable path at the low internal resolution.
            for(int z=9;z<39;z+=4)Box("Линия коридора",new Vector3(2.2f,.008f,z),new Vector3(.12f,.016f,2),brass,false);
        }
        static void Desk(float x,float z, bool computer=true)
        {
            Piece("table",new Vector3(x,0,z));Piece("chair",new Vector3(x,0,z+1.0f),180);
            Piece("phone",new Vector3(x+.55f,.88f,z));
            Box("Папка",new Vector3(x-.45f,.9f,z-.12f),new Vector3(.42f,.035f,.31f),paper,false);
            if(computer)
            {
                Box("CRT-монитор",new Vector3(x,.88f+.23f,z-.18f),new Vector3(.55f,.46f,.42f),steel);
                Box("Монохромный экран",new Vector3(x,1.13f,z+.038f),new Vector3(.43f,.31f,.012f),green,false);
                Box("Клавиатура",new Vector3(x,.92f,z+.27f),new Vector3(.52f,.045f,.18f),black,false);
            }
        }
        static void Interior()
        {
            group=Group("02 · Приёмная и отделы");
            Box("Стойка дежурного",new Vector3(10,.6f,4.7f),new Vector3(9,1.2f,1.1f),green);
            Box("Деревянная столешница",new Vector3(10,1.22f,4.7f),new Vector3(9.3f,.12f,1.3f),wood);
            Sign("ДЕЖУРНЫЙ / RECEPTION",new Vector3(10,1,4.11f),4);
            Piece("phone",new Vector3(8,1.28f,4.7f));Piece("desk_lamp",new Vector3(12,1.28f,4.8f));
            Text("POLICE DEPARTMENT",new Vector3(10,2.1f,7.81f),.18f);
            for(int i=0;i<5;i++)Piece("chair",new Vector3(-15+i*2,0,3.2f),0);
            Piece("sofa",new Vector3(-17,0,6),90);
            Box("Кофейный автомат",new Vector3(-18.8f,.95f,1.5f),new Vector3(1,1.9f,.8f),navy);
            Text("COFFEE\n25¢",new Vector3(-18.8f,1.35f,1.08f),.12f);
            Box("Доска объявлений",new Vector3(-9,1.8f,7.85f),new Vector3(4,1.5f,.08f),wood,false);
            for(int i=0;i<7;i++)Box("Объявление",new Vector3(-10.5f+i*.48f,1.8f+(i%2)*.23f,7.79f),new Vector3(.34f,.48f,.02f),paper,false);
            Text("ПРИЁМ ГРАЖДАН  08:00 — 20:00",new Vector3(-9,2.7f,7.75f),.10f);
            for(int z=11;z<=21;z+=5)for(int x=7;x<=17;x+=5)Desk(x,z);
            foreach(float x in new[]{-17f,-11f,-6f})Desk(x,11);
            Desk(-7,18);Desk(-7,22);
            for(int i=0;i<5;i++)Piece("cabinet",new Vector3(19,0,10+i*2.6f),-90);
            for(int i=0;i<4;i++)Piece("cabinet",new Vector3(-19,0,10+i*1.6f),90);
            group=Group("03 · Допросная и хранение");
            Piece("table",new Vector3(-16,0,20));Piece("chair",new Vector3(-16,0,18.8f));Piece("chair",new Vector3(-16,0,21.2f),180);
            Box("Зеркало наблюдения",new Vector3(-19.85f,1.7f,20),new Vector3(.04f,1.2f,2.5f),black);
            for(int z=27;z<=37;z+=5)
            {
                Piece("cabinet",new Vector3(-7,0,z));Piece("crate",new Vector3(-9,0,z));
            }
            // Two open-front holding cells, contained inside the west wing.
            for(int z=32;z<=36;z+=4)
            {
                for(float x=-19.6f;x<-12.4f;x+=.45f)Box("Решётка камеры",new Vector3(x,1.45f,z),new Vector3(.055f,2.9f,.055f),steel);
                Box("Скамья камеры",new Vector3(-18,.45f,z-2),new Vector3(2.7f,.2f,.7f),wood);
            }
            Sign("ВЕЩЕСТВЕННЫЕ ДОКАЗАТЕЛЬСТВА",new Vector3(-7,2.5f,39.8f),7);
        }
        static void Shelf(float x,float z)
        {
            var shelf=Group("Стеллаж архива",group);shelf.position=new Vector3(x,0,z);
            foreach(float dx in new[]{-1.4f,1.4f})Box("Стойка",new Vector3(dx,1.25f,0),new Vector3(.09f,2.5f,.65f),steel,true,shelf);
            for(int level=0;level<4;level++)
            {
                float y=.25f+level*.58f;
                Box("Полка",new Vector3(0,y,0),new Vector3(2.9f,.08f,.7f),steel,true,shelf);
                for(int i=0;i<6;i++)
                {
                    Box("Дело · короб",new Vector3(-1.14f+i*.46f,y+.23f,0),new Vector3(.39f,.39f,.58f),i%3==0?red:paper,true,shelf);
                    Box("Ярлык",new Vector3(-1.14f+i*.46f,y+.23f,-.295f),new Vector3(.21f,.12f,.01f),plaster,false,shelf);
                }
            }
        }
        static void Document(int id,Vector3 p,string caption)
        {
            var o=Box("Улика · "+DemoGame.EvidenceTitles[id],p,new Vector3(.58f,.035f,.42f),paper);
            var clue=o.AddComponent<DemoInteractable>();clue.kind=DemoInteractable.Interaction.Evidence;clue.evidenceId=id;clue.label="Осмотреть · "+DemoGame.EvidenceTitles[id];
            string path=Art+"/Case_document_"+id+".asset";
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(t==null)
            {
                t=new Texture2D(128,128,TextureFormat.RGB24,false){name=caption,filterMode=FilterMode.Point};
                for(int y=0;y<128;y++)for(int x=0;x<128;x++)
                {
                    Color c=new Color(.78f,.75f,.6f);
                    if(x>14&&x<114&&y>18&&y<104&&y%9<2)c=new Color(.3f,.32f,.27f);
                    if(y>107&&y<116&&x>14&&x<88)c=new Color(.42f,.13f,.09f);
                    t.SetPixel(x,y,c);
                }
                t.Apply();AssetDatabase.CreateAsset(t,path);
            }
            clue.illustration=t;
            var piece=o.AddComponent<LocationPiece>();piece.kitId="station_case_"+id;piece.category=KitCategory.Gameplay;piece.paintTargets=new[]{o.GetComponent<Renderer>()};
            piece.localBounds=new Bounds(Vector3.zero,Vector3.one);
            var label=Group("Надпись на деле",group);label.position=p+Vector3.up*.022f;
            label.rotation=Quaternion.Euler(90,0,0);
            Text(caption,Vector3.zero,.035f,0,new Color(.17f,.17f,.13f),label);
        }
        static void Archive()
        {
            group=Group("04 · Архив · дело отца В-97/041");
            foreach(float x in new[]{7f,12f,17f})foreach(float z in new[]{34f,37.7f})Shelf(x,z);
            for(int i=0;i<6;i++)Piece("cabinet",new Vector3(19.1f,0,25.5f+i*1.3f),-90);
            Piece("table",new Vector3(7,0,27));Piece("table",new Vector3(10,0,27));Piece("table",new Vector3(13,0,27));
            Piece("chair",new Vector3(7,0,28.3f),180);Piece("desk_lamp",new Vector3(6.3f,.88f,27.1f));
            Sign("ДЕЛО В-97/041 · ВОЛКОВ",new Vector3(10,2.15f,24.18f),6,180);
            Sign("1997 / ЗАКРЫТЫЕ ДЕЛА",new Vector3(12,2.65f,39.8f),6);
            Document(3,new Vector3(7,.91f,27),"ПРОТОКОЛ\n16.10.1997");
            Document(4,new Vector3(10,.91f,27),"ОРУЖИЕ\n.45 ACP");
            Document(5,new Vector3(13,.91f,27),"ЭКСПЕРТИЗА\n9 × 19 мм");
            Box("Красная папка дела отца",new Vector3(10,.895f,27),new Vector3(1.1f,.02f,.65f),red,false);
            // A separate sealed exhibit beside the weapon inventory.
            Box("Опечатанный лоток",new Vector3(10,.94f,27.45f),new Vector3(.7f,.07f,.25f),steel);
            Box("Пистолет · затвор",new Vector3(10,1.005f,27.45f),new Vector3(.35f,.08f,.065f),black,false);
            var grip=Box("Пистолет · рукоять",new Vector3(10.12f,.99f,27.36f),new Vector3(.08f,.075f,.2f),wood,false);grip.transform.Rotate(0,12,0);
            Text("ОПЕЧАТАНО · .45 ACP",new Vector3(10,1.19f,27.56f),.05f);
        }
        static void Exterior()
        {
            group=Group("05 · Фасад и парковка");
            var asphalt=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Art/Arrival/Wet_asphalt.mat");
            Box("Парковка",new Vector3(0,-.22f,8),new Vector3(78,.4f,88),asphalt);
            Box("Тротуар",new Vector3(0,-.08f,-2),new Vector3(44,.14f,3),plaster);
            Box("Козырёк входа",new Vector3(0,3.3f,-2),new Vector3(9,.25f,5),navy);
            foreach(float x in new[]{-4f,4f})Box("Колонна",new Vector3(x,1.55f,-3.5f),new Vector3(.35f,3.1f,.35f),plaster);
            Sign("ПОЛИЦЕЙСКИЙ УЧАСТОК",new Vector3(0,3.8f,-.3f),15);
            Text("POLICE  /  COUNTY PRECINCT",new Vector3(0,4.4f,-.35f),.16f);
            Box("Фасад · парапет",new Vector3(0,3.55f,0),new Vector3(40,1.1f,.4f),green);
            for(int x=-16;x<=16;x+=8)
            {
                if(x==0)continue;
                Box("Фасад · окно",new Vector3(x,1.75f,-.15f),new Vector3(3,1.2f,.1f),navy);
                Box("Рама окна",new Vector3(x,1.75f,-.22f),new Vector3(.07f,1.3f,.08f),steel,false);
                Box("Рама окна",new Vector3(x,1.75f,-.22f),new Vector3(3.1f,.07f,.08f),steel,false);
            }
            for(int x=-26;x<=26;x+=8)
            {
                Box("Разметка",new Vector3(x,-.006f,-13),new Vector3(.12f,.015f,6),paper,false);
                if(x!= -2)Piece("car",new Vector3(x+2,0,-14),0);
            }
            Tube(new Vector3(0,3.04f,-2));
            foreach(float x in new[]{-26f,26f})
            {
                Box("Столб фонаря",new Vector3(x,2.5f,-9),new Vector3(.16f,5,.16f),steel);
                Tube(new Vector3(x,4.8f,-9));
            }
            for(int i=0;i<5;i++)Piece("tree",new Vector3(-29,0,3+i*9));
        }
        static Transform Joint(Transform parent,string name,Vector3 p)
        {var t=Group(name,parent);t.localPosition=p;return t;}
        static StationNpc Npc(string role,Vector3 p,float yaw,bool seated=false,bool civilian=false)
        {
            var o=Group("NPC · "+role,group);o.position=p;o.rotation=Quaternion.Euler(0,yaw,0);
            var npc=o.gameObject.AddComponent<StationNpc>();npc.role=role;npc.seated=seated;npc.phase=npcCount++*1.7f;
            float hip=seated?.65f:.92f; Material top=civilian?shirt:uniform;
            Box("Туловище",new Vector3(0,hip+.31f,0),new Vector3(.48f,.57f,.27f),top,false,o);
            Box("Ремень",new Vector3(0,hip+.04f,0),new Vector3(.5f,.08f,.29f),black,false,o);
            Box("Галстук",new Vector3(0,hip+.31f,.145f),new Vector3(.055f,.33f,.02f),black,false,o);
            if(!civilian)Box("Жетон",new Vector3(-.13f,hip+.43f,.15f),new Vector3(.075f,.09f,.02f),brass,false,o);
            npc.head=Joint(o,"Голова · поворот",new Vector3(0,hip+.78f,0));
            var head=Box("Голова",Vector3.zero,new Vector3(.3f,.35f,.29f),skin,false,npc.head);
            Box("Волосы",new Vector3(0,.16f,-.025f),new Vector3(.32f,.1f,.27f),npcCount%3==0?wood:black,false,npc.head);
            foreach(float x in new[]{-.073f,.073f})Box("Глаза",new Vector3(x,.025f,.151f),new Vector3(.035f,.025f,.012f),black,false,npc.head);
            Box("Нос",new Vector3(0,-.015f,.17f),new Vector3(.055f,.08f,.06f),skin,false,npc.head);
            npc.leftArm=Joint(o,"Левая рука",new Vector3(-.3f,hip+.53f,0));
            npc.rightArm=Joint(o,"Правая рука",new Vector3(.3f,hip+.53f,0));
            foreach(var arm in new[]{npc.leftArm,npc.rightArm})
            {Box("Рукав",new Vector3(0,-.23f,0),new Vector3(.16f,.46f,.18f),top,false,arm);Box("Кисть",new Vector3(0,-.51f,0),new Vector3(.13f,.15f,.14f),skin,false,arm);}
            npc.leftLeg=Joint(o,"Левая нога",new Vector3(-.135f,hip,0));npc.rightLeg=Joint(o,"Правая нога",new Vector3(.135f,hip,0));
            foreach(var leg in new[]{npc.leftLeg,npc.rightLeg})
            {
                if(seated)
                {Box("Бедро",new Vector3(0,-.08f,.2f),new Vector3(.2f,.2f,.48f),trousers,false,leg);Box("Голень",new Vector3(0,-.36f,.39f),new Vector3(.18f,.47f,.18f),trousers,false,leg);Box("Ботинок",new Vector3(0,-.61f,.45f),new Vector3(.22f,.12f,.34f),black,false,leg);}
                else
                {Box("Штанина",new Vector3(0,-.39f,0),new Vector3(.21f,.77f,.22f),trousers,false,leg);Box("Ботинок",new Vector3(0,-.85f,.05f),new Vector3(.23f,.14f,.35f),black,false,leg);}
            }
            var collider=o.gameObject.AddComponent<CapsuleCollider>();collider.radius=.29f;collider.height=seated?1.45f:1.85f;collider.center=new Vector3(0,collider.height/2,0);
            return npc;
        }
        static void People()
        {
            npcCount=0;group=Group("06 · Сотрудники и посетители");
            Npc("Дежурный",new Vector3(8,0,6),180);Npc("Диспетчер",new Vector3(12,0,6),180);
            for(int i=0;i<4;i++)Npc("Посетитель",new Vector3(-15+i*2,0,3.2f),0,true,true);
            for(int z=11;z<=21;z+=5)for(int x=7;x<=17;x+=5)Npc("Оперативник",new Vector3(x,0,z+1),180,true,(x+z)%2==0);
            foreach(float x in new[]{-17f,-11f,-6f})Npc("Детектив",new Vector3(x,0,12),180,true,true);
            Npc("Следователь",new Vector3(-7,0,19),180,true,true);Npc("Следователь",new Vector3(-7,0,23),180,true,true);
            Npc("Сотрудник допросной",new Vector3(-16,0,21.2f),180,true);Npc("Задержанный",new Vector3(-16,0,18.8f),0,true,true);
            Npc("Архивариус",new Vector3(17,0,31),-90);Npc("Кладовщик",new Vector3(-6,0,33),90);
            Npc("Охранник",new Vector3(-5,0,5),110);Npc("Сотрудник у стойки",new Vector3(13,0,2.8f),0);
            for(int i=0;i<4;i++)
            {
                float x=i%2==0?-1.25f:1.25f;float z=10+i*6;
                var n=Npc("Патруль коридора",new Vector3(x,0,z),0);
                n.route=new[]{new Vector3(x,0,10),new Vector3(x,0,36)};n.speed=.65f+i*.035f;
            }
            Npc("Полицейский на парковке",new Vector3(-9,0,-7),40);Npc("Полицейский у входа",new Vector3(5.2f,0,-2.3f),-40);
        }
    }
}
