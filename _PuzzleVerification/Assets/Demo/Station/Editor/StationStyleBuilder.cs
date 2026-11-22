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
namespace Hakaton.Demo.Station.Editor
{
    public static class StationStyleBuilder
    {
        const string Art="Assets/Demo/Station/Art";
        const string Marker="07 · Обжитой участок · детали";
        static LocationRoot location;
        static Transform root,group;
        static Scene menuSource;
        static Font font;
        static Mesh cylinder;
        static Material plaster,wood,trim,paper,ink,cream,steel,sign,vinyl,tile,glow,printed,red;
        static System.Random random;
        static readonly List<Transform> fans=new List<Transform>();
        static readonly List<Transform> hands=new List<Transform>();
        static readonly Dictionary<string,Material> remap=new Dictionary<string,Material>();
        [MenuItem("Hakaton/Участок/Стиль кабинета и детали")]
        public static string Apply()
        {
            if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Остановите Play и сохраните текущую сцену.");
            if(SceneManager.GetActiveScene().name!="PoliceStation")EditorSceneManager.OpenScene("Assets/Scenes/PoliceStation.unity");
            if(GameObject.Find(Marker)!=null)return "Участок уже оформлен. Детали находятся в группе «"+Marker+"».";
            location=UnityEngine.Object.FindAnyObjectByType<LocationRoot>();font=location.game.hud.font;random=new System.Random(2041);fans.Clear();hands.Clear();remap.Clear();
            menuSource=EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainMenu.unity");
            try
            {
                root=new GameObject(Marker).transform;root.SetParent(location.props,false);
                Materials();ReplaceOuterWalls();Lobby();Workrooms();InterviewAndStorage();Archive();Corridor();
                StyleExisting();Environment();
                var ambience=root.gameObject.AddComponent<StationAmbience>();ambience.fans=fans.ToArray();ambience.clockHands=hands.ToArray();
                LocationKitOperations.RefreshConnections(location);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
                Selection.activeGameObject=root.gameObject;
                return "Оформлено: "+fans.Count+" вентиляторов, "+hands.Count+" часов, окна с жалюзи, рабочие места и архив.";
            }
            finally{EditorSceneManager.ClosePreviewScene(menuSource);}
        }
        static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent??root,false);return t;}
        static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,Transform parent=null,bool collision=false)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent??group,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collision)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;
        }
        static GameObject Cylinder(string name,Vector3 p,Vector3 size,Material mat,Transform parent=null)
        {
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent??group,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<MeshFilter>().sharedMesh=cylinder;o.GetComponent<Renderer>().sharedMaterial=mat;return o;
        }
        static GameObject Kit(string id,Vector3 p,float yaw=0)
        {
            var o=(GameObject)PrefabUtility.InstantiatePrefab(KitLibraryBuilder.Load().Find(id).prefab);o.transform.SetParent(group,false);o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));PrefabUtility.RecordPrefabInstancePropertyModifications(o.transform);return o;
        }
        static GameObject MenuProp(string name,Vector3 p,float yaw=0,Transform parent=null)
        {
            var source=menuSource.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).First(t=>t.name==name);
            var o=UnityEngine.Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(o,SceneManager.GetActiveScene());o.transform.SetParent(parent??group,false);o.transform.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));return o;
        }
        static Material Mat(string name,Color color,string template=null)
        {
            string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            var source=template==null?null:AssetDatabase.LoadAssetAtPath<Material>(template);m=source==null?new Material(Shader.Find("Universal Render Pipeline/Lit")):new Material(source);m.name=name;m.color=color;m.SetFloat("_Smoothness",.17f);AssetDatabase.CreateAsset(m,path);return m;
        }
        static void Materials()
        {
            plaster=Mat("Warm_plaster",new Color(.59f,.53f,.42f),"Assets/Demo/Menu/Art/Office_plaster.mat");plaster.mainTextureScale=new Vector2(2,2);
            wood=Mat("Wood_wainscot",new Color(.32f,.22f,.12f),"Assets/Demo/Menu/Art/Desk_walnut.mat");wood.mainTextureScale=new Vector2(2,2);
            trim=Mat("Wood_trim",new Color(.16f,.11f,.07f),"Assets/Demo/Menu/Art/Wood_trim.mat");
            paper=Mat("Case_paper",new Color(.73f,.65f,.45f));ink=Mat("Ink",new Color(.04f,.038f,.031f));cream=Mat("Old_ivory",new Color(.73f,.68f,.54f));
            steel=Mat("Filing_enamel",new Color(.29f,.33f,.27f));sign=Mat("Bronze_sign",new Color(.12f,.105f,.07f));red=Mat("Archive_red",new Color(.35f,.09f,.06f));
            glow=Mat("Fluorescent_cream",new Color(.9f,.86f,.69f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1,.82f,.51f)*1.35f);
            vinyl=Mat("Worn_vinyl",new Color(.53f,.48f,.37f));
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/VinylTexture.asset");
            if(texture==null)
            {
                texture=new Texture2D(64,64,TextureFormat.RGB24,false){name="Quiet worn linoleum",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
                var rng=new System.Random(103);
                for(int y=0;y<64;y++)for(int x=0;x<64;x++){float c=.83f+(float)rng.NextDouble()*.12f;if(x%32==0||y%32==0)c*=.84f;texture.SetPixel(x,y,new Color(c,c*.98f,c*.92f));}
                texture.Apply();AssetDatabase.CreateAsset(texture,Art+"/VinylTexture.asset");
            }
            vinyl.mainTexture=texture;vinyl.mainTextureScale=new Vector2(2,2);
            tile=Mat("Acoustic_ceiling",new Color(.64f,.61f,.51f),"Assets/Demo/Menu/Art/Office_plaster.mat");tile.mainTextureScale=new Vector2(2,2);
            printed=Mat("Printed_case_sheet",new Color(.9f,.86f,.72f));printed.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Demo/Art/Station/Case_document_3.asset");
            cylinder=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Demo/Art/Generated/Cylinder8.asset");
            remap["Checker linoleum"]=vinyl;remap["Institutional_plaster"]=plaster;remap["Striped wallpaper"]=plaster;
            remap["Police_green"]=wood;remap["Dark walnut"]=wood;remap["Ceiling"]=tile;remap["Sign_navy"]=sign;
            remap["Fluorescent"]=glow;remap["Kitchen green"]=steel;remap["Dull enamel"]=steel;remap["Filing_steel"]=steel;
        }
        static void Label(string text,Vector3 p,float size=.025f,float yaw=0,Transform parent=null)
        {
            var t=Group(text,parent??group);t.localPosition=p;t.localRotation=Quaternion.Euler(0,yaw,0);
            var m=t.gameObject.AddComponent<TextMesh>();m.text=text;m.font=font;m.fontSize=48;m.characterSize=size;m.anchor=TextAnchor.MiddleCenter;m.alignment=TextAlignment.Center;m.color=new Color(.78f,.7f,.49f);
            t.gameObject.AddComponent<WorldTextLabel>().depthShader=Shader.Find("Hakaton/World Text");
        }
        static void Window(Vector3 p,float yaw)
        {
            var t=Group("Окно с жалюзи",group);t.position=p;t.rotation=Quaternion.Euler(0,yaw,0);
            foreach(float x in new[]{-1.25f,1.25f})Box("Рама окна",new Vector3(x,1.92f,-.10f),new Vector3(.10f,1.55f,.16f),trim,t);
            foreach(float y in new[]{1.2f,2.68f})Box("Подоконник и верх рамы",new Vector3(0,y,-.12f),new Vector3(2.6f,.10f,.32f),wood,t);
            for(int i=0;i<17;i++){var slat=Box("Ламель жалюзи",new Vector3(0,1.29f+i*.079f,-.08f),new Vector3(2.4f,.025f,.10f),trim,t);slat.transform.Rotate(-17,0,0);}
            foreach(float x in new[]{-.83f,.83f})Box("Шнур жалюзи",new Vector3(x,1.94f,-.16f),new Vector3(.012f,1.43f,.012f),paper,t);
            var glass=t.gameObject.AddComponent<BoxCollider>();glass.center=new Vector3(0,1.94f,0);glass.size=new Vector3(2.4f,1.45f,.06f);
        }
        static void ReplaceOuterWalls()
        {
            group=Group("01 · Настоящие окна и обшивка");
            foreach(var renderer in location.architecture.GetComponentsInChildren<Renderer>().ToArray())
            {
                Vector3 p=renderer.transform.position;
                if(renderer.name=="Штукатурка · стена"&&(Mathf.Abs(Mathf.Abs(p.x)-20)<.1f||Mathf.Abs(p.z)<.1f))
                {renderer.enabled=false;var c=renderer.GetComponent<Collider>();if(c!=null)c.enabled=false;}
            }
            foreach(var renderer in location.props.GetComponentsInChildren<Renderer>())
                if(renderer.name=="Фасад · окно"||renderer.name=="Рама окна")renderer.enabled=false;
            foreach(float x in new[]{-20f,20f})
            {
                Box("Стена над окнами",new Vector3(x,2.85f,20),new Vector3(.22f,.3f,40),plaster,collision:true);
                for(int i=0;i<=10;i++)
                {
                    float z=i==0?.4f:i==10?39.6f:i*4f;float length=i==0||i==10?.8f:1.6f;
                    Box("Простенок",new Vector3(x,1.95f,z),new Vector3(.22f,1.5f,length),plaster,collision:true);
                }
                for(int z=2;z<40;z+=4)Window(new Vector3(x,0,z),x<0?-90:90);
            }
            foreach(float side in new[]{-1f,1f})
            {
                Box("Фасад · перемычка",new Vector3(side*11,2.85f,0),new Vector3(18,.3f,.22f),plaster,collision:true);
                float[] ends={2,6.75f,9.25f,14.75f,17.25f,20};
                for(int i=0;i<ends.Length;i+=2)Box("Фасад · простенок",new Vector3(side*(ends[i]+ends[i+1])/2,1.95f,0),new Vector3(ends[i+1]-ends[i],1.5f,.22f),plaster,collision:true);
                Window(new Vector3(side*8,0,0),180);Window(new Vector3(side*16,0,0),180);
            }
            // Raised panel moulding follows solid walls and keeps door openings clear.
            foreach(float x in new[]{-19.82f,19.82f})for(float z=.5f;z<40;z+=1.5f)Box("Рейка деревянной панели",new Vector3(x,.57f,z),new Vector3(.04f,1.05f,.045f),trim);
        }
        static void Stack(Vector3 p,float yaw=0,float scale=1,int sheets=5)
        {
            var t=Group("Стопка материалов дела",group);t.position=p;t.rotation=Quaternion.Euler(0,yaw,0);t.localScale=Vector3.one*scale;
            for(int i=0;i<sheets;i++){var sheet=Box("Бумажный лист",new Vector3((float)random.NextDouble()*.04f,i*.008f,0),new Vector3(.4f,.008f,.30f),i==0?wood:paper,t);sheet.transform.Rotate(0,(float)random.NextDouble()*14-7,0);}
            var page=GameObject.CreatePrimitive(PrimitiveType.Quad);page.name="Напечатанный документ";page.transform.SetParent(t,false);page.transform.localPosition=new Vector3(0,sheets*.008f,0);page.transform.localRotation=Quaternion.Euler(90,0,0);page.transform.localScale=new Vector3(.36f,.28f,1);page.GetComponent<Renderer>().sharedMaterial=printed;UnityEngine.Object.DestroyImmediate(page.GetComponent<Collider>());
        }
        static void Mug(Vector3 p,float yaw=0)=>MenuProp("Кружка POLICE",p,yaw);
        static void Lamp(Vector3 p,float yaw=0)
        {
            var lamp=MenuProp("Настольная лампа",p,yaw);lamp.transform.localScale=Vector3.one*.82f;
            foreach(var light in lamp.GetComponentsInChildren<Light>()){light.intensity=1.25f;light.range=3.7f;}
        }
        static void Fan(Vector3 p)
        {
            var o=MenuProp("Вращающийся потолочный вентилятор",p);o.name="Потолочный вентилятор";fans.Add(o.transform);
            Cylinder("Подвес вентилятора",p+Vector3.up*.17f,new Vector3(.065f,.35f,.065f),trim);
        }
        static void Clock(Vector3 p,float yaw=0)
        {
            var o=MenuProp("Настенные часы",p,yaw);hands.Add(o.transform.Find("Секундная стрелка"));
        }
        static void Board(Vector3 p,float yaw,string title)
        {
            var t=Group("Доска · "+title,group);t.position=p;t.rotation=Quaternion.Euler(0,yaw,0);
            Box("Рама",Vector3.zero,new Vector3(2.5f,1.25f,.10f),trim,t);Box("Пробка",new Vector3(0,0,-.06f),new Vector3(2.35f,1.1f,.035f),wood,t);
            for(int i=0;i<9;i++)
            {
                float x=-.93f+(i%5)*.46f,y=.27f-(i/5)*.49f;
                var page=Box("Материал на доске",new Vector3(x,y,-.095f),new Vector3(.32f,.40f,.012f),paper,t);page.transform.Rotate(0,0,(i%3-1)*7);
                Box("Кнопка",new Vector3(x,y+.16f,-.11f),new Vector3(.025f,.025f,.016f),red,t);
            }
            Label(title,new Vector3(0,.79f,-.08f),.036f,0,t);
        }
        static void Basket(Vector3 p)
        {
            var t=Group("Корзина для бумаг",group);t.position=p;
            Cylinder("Корзина",new Vector3(0,.27f,0),new Vector3(.38f,.54f,.38f),steel,t);
            Cylinder("Тёмный верх",new Vector3(0,.545f,0),new Vector3(.33f,.012f,.33f),ink,t);
            for(int i=0;i<3;i++)Box("Смятая бумага",new Vector3((i-1)*.08f,.55f,.03f),new Vector3(.08f,.05f,.08f),cream,t);
        }
        static void ReceptionStamp(Vector3 p)
        {
            Cylinder("Основание печати",p,new Vector3(.12f,.025f,.12f),ink);
            Cylinder("Рукоять печати",p+Vector3.up*.07f,new Vector3(.065f,.13f,.065f),wood);
        }
        static void Lobby()
        {
            group=Group("02 · Приёмная и ожидание");
            Stack(new Vector3(7.1f,1.29f,4.7f),-7,1.35f,8);Stack(new Vector3(10.1f,1.29f,4.7f),10,1.1f,6);Stack(new Vector3(13.5f,1.29f,4.7f),-5,1.3f,7);
            Mug(new Vector3(11.3f,1.29f,4.8f),-10);ReceptionStamp(new Vector3(9.3f,1.3f,4.5f));
            Box("Лоток входящих",new Vector3(14.1f,1.34f,4.8f),new Vector3(.65f,.10f,.36f),steel);Label("ВХОДЯЩИЕ",new Vector3(14.1f,1.44f,4.6f),.018f);
            MenuProp("Пишущая машинка",new Vector3(8.7f,1.29f,4.9f));
            Board(new Vector3(17.7f,1.85f,7.82f),0,"НАЙДЕНЫЕ ЛЮДИ / ОБЪЯВЛЕНИЯ");
            Clock(new Vector3(3.8f,2.45f,7.79f));Fan(new Vector3(0,2.65f,4));
            var coffee=Kit("table",new Vector3(-17.7f,0,1.2f));coffee.name="Столик у кофейного автомата";coffee.transform.localScale=new Vector3(.6f,1,.85f);PrefabUtility.RecordPrefabInstancePropertyModifications(coffee.transform);
            for(int i=0;i<3;i++)Mug(new Vector3(-18.1f+i*.3f,.89f,1.2f),i*28);
            Cylinder("Кофейник",new Vector3(-17.9f,1.1f,1.45f),new Vector3(.23f,.42f,.23f),steel);Basket(new Vector3(-16.7f,0,.8f));
            var magazines=Kit("bookcase",new Vector3(-18.8f,0,5.3f),90);magazines.name="Журналы для посетителей";magazines.transform.localScale=Vector3.one*.65f;PrefabUtility.RecordPrefabInstancePropertyModifications(magazines.transform);
            Stack(new Vector3(-17.2f,.80f,6),-12,.75f,4);
            for(int i=0;i<5;i++)Box("Одежда на вешалке",new Vector3(-4.5f,1.2f,1.5f+i*.3f),new Vector3(.17f,.75f,.25f),i%2==0?wood:steel);
            Box("Стойка вешалки",new Vector3(-4.5f,.95f,2.1f),new Vector3(.07f,1.9f,1.6f),trim,collision:true);
        }
        static void DeskDetail(Vector3 p,int index)
        {
            if(index==2||index==10)
            {
                var ashtray=MenuProp("Пепельница с тлеющей сигарой",p+new Vector3(-.55f,.89f,.20f));ashtray.transform.localScale=Vector3.one*.55f;
            }
            else Stack(p+new Vector3(-.50f,.89f,.12f),index*7,.8f,4+index%4);
            Mug(p+new Vector3(.68f,.89f,.32f),index*35);
            Cylinder("Стакан с карандашами",p+new Vector3(.75f,.99f,-.34f),new Vector3(.13f,.21f,.13f),steel);
            for(int j=0;j<3;j++){var pencil=Box("Карандаш",p+new Vector3(.72f+j*.025f,1.16f,-.34f),new Vector3(.012f,.28f,.012f),paper);pencil.transform.Rotate(0,0,(j-1)*6);}
            if(index%2==0)Lamp(p+new Vector3(-.68f,.89f,-.23f),-10);
            if(index%3==0)Basket(p+new Vector3(1.25f,0,-.05f));
            Box("Записка у клавиатуры",p+new Vector3(-.30f,.886f,.32f),new Vector3(.13f,.004f,.10f),paper);
            var drawers=Kit("cabinet",p+new Vector3(.55f,0,0));drawers.name="Тумба под рабочим столом";drawers.transform.localScale=new Vector3(.55f,.55f,.65f);PrefabUtility.RecordPrefabInstancePropertyModifications(drawers.transform);
        }
        static void Workrooms()
        {
            group=Group("03 · Обжитые рабочие места");int index=0;
            for(int z=11;z<=21;z+=5)for(int x=7;x<=17;x+=5)DeskDetail(new Vector3(x,0,z),index++);
            foreach(float x in new[]{-17f,-11f,-6f})DeskDetail(new Vector3(x,0,11),index++);
            DeskDetail(new Vector3(-7,0,18),index++);DeskDetail(new Vector3(-7,0,22),index++);
            Board(new Vector3(7,1.9f,23.82f),0,"ОПЕРАТИВНАЯ СВОДКА");
            Copier(new Vector3(9.5f,0,13.5f));Copier(new Vector3(14.5f,0,18.5f));
            Board(new Vector3(-8.5f,1.9f,8.18f),180,"НЕЗАКРЫТЫЕ ДЕЛА");
            Clock(new Vector3(3.18f,2.5f,19),-90);Clock(new Vector3(-3.18f,2.5f,19),90);
            Fan(new Vector3(12,2.63f,15));Fan(new Vector3(-9,2.63f,12));Fan(new Vector3(-7,2.63f,21));
            var shelf=Kit("bookcase",new Vector3(17.8f,0,23.2f),180);shelf.name="Справочники и папки сотрудников";
            // Files on the top of the existing filing cabinets.
            for(int i=0;i<5;i++)Stack(new Vector3(19,1.37f,10+i*2.6f),i*6,1,4+i%3);
            for(int i=0;i<4;i++)Stack(new Vector3(-19,1.37f,10+i*1.6f),-i*11,.95f,6);
        }
        static void Copier(Vector3 p)
        {
            var t=Group("Копировальный аппарат",group);t.position=p;
            Box("Тумба копира",new Vector3(0,.39f,0),new Vector3(1.1f,.78f,.8f),steel,t,true);
            Box("Корпус копира",new Vector3(0,.88f,0),new Vector3(1.2f,.24f,.88f),cream,t);
            Box("Стекло сканера",new Vector3(-.12f,1.01f,.05f),new Vector3(.75f,.015f,.57f),ink,t);
            var lid=Box("Приподнятая крышка",new Vector3(-.12f,1.18f,.28f),new Vector3(.82f,.08f,.6f),cream,t);lid.transform.Rotate(-32,0,0);
            Box("Панель кнопок",new Vector3(.45f,1.02f,-.12f),new Vector3(.20f,.025f,.45f),ink,t);
            for(int i=0;i<3;i++)Box("Кнопка копира",new Vector3(.45f,1.04f,-.23f+i*.11f),new Vector3(.06f,.012f,.055f),paper,t);
            Box("Выходной лоток",new Vector3(-.6f,.8f,0),new Vector3(.42f,.04f,.55f),ink,t);
            Stack(p+new Vector3(-.62f,.83f,0),3,.6f,4);
        }
        static void InterviewAndStorage()
        {
            group=Group("04 · Допросная и хранилище");
            Stack(new Vector3(-16.55f,.89f,20.1f),4,.7f,3);Mug(new Vector3(-15.5f,.89f,20),-35);
            Box("Кассетный диктофон",new Vector3(-16,.94f,20.1f),new Vector3(.28f,.1f,.20f),ink);
            Box("Индикатор диктофона",new Vector3(-16.10f,1,20.01f),new Vector3(.025f,.016f,.015f),red);
            Board(new Vector3(-16,1.95f,23.82f),0,"ПРОТОКОЛЫ / ОПРОСЫ");
            for(int i=0;i<3;i++)
            {
                var locker=Kit("cabinet",new Vector3(-11.2f,0,new[]{26,34,37}[i]),90);locker.name="Шкаф личных вещей";
                Stack(new Vector3(-7,1.37f,27+i*5),i*4,1,6);
                Box("Опечатанная коробка",new Vector3(-9,.775f,27+i*5),new Vector3(.82f,.35f,.64f),paper,collision:true);
                Box("Лента пломбы",new Vector3(-9,.954f,27+i*5),new Vector3(.83f,.008f,.045f),cream);
                Box("Ярлык коробки",new Vector3(-9,.78f,26.675f+i*5),new Vector3(.23f,.10f,.015f),cream);
            }
            for(int i=0;i<2;i++)
            {
                Box("Матрас камеры",new Vector3(-18,.59f,30+i*4),new Vector3(2.5f,.10f,.65f),steel);
                Cylinder("Кружка задержанного",new Vector3(-17.5f,.73f,30+i*4),new Vector3(.15f,.2f,.15f),cream);
            }
        }
        static void Archive()
        {
            group=Group("05 · Архив · документы и рабочие детали");
            // Evidence on x=7,10,13 / z=27 remains uncovered and reachable from z=25.65.
            Lamp(new Vector3(6.25f,.89f,27.15f),12);Lamp(new Vector3(13.7f,.89f,27.2f),-14);
            Mug(new Vector3(16.4f,.89f,27.85f),0);
            Stack(new Vector3(16.2f,.89f,27.4f),4,.9f,8);
            var catalogDesk=Kit("table",new Vector3(16.8f,0,27.6f));catalogDesk.name="Стол архивариуса";
            Kit("chair",new Vector3(16.8f,0,28.7f));MenuProp("Пишущая машинка",new Vector3(17,.89f,27.5f));
            Board(new Vector3(16.8f,1.9f,24.18f),180,"УЧЁТ ВЫДАННЫХ ДЕЛ");
            Clock(new Vector3(19.78f,2.5f,31),90);Fan(new Vector3(12,2.63f,30.7f));
            for(int i=0;i<6;i++)
            {
                Stack(new Vector3(19.1f,1.37f,25.5f+i*1.3f),i*9,.8f,5);
                Box("Номер ящика",new Vector3(18.61f,1.12f,25.5f+i*1.3f),new Vector3(.012f,.09f,.20f),cream);
            }
            foreach(float x in new[]{7f,12f,17f})foreach(float z in new[]{34f,37.7f})
            {
                Box("Верхняя полка",new Vector3(x,2.47f,z),new Vector3(2.9f,.07f,.7f),steel);
                Label("199"+(int)(x%5)+" / ЗАКРЫТЫЕ",new Vector3(x,2.56f,z-.36f),.032f);
                for(int i=0;i<2;i++)Box("Короб на стеллаже",new Vector3(x-.65f+i*1.25f,2.64f,z),new Vector3(1.1f,.28f,.58f),paper);
            }
            Basket(new Vector3(18,0,29));
        }
        static void Corridor()
        {
            group=Group("06 · Коридоры и навигация");
            Board(new Vector3(2.82f,1.85f,20),90,"РОЗЫСК / СВОДКИ");Board(new Vector3(-2.82f,1.85f,20),-90,"ДЕЖУРСТВА");
            Clock(new Vector3(0,2.5f,39.77f));Fan(new Vector3(0,2.63f,18));Fan(new Vector3(0,2.63f,33));
            for(int z=10;z<40;z+=6)
            {
                Box("Рамка на стене коридора",new Vector3(2.82f,1.6f,z),new Vector3(.08f,.58f,.43f),trim);
                Box("Служебная памятка",new Vector3(2.765f,1.6f,z),new Vector3(.015f,.48f,.33f),paper);
            }
        }
        static void StyleExisting()
        {
            foreach(var renderer in location.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer is ParticleSystemRenderer)continue;
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)if(materials[i]!=null&&remap.TryGetValue(materials[i].name,out var replacement)){materials[i]=replacement;changed=true;}
                if(changed){renderer.sharedMaterials=materials;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
            }
            foreach(var text in location.GetComponentsInChildren<TextMesh>(true))
                if(!text.transform.IsChildOf(root)&&text.characterSize>.06f)text.characterSize*=.6f;
            // Correct the old chair facing: local -Z is the front of the library chair.
            var seated=location.GetComponentsInChildren<StationNpc>(true).Where(n=>n.seated).ToArray();
            foreach(var piece in location.GetComponentsInChildren<LocationPiece>())if(piece.kitId=="chair")
            {
                var npc=seated.FirstOrDefault(n=>Vector3.Distance(n.transform.position,piece.transform.position)<.25f);
                if(npc!=null){piece.transform.rotation=Quaternion.Euler(0,npc.transform.eulerAngles.y+180,0);PrefabUtility.RecordPrefabInstancePropertyModifications(piece.transform);}
            }
            // Replace bulky placeholder desk lamps with the same lamp model as the menu.
            foreach(var piece in location.GetComponentsInChildren<LocationPiece>().Where(p=>p.kitId=="desk_lamp"&&!p.transform.IsChildOf(root)))piece.gameObject.SetActive(false);
            group=root;Lamp(new Vector3(12,1.29f,4.8f));
            FixLabels();
        }
        static void FixLabels()
        {
            var label=location.GetComponentsInChildren<TextMesh>(true).First(t=>t.text=="ОПЕЧАТАНО · .45 ACP");
            label.transform.SetPositionAndRotation(new Vector3(10,1.0f,27.32f),Quaternion.Euler(90,0,0));label.characterSize=.012f;
            Box("Бирка опечатанного оружия",new Vector3(10,.983f,27.32f),new Vector3(.48f,.008f,.15f),paper);
            var department=location.GetComponentsInChildren<TextMesh>(true).First(t=>t.text=="POLICE DEPARTMENT");
            department.transform.position=new Vector3(7,2.25f,7.827f);department.characterSize=.04f;
            Box("Табличка отдела",new Vector3(7,2.25f,7.86f),new Vector3(3,.35f,.04f),sign);
            var notice=location.GetComponentsInChildren<TextMesh>(true).First(t=>t.text.StartsWith("ПРИЁМ ГРАЖДАН"));
            notice.transform.position=new Vector3(-7.5f,2.6f,7.82f);notice.characterSize=.035f;
            var father=location.GetComponentsInChildren<TextMesh>(true).First(t=>t.text=="ДЕЛО В-97/041 · ВОЛКОВ");
            father.transform.parent.position=new Vector3(7,2.3f,24.18f);father.characterSize=.045f;
            var plate=father.transform.parent.GetComponentsInChildren<Renderer>().First(r=>r.name=="Синяя эмаль");var scale=plate.transform.localScale;scale.x=5;plate.transform.localScale=scale;
        }
        static void WeatherCover(LocationEnvironment env)
        {
            var t=Group("Коллизия осадков",root);
            Action<string,Vector3,Vector3> cover=(name,p,size)=>
            {
                var o=new GameObject(name);o.layer=2;o.transform.SetParent(t,false);o.transform.position=p;var box=o.AddComponent<BoxCollider>();box.size=size;
            };
            cover("Крыша · защита от дождя",new Vector3(0,3.25f,20),new Vector3(40.5f,.5f,40.5f));
            cover("Земля · осадки",new Vector3(0,-.3f,15),new Vector3(88,.1f,104));
            cover("Козырёк · осадки",new Vector3(0,3.3f,-2),new Vector3(9,.25f,5));
            var collision=env.rainSystem.collision;collision.collidesWith=1<<2;collision.enableDynamicColliders=false;collision.maxCollisionShapes=16;
            PrefabUtility.RecordPrefabInstancePropertyModifications(env.rainSystem);
        }
        static void Environment()
        {
            var env=location.GetComponent<LocationEnvironment>();env.ambientColor=new Color(.48f,.43f,.33f);env.mainLightColor=new Color(1,.80f,.52f);env.mainLightIntensity=1.2f;env.fogColor=new Color(.28f,.28f,.25f);env.fogStart=28;env.fogEnd=100;
            env.mainLight.transform.rotation=Quaternion.Euler(28,65,0);env.mainLight.shadows=LightShadows.Hard;env.mainLight.shadowBias=.025f;env.mainLight.shadowNormalBias=.025f;
            foreach(var light in location.GetComponentsInChildren<Light>(true))
            {
                if(light.name=="Свет · люминесцентный"){light.color=new Color(1,.87f,.65f);light.intensity=1.8f;}
            }
            var grade=AssetDatabase.LoadAssetAtPath<Material>(Art+"/StationGrade.mat");
            if(grade==null){grade=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Menu/Art/MenuGrade.mat")){name="Warm police station grade"};AssetDatabase.CreateAsset(grade,Art+"/StationGrade.mat");}
            location.game.hud.retroMaterial=grade;location.game.hud.worldVerticalResolution=360;location.game.hud.locationCaption="НЬЮ-ЙОРК / ОКТЯБРЬ 2000";
            PrefabUtility.RecordPrefabInstancePropertyModifications(location.game.hud);EditorUtility.SetDirty(env);WeatherCover(env);env.Apply();
        }
    }
}
