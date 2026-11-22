using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Hakaton.Demo.Vehicles.Editor
{
    // Modelled from the 2000 Ford order guide and photographs, in metres, nose at -Z.
    public static class CrownVictoriaBuilder
    {
        public const string Folder="Assets/Demo/Vehicles/CrownVictoria";
        public const string PrefabPath=Folder+"/CrownVictoria2000.prefab";
        static Transform root;static Material white,blue,rubber,steel,cloth,glass,lens,amber,red,ink,gauge,gold,headlamp;
        static Font font;static int meshId;static List<Renderer> windows;
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent??root,false);return t;}
        static Material Mat(string name,Color color,float smooth=.2f,float emission=0)
        {
            string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,path);}
            m.color=color;if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}EditorUtility.SetDirty(m);return m;
        }
        static Mesh Asset(Mesh m,string name)
        {
            string path=Folder+"/"+(meshId++).ToString("000")+"_"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old==null){AssetDatabase.CreateAsset(m,path);return m;}EditorUtility.CopySerialized(m,old);UnityEngine.Object.DestroyImmediate(m);return old;
        }
        static GameObject MeshObject(string name,List<Vector3> points,List<int> faces,Material material,Transform parent=null,bool smooth=false)
        {
            var mesh=new Mesh{name=name};
            if(!smooth)
            {
                var v=new List<Vector3>();var f=new List<int>();foreach(int i in faces){f.Add(v.Count);v.Add(points[i]);}points=v;faces=f;
            }
            mesh.SetVertices(points);mesh.SetTriangles(faces,0);var uv=points.Select(p=>new Vector2(p.z*.5f,p.y)).ToArray();mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent??root,false);o.GetComponent<MeshFilter>().sharedMesh=Asset(mesh,name.Replace(' ','_'));o.GetComponent<Renderer>().sharedMaterial=material;return o;
        }
        static GameObject Poly(string name,Material material,Transform parent,params Vector3[] p)
        {var t=new List<int>();for(int i=1;i<p.Length-1;i++){t.Add(0);t.Add(i);t.Add(i+1);}return MeshObject(name,p.ToList(),t,material,parent);}
        static GameObject Box(string name,Vector3 position,Vector3 size,Material material,Transform parent=null)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent??root,false);o.transform.localPosition=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;}
        static void Bar(string name,Vector3 a,Vector3 b,float width,Material material,Transform parent=null)
        {var o=Box(name,(a+b)/2,V(width,width,(b-a).magnitude),material,parent);o.transform.localRotation=Quaternion.LookRotation(b-a);}
        static GameObject Disc(string name,Vector3 p,float radius,float depth,Material material,Transform parent=null,int sides=24)
        {
            var vertices=new List<Vector3>();var faces=new List<int>();
            for(int k=0;k<2;k++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(V(Mathf.Cos(a)*radius,k==0?-depth/2:depth/2,Mathf.Sin(a)*radius));}
            for(int i=0;i<sides;i++){int j=(i+1)%sides;faces.AddRange(new[]{i,j,sides+i,j,sides+j,sides+i});}
            for(int i=1;i<sides-1;i++){faces.AddRange(new[]{0,i+1,i,sides,sides+i,sides+i+1});}
            var o=MeshObject(name,vertices,faces,material,parent,true);Flip(o);o.transform.localPosition=p;return o;
        }
        static void Text(string text,Vector3 p,float size,Color color,Quaternion rotation,Transform parent=null)
        {var t=Group(text,parent);t.localPosition=p;t.localRotation=rotation;var m=t.gameObject.AddComponent<TextMesh>();m.font=font;m.text=text;m.fontSize=48;m.characterSize=size;m.anchor=TextAnchor.MiddleCenter;m.alignment=TextAlignment.Center;m.color=color;t.gameObject.AddComponent<Hakaton.Demo.WorldTextLabel>().depthShader=Shader.Find("Hakaton/World Text");}
        static float[] sectionZ={-2.6924f,-2.52f,-2.1f,-1.6f,-1.05f,-.6f,.2f,1.15f,1.85f,2.5f,2.6924f};
        static float[] sectionW={.81f,.945f,.979f,.99314f,.967f,.957f,.972f,.96f,.945f,.91f,.82f};
        static float[] sectionY={.63f,.775f,.84f,.87f,.91f,.93f,.93f,.91f,.875f,.83f,.72f};
        static float Sample(float z,float[] values)
        {for(int i=1;i<sectionZ.Length;i++)if(z<=sectionZ[i])return Mathf.Lerp(values[i-1],values[i],Mathf.InverseLerp(sectionZ[i-1],sectionZ[i],z));return values.Last();}
        static float Width(float z)=>Sample(z,sectionW);
        static float Top(float z)=>Sample(z,sectionY);
        static float Bottom(float z)
        {foreach(float axle in new[]{-1.60f,1.31338f}){float d=z-axle;if(Mathf.Abs(d)<.405f)return .338f+Mathf.Sqrt(.405f*.405f-d*d);}return .295f;}
        public static GameObject BuildPrefab()
        {
            meshId=0;windows=new List<Renderer>();font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");root=new GameObject("Crown Victoria · Police Interceptor 2000").transform;
            try
            {
                white=Mat("Ivory_paint",new Color(.68f,.72f,.71f),.36f);blue=Mat("Patrol_blue",new Color(.025f,.22f,.44f),.25f);rubber=Mat("Rubber",new Color(.014f,.018f,.019f));steel=Mat("Brushed_metal",new Color(.33f,.38f,.39f),.4f);cloth=Mat("Graphite_cloth",new Color(.12f,.15f,.16f));ink=Mat("Dashboard_charcoal",new Color(.028f,.038f,.041f));lens=Mat("Headlamp_lens",new Color(.66f,.69f,.55f),.5f,.38f);amber=Mat("Amber_lens",new Color(.65f,.28f,.045f),.4f,.15f);red=Mat("Red_lens",new Color(.43f,.025f,.014f),.4f,.10f);gauge=Mat("Gauge_ink",new Color(.40f,.58f,.43f),.1f,.22f);gold=Mat("Badge_brass",new Color(.53f,.43f,.19f));
                headlamp=Mat("Low_beam_lens",new Color(.78f,.78f,.60f));headlamp.shader=Shader.Find("Universal Render Pipeline/Unlit");headlamp.DisableKeyword("_EMISSION");EditorUtility.SetDirty(headlamp);
                glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/CarIntro/Art/Rain_on_windows.mat");
                var rig=root.gameObject.AddComponent<CrownVictoriaRig>();Body(rig);Cabin();Wheels(rig);Equipment();Interior(rig);
                rig.rainWindows=windows.ToArray();rig.driverSocket=Group("Посадка водителя");rig.driverSocket.localPosition=V(.43f,-.24f,-.15f);rig.driverSocket.localRotation=Quaternion.Euler(0,180,0);
                var rain=Group("Крыша · коллизия дождя");rain.gameObject.layer=2;rain.localPosition=V(0,1.48f,.14f);var cover=rain.gameObject.AddComponent<BoxCollider>();cover.size=V(1.65f,.12f,1.80f);
                var collision=root.gameObject.AddComponent<BoxCollider>();collision.center=V(0,.72f,0);collision.size=V(CrownVictoriaRig.BodyWidth,1.44f,CrownVictoriaRig.BodyLength);
                var interaction=root.gameObject.AddComponent<Hakaton.Demo.DemoInteractable>();interaction.kind=Hakaton.Demo.DemoInteractable.Interaction.Car;interaction.label="Полицейская машина · выбрать локацию";
                AssetDatabase.SaveAssets();var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabPath);AssetDatabase.SaveAssets();return prefab;
            }
            finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
        }
        static void Body(CrownVictoriaRig rig)
        {
            var z=new SortedSet<float>(sectionZ);foreach(float axle in new[]{-1.6f,1.31338f})for(int i=0;i<=16;i++)z.Add(axle+Mathf.Cos(i*Mathf.PI/16)*.405f);var rows=z.ToArray();var points=new List<Vector3>();var faces=new List<int>();
            foreach(float side in new[]{-1f,1f})
            {
                int first=points.Count;
                foreach(float a in rows){float bottom=Bottom(a),top=Top(a),w=Width(a);points.Add(V(side*(w-.058f),bottom,a));points.Add(V(side*w,Mathf.Lerp(bottom,top,.64f),a));points.Add(V(side*(w-.018f),top,a));}
                for(int i=0;i<rows.Length-1;i++)for(int j=0;j<2;j++){int a=first+i*3+j,b=a+3;if(side<0)faces.AddRange(new[]{a,b,a+1,a+1,b,b+1});else faces.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            }
            rig.bodyShell=MeshObject("Кузов · арки и крылья",points,faces,white,null,true).GetComponent<Renderer>();
            // Crown Victoria's shallow bonnet and separate long deck are surfaces, not solid window blocks.
            Deck("Капот",new[]{-2.54f,-2.35f,-2.0f,-1.6f,-1.20f,-1.02f},new[]{.78f,.84f,.87f,.89f,.92f,.93f});
            Deck("Багажник",new[]{1.40f,1.65f,2.0f,2.35f,2.56f},new[]{.94f,.925f,.90f,.86f,.825f});
            Box("Днище",V(0,.26f,0),V(1.54f,.08f,4.9f),rubber);
            foreach(float end in new[]{-1f,1f})
            {
                float[] xs={-.93f,-.85f,-.55f,0,.55f,.85f,.93f};var v=new List<Vector3>();var f=new List<int>();
                foreach(float x in xs){float zEnd=end*(2.6924f-.19f*Mathf.Pow(Mathf.Abs(x)/.93f,4));v.Add(V(x,.32f,zEnd));v.Add(V(x,.58f,zEnd));v.Add(V(x*.97f,.65f,zEnd-end*.03f));}
                for(int i=0;i<xs.Length-1;i++)for(int j=0;j<2;j++){int a=i*3+j,b=a+3;if(end<0)f.AddRange(new[]{a,a+1,b,a+1,b+1,b});else f.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
                MeshObject(end<0?"Округлый передний бампер":"Округлый задний бампер",v,f,white,null,true);
                Box("Тёмный молдинг бампера",V(0,.52f,end*2.697f),V(1.5f,.027f,.015f),rubber);
                Box("Госномер",V(0,.43f,end*2.704f),V(.30f,.14f,.015f),white);Text("027  00",V(0,.43f,end*2.716f),.012f,new Color(.05f,.09f,.13f),Quaternion.Euler(0,end<0?0:180,0));
            }
            foreach(float end in new[]{-1f,1f})
            {
                var cap=new List<Vector3>();var tris=new List<int>();float[] cross={-.93f,-.86f,-.48f,0,.48f,.86f,.93f};
                foreach(float x in cross){float capZ=end*(2.63f-.15f*Mathf.Pow(Mathf.Abs(x)/.93f,4));cap.Add(V(x,.60f,capZ));cap.Add(V(x,.825f,capZ));}
                for(int i=0;i<cross.Length-1;i++){int a=i*2,b=a+2;if(end<0)tris.AddRange(new[]{a,a+1,b,a+1,b+1,b});else tris.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
                MeshObject(end<0?"Передняя облицовка":"Задняя облицовка",cap,tris,white,null,true);
            }
            Poly("Решётка радиатора",rubber,null,V(-.43f,.62f,-2.672f),V(-.39f,.82f,-2.650f),V(.39f,.82f,-2.650f),V(.43f,.62f,-2.672f));
            for(int i=0;i<13;i++)Box("Соты решётки",V(-.37f+i*.062f,.718f,-2.680f),V(.012f,.15f,.012f),steel);
            var badge=Disc("Овал на решётке",V(0,.726f,-2.695f),.048f,.009f,blue,null,16);badge.transform.localRotation=Quaternion.Euler(90,0,0);badge.transform.localScale=V(1,.58f,1);
            foreach(float side in new[]{-1f,1f})
            {
                var lamp=Poly("Составная передняя фара",headlamp,null,V(side*.48f,.635f,-2.660f),V(side*.48f,.82f,-2.650f),V(side*.86f,.80f,-2.570f),V(side*.92f,.635f,-2.560f));if(side<0)Flip(lamp);
                var turn=Poly("Поворотник на крыле",amber,null,V(side*.925f,.65f,-2.55f),V(side*.875f,.80f,-2.56f),V(side*.97f,.79f,-2.29f),V(side*.99f,.65f,-2.30f));if(side<0)Flip(turn);
                for(int i=0;i<3;i++)Bar("Рёбра отражателя",V(side*(.53f+i*.10f),.66f,-2.666f+i*.023f),V(side*(.53f+i*.10f),.79f,-2.666f+i*.023f),.008f,white);
                var tail=Poly("Задний фонарь",red,null,V(side*.58f,.62f,2.63f),V(side*.91f,.62f,2.54f),V(side*.9f,.82f,2.53f),V(side*.59f,.84f,2.62f));if(side<0)Flip(tail);
                var pipe=Disc("Выхлопная труба",V(side*.57f,.22f,2.58f),.032f,.20f,steel);pipe.transform.localRotation=Quaternion.Euler(90,0,0);
                var light=Group("Фара ближнего света").gameObject.AddComponent<Light>();light.transform.localPosition=V(side*.68f,.73f,-2.68f);light.transform.localRotation=Quaternion.Euler(9,180,0);light.type=LightType.Spot;light.color=new Color(1,.91f,.70f);light.intensity=75;light.range=28;light.spotAngle=54;
                // Blue side stripes follow the real fender contour and avoid the wheel cut-outs.
                for(int i=0;i<rows.Length-1;i++)
                {
                    float a=rows[i],b=rows[i+1];if(a< -2.44f||b>2.43f)continue;
                    var stripe=Poly("Верхняя синяя полоса",blue,null,V(side*(Width(a)+.004f),Top(a)-.035f,a),V(side*(Width(b)+.004f),Top(b)-.035f,b),V(side*(Width(b)+.004f),Top(b)-.078f,b),V(side*(Width(a)+.004f),Top(a)-.078f,a));if(side<0)Flip(stripe);
                    if(Bottom(a)<.37f&&Bottom(b)<.37f){var low=Poly("Нижняя синяя полоса",blue,null,V(side*(Width(a)-.028f),.405f,a),V(side*(Width(b)-.028f),.405f,b),V(side*(Width(b)-.045f),.35f,b),V(side*(Width(a)-.045f),.35f,a));if(side<0)Flip(low);}
                }
                foreach(float door in new[]{-.90f,.13f,.99f})Bar("Шов двери",V(side*(Width(door)-.01f),Mathf.Max(.39f,Bottom(door)),door),V(side*(Width(door)-.014f),Top(door)-.012f,door),.005f,rubber);
                foreach(float handle in new[]{.01f,.91f})Box("Ручка двери",V(side*(Width(handle)+.01f),.815f,handle),V(.028f,.029f,.13f),steel);
                Text("NYPD",V(side*.985f,.70f,-.36f),.061f,new Color(.025f,.21f,.43f),Quaternion.Euler(0,side>0?-90:90,0));
                Text("COURTESY\nPROFESSIONALISM\nRESPECT",V(side*.985f,.67f,.62f),.013f,new Color(.08f,.27f,.45f),Quaternion.Euler(0,side>0?-90:90,0));
                Text("POLICE",V(side*.988f,.80f,-1.35f),.018f,new Color(.03f,.20f,.40f),Quaternion.Euler(0,side>0?-90:90,0));
            }
            Box("Задняя чёрная панель",V(0,.72f,2.650f),V(.94f,.18f,.022f),rubber);
        }
        static void Flip(GameObject o)
        {var mesh=o.GetComponent<MeshFilter>().sharedMesh;var t=mesh.triangles;for(int i=0;i<t.Length;i+=3){int a=t[i];t[i]=t[i+2];t[i+2]=a;}mesh.triangles=t;mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);}
        static void Deck(string name,float[] z,float[] y)
        {
            var v=new List<Vector3>();var t=new List<int>();float[] cross={-1,-.75f,0,.75f,1};
            for(int i=0;i<z.Length;i++)foreach(float x in cross)v.Add(V(x*(Width(z[i])-.025f),y[i]-.035f*x*x,z[i]));
            for(int i=0;i<z.Length-1;i++)for(int j=0;j<4;j++){int a=i*5+j,b=a+5;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            MeshObject(name,v,t,white,null,true);
        }
        static Renderer Window(string name,params Vector3[] p)
        {var o=Poly(name,glass,null,p);var mesh=o.GetComponent<MeshFilter>().sharedMesh;mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,0),new Vector2(1,1),new Vector2(0,1)};EditorUtility.SetDirty(mesh);windows.Add(o.GetComponent<Renderer>());return o.GetComponent<Renderer>();}
        static void Cabin()
        {
            float[] z={-.50f,-.29f,.15f,.61f,.84f};float[] w={.70f,.765f,.78f,.75f,.655f};float[] y={1.33f,1.407f,1.44272f,1.385f,1.30f};var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<z.Length;i++)for(int j=0;j<7;j++){float x=(j-3)/3f;v.Add(V(x*w[i],y[i]-.035f*x*x,z[i]));}
            for(int i=0;i<z.Length-1;i++)for(int j=0;j<6;j++){int a=i*7+j,b=a+7;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}MeshObject("Скруглённая крыша",v,t,white,null,true);
            Poly("Потолок салона",cloth,null,V(-.68f,1.313f,-.48f),V(.68f,1.313f,-.48f),V(.64f,1.29f,.82f),V(-.64f,1.29f,.82f));
            Window("Лобовое стекло",V(-.87f,.93f,-1.05f),V(.87f,.93f,-1.05f),V(.70f,1.33f,-.50f),V(-.70f,1.33f,-.50f));
            Window("Заднее стекло",V(.82f,.94f,1.46f),V(-.82f,.94f,1.46f),V(-.655f,1.30f,.84f),V(.655f,1.30f,.84f));
            foreach(float s in new[]{-1f,1f})
            {
                Window("Переднее боковое стекло",V(s*.91f,.94f,-1.02f),V(s*.946f,.94f,.09f),V(s*.772f,1.393f,.09f),V(s*.71f,1.31f,-.47f));
                Window("Заднее боковое стекло",V(s*.946f,.94f,.21f),V(s*.925f,.94f,1.18f),V(s*.685f,1.30f,.73f),V(s*.774f,1.395f,.21f));
                Bar("A-стойка",V(s*.895f,.93f,-1.045f),V(s*.713f,1.342f,-.49f),.046f,white);
                var b=Poly("B-стойка",white,null,V(s*.947f,.925f,.095f),V(s*.947f,.925f,.21f),V(s*.775f,1.408f,.21f),V(s*.775f,1.408f,.095f));if(s>0)Flip(b);
                var c=Poly("Широкая задняя стойка",white,null,V(s*.925f,.929f,1.18f),V(s*.82f,.929f,1.46f),V(s*.655f,1.301f,.84f),V(s*.685f,1.301f,.73f));if(s>0)Flip(c);
                Bar("Уплотнитель окна",V(s*.918f,.936f,-1.05f),V(s*.947f,.936f,1.18f),.018f,rubber);
                Bar("Молдинг над окнами",V(s*.716f,1.337f,-.48f),V(s*.776f,1.417f,.14f),.014f,steel);
                Box("Зеркало · кронштейн",V(s*.985f,1.015f,-.77f),V(.12f,.035f,.045f),rubber);
                var mirror=Disc("Корпус зеркала",V(s*1.068f,1.045f,-.77f),.092f,.13f,white,null,12);mirror.transform.localRotation=Quaternion.Euler(0,0,90);mirror.transform.localScale=V(1,.65f,1);
                Box("Зеркальная вставка",V(s*1.077f,1.045f,-.701f),V(.14f,.085f,.006f),steel);
            }
            Bar("Уплотнитель лобового стекла",V(-.87f,.925f,-1.05f),V(.87f,.925f,-1.05f),.02f,rubber);
            Bar("Верхняя рамка лобового стекла",V(-.70f,1.335f,-.5f),V(.70f,1.335f,-.5f),.025f,rubber);
        }
        static void Wheels(CrownVictoriaRig rig)
        {
            var list=new List<Transform>();
            foreach(float axle in new[]{-1.6f,1.31338f})foreach(float side in new[]{-1f,1f})
            {
                float track=axle<0?1.61036f:1.65862f;var pivot=Group("Колесо · "+(axle<0?"переднее":"заднее")+(side>0?" левое":" правое"));pivot.localPosition=V(side*track/2,.338f,axle);pivot.localRotation=Quaternion.Euler(0,0,90);list.Add(pivot);
                float[] a={-.114f,-.106f,-.080f,0,.080f,.106f,.114f};float[] r={.214f,.285f,.329f,.338f,.329f,.285f,.214f};var v=new List<Vector3>();var t=new List<int>();
                for(int i=0;i<a.Length;i++)for(int j=0;j<24;j++){float angle=j*Mathf.PI/12;v.Add(V(Mathf.Cos(angle)*r[i],a[i],Mathf.Sin(angle)*r[i]));}
                for(int i=0;i<a.Length-1;i++)for(int j=0;j<24;j++){int k=(j+1)%24,p=i*24+j,q=i*24+k;t.AddRange(new[]{p,p+24,q,q,p+24,q+24});}MeshObject("Шина · округлый профиль",v,t,rubber,pivot,true);
                float face=-side*.116f;Disc("Стальной диск",V(0,face,0),.204f,.025f,steel,pivot,24);Disc("Чёрная чашка диска",V(0,face-side*.015f,0),.178f,.007f,rubber,pivot,24);
                for(int i=0;i<12;i++){float ang=i*Mathf.PI/6;Disc("Вентиляция диска",V(Mathf.Cos(ang)*.15f,face-side*.020f,Mathf.Sin(ang)*.15f),.024f,.009f,ink,pivot,8);}
                Disc("Хромированный колпак",V(0,face-side*.028f,0),.103f,.030f,steel,pivot,20);
            }
            rig.wheels=list.ToArray();
        }
        static void Equipment()
        {
            Box("Основание Vector-подобной балки",V(0,1.47f,.13f),V(1.44f,.045f,.37f),rubber);
            for(int i=-3;i<=3;i++)
            {
                float x=i*.20f,z=.09f+Mathf.Abs(i)*.045f;Disc("Маяк · основание",V(x,1.52f,z),.104f,.09f,steel,null,12);var m=i==0||Mathf.Abs(i)==2?lens:red;
                Disc("Маяк · колпак",V(x,1.588f,z),.097f,.092f,m,null,16);Disc("Маяк · верх",V(x,1.64f,z),.080f,.02f,m,null,16);
            }
            Bar("Прожектор · ножка",V(.94f,.99f,-.72f),V(1.01f,1.12f,-.72f),.025f,steel);
            var spot=Disc("Прожектор на A-стойке",V(1.01f,1.13f,-.76f),.078f,.075f,steel);spot.transform.localRotation=Quaternion.Euler(90,0,0);
            var face=Disc("Стекло прожектора",V(1.01f,1.13f,-.804f),.067f,.006f,lens);face.transform.localRotation=Quaternion.Euler(90,0,0);
            Bar("Радиоантенна",V(-.53f,.91f,1.8f),V(-.53f,1.50f,1.8f),.008f,rubber);
        }
        static void Interior(CrownVictoriaRig rig)
        {
            var cabin=Group("Салон · Crown Victoria");Box("Ковёр пола",V(0,.32f,.10f),V(1.60f,.06f,2.35f),rubber,cabin);
            foreach(float x in new[]{-.43f,.43f})
            {
                var seat=Group("Переднее кресло",cabin);seat.localPosition=V(x,0,.32f);
                Box("Подушка",V(0,.45f,0),V(.60f,.15f,.66f),cloth,seat);foreach(float side in new[]{-.25f,.25f})Box("Боковая поддержка",V(side,.51f,.02f),V(.08f,.10f,.58f),cloth,seat);
                var back=Box("Спинка",V(0,.76f,.31f),V(.58f,.60f,.16f),cloth,seat);back.transform.localRotation=Quaternion.Euler(-9,0,0);Box("Подголовник",V(0,1.13f,.38f),V(.34f,.18f,.13f),cloth,seat);
                for(int i=0;i<5;i++)Box("Шов ткани",V(0,.54f,-.2f+i*.09f),V(.39f,.003f,.008f),ink,seat);
            }
            Box("Задний диван",V(0,.48f,1.04f),V(1.55f,.19f,.52f),cloth,cabin);Box("Спинка заднего дивана",V(0,.78f,1.27f),V(1.48f,.57f,.14f),cloth,cabin);
            var dashboard=Group("Панель приборов",cabin);
            float[] xs={-.88f,-.72f,-.40f,0,.4f,.72f,.88f};var v=new List<Vector3>();var f=new List<int>();
            foreach(float x in xs){float y=.91f-.07f*Mathf.Abs(x);v.Add(V(x,y,-.98f));v.Add(V(x,y+.015f,-.70f));v.Add(V(x,.73f,-.48f));}
            for(int i=0;i<xs.Length-1;i++)for(int j=0;j<2;j++){int a=i*3+j,b=a+3;f.AddRange(new[]{a,b,a+1,a+1,b,b+1});}MeshObject("Изогнутая приборная панель",v,f,ink,dashboard,true);
            Box("Перчаточный ящик",V(-.45f,.76f,-.53f),V(.50f,.16f,.05f),cloth,dashboard);Box("Ручка бардачка",V(-.45f,.81f,-.496f),V(.12f,.019f,.015f),rubber,dashboard);
            Box("Козырёк аналоговых приборов",V(.43f,1.015f,-.775f),V(.59f,.17f,.18f),ink,dashboard);
            Box("Лицевая панель приборов",V(.43f,.998f,-.674f),V(.55f,.13f,.012f),rubber,dashboard);
            Gauge(V(.43f,1.0f,-.662f),.074f,true,dashboard);Gauge(V(.25f,1.0f,-.662f),.042f,false,dashboard);Gauge(V(.61f,1.0f,-.662f),.042f,false,dashboard);
            Text("MPH",V(.43f,.966f,-.650f),.0045f,new Color(.45f,.62f,.43f),Quaternion.Euler(0,180,0),dashboard);
            for(int i=0;i<4;i++){Box("Дефлектор",V(-.62f+i*.18f,.889f,-.626f),V(.12f,.055f,.025f),rubber,dashboard);for(int j=0;j<4;j++)Box("Ламель дефлектора",V(-.66f+i*.18f+j*.027f,.889f,-.605f),V(.006f,.047f,.008f),cloth,dashboard);}
            Box("Радио AM FM",V(-.06f,.77f,-.527f),V(.32f,.13f,.06f),rubber,dashboard);Box("Радио · дисплей",V(-.07f,.797f,-.491f),V(.19f,.031f,.006f),gauge,dashboard);
            for(int i=0;i<6;i++)Box("Кнопка радиостанции",V(-.17f+i*.044f,.745f,-.490f),V(.026f,.018f,.014f),cloth,dashboard);
            foreach(float x in new[]{-.18f,-.06f,.06f}){var knob=Disc("Ручка климата",V(x,.659f,-.514f),.024f,.025f,rubber,dashboard,12);knob.transform.localRotation=Quaternion.Euler(90,0,0);}
            var wheel=Group("Руль",cabin);wheel.localPosition=V(.43f,.97f,-.47f);wheel.localRotation=Quaternion.Euler(18,0,0);rig.steering=wheel;
            var ring=new List<Vector3>();var tri=new List<int>();
            for(int i=0;i<32;i++)for(int j=0;j<8;j++){float a=i*Mathf.PI/16,b=j*Mathf.PI/4;ring.Add(V(Mathf.Cos(a)*(.188f+Mathf.Cos(b)*.017f),Mathf.Sin(a)*(.188f+Mathf.Cos(b)*.017f),Mathf.Sin(b)*.017f));}
            for(int i=0;i<32;i++)for(int j=0;j<8;j++){int a=i*8+j,b=i*8+(j+1)%8,c=((i+1)%32)*8+j,d=((i+1)%32)*8+(j+1)%8;tri.AddRange(new[]{a,b,c,b,d,c});}MeshObject("Круглый обод руля",ring,tri,rubber,wheel,true);
            Box("Airbag · центр руля",V(0,-.005f,.018f),V(.19f,.12f,.065f),cloth,wheel);foreach(float x in new[]{-.11f,.11f})Box("Спица руля",V(x,.015f,0),V(.11f,.045f,.033f),cloth,wheel);Box("Нижняя спица",V(0,-.12f,0),V(.04f,.14f,.03f),cloth,wheel);
            Bar("Подрулевой переключатель",V(.26f,.94f,-.57f),V(.13f,.97f,-.57f),.018f,rubber,cabin);
            Bar("Рычаг коробки передач",V(.57f,.94f,-.57f),V(.67f,1.0f,-.55f),.016f,steel,cabin);
            Box("Полицейская радиоконсоль",V(-.02f,.51f,-.08f),V(.30f,.25f,.38f),rubber,cabin);Box("Рация · лицевая панель",V(-.02f,.64f,-.08f),V(.26f,.027f,.28f),steel,cabin);for(int i=0;i<4;i++)Box("Рация · клавиша",V(-.10f+i*.052f,.66f,-.11f),V(.033f,.014f,.035f),rubber,cabin);
            foreach(float s in new[]{-1f,1f}){Box("Внутренняя дверная карта",V(s*.87f,.67f,.05f),V(.035f,.39f,.99f),cloth,cabin);Box("Дверной подлокотник",V(s*.815f,.72f,.0f),V(.14f,.075f,.40f),rubber,cabin);Box("Внутренняя ручка",V(s*.825f,.81f,-.24f),V(.03f,.028f,.12f),steel,cabin);}
            Box("Зеркало заднего вида",V(0,1.29f,-.55f),V(.23f,.08f,.04f),rubber,cabin);Box("Зеркальная поверхность",V(0,1.29f,-.525f),V(.20f,.055f,.005f),steel,cabin);
            foreach(float x in new[]{-.40f,.40f})Box("Солнцезащитный козырёк",V(x,1.354f,-.39f),V(.37f,.025f,.20f),cloth,cabin);
            foreach(float x in new[]{-.39f,.39f}){var w=Group("Дворник");w.localPosition=V(x,.94f,-1.08f);Bar("Рычаг дворника",V(0,0,0),V(.09f,.25f,.24f),.012f,rubber,w);Bar("Щётка дворника",V(.09f,.20f,.19f),V(.09f,.40f,.36f),.019f,rubber,w);if(x<0)rig.wiperLeft=w;else rig.wiperRight=w;}
            var fill=Group("Приборы · слабая подсветка").gameObject.AddComponent<Light>();fill.type=LightType.Point;fill.color=new Color(.48f,.62f,.49f);fill.range=.65f;fill.intensity=.012f;fill.transform.localPosition=V(.43f,1.10f,-.65f);
        }
        static void Gauge(Vector3 p,float radius,bool speed,Transform parent)
        {
            var disc=Disc("Аналоговый циферблат",p,radius,.006f,rubber,parent,24);disc.transform.localRotation=Quaternion.Euler(90,0,0);
            int count=speed?15:7;for(int i=0;i<count;i++){float a=(-130+i*260f/(count-1))*Mathf.Deg2Rad;var tick=Box("Штрих шкалы",p+V(Mathf.Sin(a)*radius*.81f,Mathf.Cos(a)*radius*.81f,.006f),V(.003f,radius*.18f,.003f),gauge,parent);tick.transform.localRotation=Quaternion.Euler(0,0,-a*Mathf.Rad2Deg);}
            var needle=Box("Стрелка прибора",p+V(-radius*.24f,-radius*.03f,.010f),V(radius*.70f,.0035f,.003f),gauge,parent);needle.transform.localRotation=Quaternion.Euler(0,0,25);
        }
    }
}
