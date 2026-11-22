using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hakaton.Demo.Editor
{
    // Generates real scene objects and asset files, editable in the normal Unity inspector.
    // Run through CLI: unity command 'script eval' -- --code 'Hakaton.Demo.Editor.DemoSceneBuilder.Build();'
    public static class DemoSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/HouseDemo.unity";
        const string Art = "Assets/Demo/Art/Generated";
        static Transform root, room;
        static Material wood, darkWood, plaster, wallpaper, ceiling, metal, brass, fabric, green, cream, black, glass, paper;
        static Material photoMat, letterMat, meetingMat;
        static Mesh cylinder;
        static Font font;
        static List<Light> hallLights;
        static DemoGame game;

        [MenuItem("Hakaton/Build first playable scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before building the scene.");
            if (EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene before generating HouseDemo.");
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Materials();
            cylinder = MakeCylinder();
            hallLights = new List<Light>();
            root = new GameObject("HOUSE · October 2000").transform;
            game = new GameObject("Demo · game state").AddComponent<DemoGame>();
            game.sound = game.gameObject.AddComponent<DemoSound>();
            game.hud = new GameObject("Interface").AddComponent<DemoHud>();
            game.hud.retroMaterial = SaveAsset(new Material(Shader.Find("Hakaton/Retro Display")), Art + "/RetroDisplay.mat");
            game.hud.font = font;
            game.evidence = new DemoInteractable[3];
            Environment();
            Architecture();
            LivingRoom();
            Study();
            Kitchen();
            Corridor();
            People();
            Player();
            game.hallLights = hallLights.ToArray();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 1, 2), Quaternion.Euler(28, 15, 0), 13);
            Debug.Log("HouseDemo ready: editable house, 3 clues, dialogue, chase and checkpoint.");
        }

        static T SaveAsset<T>(T value, string path) where T : UnityEngine.Object
        {
            value.name = Path.GetFileNameWithoutExtension(path);
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) { EditorUtility.CopySerialized(value, existing); UnityEngine.Object.DestroyImmediate(value); return existing; }
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        static void Materials()
        {
            wood = Mat("Oak floor", new Color(0.48f, 0.34f, 0.20f), Pattern("Oak", 0));
            wood.mainTextureScale = new Vector2(10, 14);
            darkWood = Mat("Dark walnut", new Color(0.24f, 0.15f, 0.09f), Pattern("Walnut", 0));
            plaster = Mat("Faded plaster", new Color(0.5f, 0.51f, 0.38f), Pattern("Plaster", 1));
            wallpaper = Mat("Striped wallpaper", new Color(0.48f, 0.45f, 0.3f), Pattern("Wallpaper", 2));
            wallpaper.mainTextureScale = new Vector2(4, 2);
            ceiling = Mat("Ceiling", new Color(0.38f, 0.39f, 0.32f), Pattern("Ceiling", 1));
            metal = Mat("Dull enamel", new Color(0.33f, 0.38f, 0.35f));
            brass = Mat("Aged brass", new Color(0.48f, 0.33f, 0.14f));
            fabric = Mat("Worn burgundy", new Color(0.28f, 0.13f, 0.095f), Pattern("Fabric", 3));
            green = Mat("Kitchen green", new Color(0.2f, 0.28f, 0.22f), Pattern("Paint", 1));
            cream = Mat("Ivory", new Color(0.65f, 0.61f, 0.45f));
            black = Mat("Charcoal", new Color(0.035f, 0.045f, 0.037f));
            glass = Mat("Night through window", new Color(0.11f, 0.18f, 0.22f));
            glass.EnableKeyword("_EMISSION"); glass.SetColor("_EmissionColor", new Color(0.045f, 0.09f, 0.12f));
            paper = Mat("Old paper", new Color(0.77f, 0.73f, 0.56f));
            photoMat = Mat("Family photograph", Color.white, Document("Family", 0));
            letterMat = Mat("Unsent letter", Color.white, Document("Letter", 1));
            meetingMat = Mat("Meeting note", Color.white, Document("Meeting", 2));
        }

        static Material Mat(string name, Color color, Texture2D texture = null)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0);
            material.SetFloat("_SpecularHighlights", 0);
            material.SetFloat("_EnvironmentReflections", 0);
            if (texture != null) material.SetTexture("_BaseMap", texture);
            return SaveAsset(material, Art + "/" + name.Replace(" ", "_") + ".mat");
        }

        static Texture2D Pattern(string name, int kind)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGB24, false);
            var rng = new System.Random(name.Length * 77 + kind);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float v = 0.82f + (float)rng.NextDouble() * 0.18f;
                if (kind == 0) { v -= Mathf.Abs(Mathf.Sin(x * 0.85f + Mathf.Sin(y * 0.15f) * 0.8f)) * 0.17f; if (x % 16 == 0 || (y == 0 && x < 32) || (y == 32 && x >= 32)) v *= 0.48f; }
                if (kind == 2) { if (x % 16 < 3) v *= 0.72f; if (x % 16 == 8 && y % 16 < 3) v *= 0.65f; }
                if (kind == 3) v *= (x + y) % 2 == 0 ? 0.9f : 1;
                texture.SetPixel(x, y, new Color(v, v, v));
            }
            texture.Apply();
            return WriteTexture(texture, name);
        }

        static Texture2D Document(string name, int kind)
        {
            const int size = 128;
            var t = new Texture2D(size, size, TextureFormat.RGB24, false);
            var rng = new System.Random(1989 + kind);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float grain = (float)rng.NextDouble() * 0.08f;
                Color c = new Color(0.75f + grain, 0.70f + grain, 0.52f + grain);
                if (kind == 0)
                {
                    c = y < 30 ? new Color(0.24f, 0.27f, 0.16f) : new Color(0.42f, 0.49f, 0.48f);
                    if (x > 15 && x < 113 && y > 25 && y < 89) c = new Color(0.52f, 0.47f, 0.32f);
                    if (y >= 89 && y < 113 && Mathf.Abs(x - 64) < (113 - y) * 2.3f) c = new Color(0.19f, 0.17f, 0.14f);
                    if (y > 47 && y < 72 && ((x > 25 && x < 43) || (x > 84 && x < 102))) c = new Color(0.14f, 0.20f, 0.21f);
                    if (x > 56 && x < 73 && y > 27 && y < 73) c = new Color(0.23f, 0.16f, 0.11f);
                    for (int p = 0; p < 4; p++)
                    {
                        int px = 33 + p * 20, height = p == 0 ? 39 : 26 + p * 2;
                        if (Mathf.Abs(x - px) < 5 && y > 18 && y < height) c = p % 2 == 0 ? new Color(0.19f, 0.22f, 0.19f) : new Color(0.31f, 0.22f, 0.16f);
                        if (Mathf.Abs(x - px) < 4 && y >= height && y < height + 9) c = new Color(0.63f, 0.49f, 0.33f);
                    }
                    c *= 0.87f + grain;
                    if (x < 8 || x > 119 || y < 10 || y > 119) c = new Color(0.77f, 0.73f, 0.58f);
                }
                else
                {
                    if (kind == 2 && y > 93 && y < 116 && x > 10 && x < 118) c = new Color(0.4f, 0.16f, 0.10f);
                    if (y > 25 && y < 92 && y % 12 < 2 && x > 14 && x < 108 - ((y / 12) % 3) * 8) c *= 0.30f;
                    if (kind == 2 && x > 48 && x < 98 && y > 46 && y < 63 && (x == 49 || x == 97 || y == 47 || y == 62)) c = new Color(0.55f, 0.12f, 0.08f);
                }
                t.SetPixel(x, y, c);
            }
            t.Apply();
            return WriteTexture(t, name);
        }

        static Texture2D WriteTexture(Texture2D t, string name)
        {
            string path = Art + "/" + name + ".png";
            File.WriteAllBytes(path, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void Environment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.23f, 0.26f, 0.25f);
            RenderSettings.ambientEquatorColor = new Color(0.19f, 0.20f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.10f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.065f, 0.085f, 0.075f);
            RenderSettings.fogStartDistance = 8; RenderSettings.fogEndDistance = 29;
            RenderSettings.skybox = null;
        }

        static void Architecture()
        {
            room = Group("01 · Architecture");
            Box("Oak floor", new Vector3(0, -0.15f, 4), new Vector3(18, 0.3f, 18), wood);
            Box("Ceiling", new Vector3(0, 3.15f, 4), new Vector3(18, 0.3f, 18), ceiling);
            Wall(new Vector3(-9, 1.5f, 4), new Vector3(0.25f, 3, 18), wallpaper);
            Wall(new Vector3(9, 1.5f, 4), new Vector3(0.25f, 3, 18), plaster);
            Wall(new Vector3(0, 1.5f, -5), new Vector3(18, 3, 0.25f), wallpaper);
            Wall(new Vector3(0, 1.5f, 13), new Vector3(18, 3, 0.25f), plaster);
            // Side walls with real gaps, rather than invisible collider doors.
            Partition(-1.5f, -5, 13, new[] { -0.4f, 7.0f });
            Partition(1.5f, -5, 13, new[] { 0f });
            Wall(new Vector3(-5.2f, 1.5f, 4), new Vector3(7.4f, 3, 0.22f), wallpaper);
            Wall(new Vector3(5.2f, 1.5f, 4), new Vector3(7.4f, 3, 0.22f), plaster);
            // Rear right wing is closed in this slice.
            Box("Hall runner", new Vector3(0, 0.018f, 4), new Vector3(1.12f, 0.035f, 16.8f), fabric, false);
            Door("Гостиная", -1.5f, -0.4f, -105, false);
            Door("Кабинет", -1.5f, 7, -105, false);
            game.kitchenDoor = Door("Кухня", 1.5f, 0, 105, true);
            game.kitchenDoor.SetOpen(true);
            foreach (float z in new[] { -0.4f, 7f }) Frame(-1.5f, z);
            Frame(1.5f, 0);
            Box("Front door", new Vector3(0, 1.1f, -4.82f), new Vector3(1.45f, 2.2f, 0.12f), darkWood)
                .AddComponent<DemoInteractable>().label = "Входная дверь";
            var front = room.GetChild(room.childCount - 1).GetComponent<DemoInteractable>();
            front.kind = DemoInteractable.Interaction.FrontDoor;
            WorldText("КУХНЯ", new Vector3(1.34f, 2.48f, 0.55f), new Vector3(0, -90, 0), 0.045f);
            WorldText("КАБИНЕТ", new Vector3(-1.34f, 2.48f, 6.45f), new Vector3(0, 90, 0), 0.04f);
        }

        static void Partition(float x, float start, float end, float[] doors)
        {
            float at = start;
            foreach (float center in doors)
            {
                float a = center - 0.85f, b = center + 0.85f;
                Wall(new Vector3(x, 1.5f, (at + a) / 2), new Vector3(0.22f, 3, a - at), wallpaper);
                Wall(new Vector3(x, 2.64f, center), new Vector3(0.22f, 0.72f, 1.7f), wallpaper);
                at = b;
            }
            Wall(new Vector3(x, 1.5f, (at + end) / 2), new Vector3(0.22f, 3, end - at), wallpaper);
        }

        static void Wall(Vector3 p, Vector3 size, Material mat)
        {
            Box("Wall", p, size, mat);
            Box("Skirting", new Vector3(p.x, 0.12f, p.z), new Vector3(size.x + 0.045f, 0.24f, size.z + 0.045f), darkWood);
            Box("Picture rail", new Vector3(p.x, 2.52f, p.z), new Vector3(size.x + 0.035f, 0.065f, size.z + 0.035f), darkWood, false);
        }

        static void Frame(float x, float z)
        {
            foreach (float side in new[] { -0.92f, 0.92f })
                Box("Door casing", new Vector3(x, 1.17f, z + side), new Vector3(0.36f, 2.34f, 0.11f), cream, false);
            Box("Lintel", new Vector3(x, 2.3f, z), new Vector3(0.36f, 0.14f, 1.95f), cream, false);
        }

        static DemoDoor Door(string title, float x, float z, float angle, bool kitchen)
        {
            var hinge = new GameObject(title + " · hinge").transform;
            hinge.SetParent(room); hinge.position = new Vector3(x, 0, z - 0.8f);
            var door = hinge.gameObject.AddComponent<DemoDoor>();
            door.label = title; door.openAngle = angle; door.kitchenDoor = kitchen;
            var leaf = Box("Door leaf", new Vector3(x, 1.08f, z), new Vector3(0.13f, 2.16f, 1.58f), darkWood);
            leaf.transform.SetParent(hinge, true);
            var panel = Box("Inset panel", new Vector3(x + (x < 0 ? 0.075f : -0.075f), 1.15f, z), new Vector3(0.04f, 1.55f, 1.18f), wood, false);
            panel.transform.SetParent(hinge, true);
            foreach (float side in new[] { -1f, 1f })
            {
                var knob = LowCylinder("Handle", new Vector3(x + side * 0.12f, 1.02f, z + 0.6f), new Vector3(0.09f, 0.11f, 0.09f), brass);
                knob.transform.SetParent(hinge, true); knob.transform.Rotate(0, 0, 90);
            }
            door.SetOpen(!kitchen);
            return door;
        }

        static void LivingRoom()
        {
            room = Group("02 · Living room");
            Window(new Vector3(-8.82f, 1.8f, -0.3f), 90);
            // Sofa faces the room, with blocky cushions and a visible silhouette.
            Box("Sofa base", new Vector3(-6.9f, 0.4f, 1.7f), new Vector3(3.2f, 0.55f, 1.2f), darkWood);
            Box("Sofa back", new Vector3(-6.9f, 0.95f, 2.22f), new Vector3(3.2f, 1.1f, 0.3f), fabric);
            foreach (float x in new[] { -8.4f, -5.4f }) Box("Arm rest", new Vector3(x, 0.7f, 1.7f), new Vector3(0.26f, 0.85f, 1.2f), fabric);
            for (int i = 0; i < 3; i++) Box("Cushion", new Vector3(-7.9f + i, 0.73f, 1.6f), new Vector3(0.93f, 0.22f, 0.94f), fabric);
            Box("Persian rug", new Vector3(-5.5f, 0.027f, -0.6f), new Vector3(4.9f, 0.035f, 3.8f), fabric, false);
            Table(new Vector3(-5.5f, 0, -0.4f), 2.25f, 1.1f, 0.65f);
            Clue(0, new Vector3(-5.5f, 0.715f, -0.35f), photoMat, new Vector3(0, 12, 0));
            Table(new Vector3(-7.4f, 0, -3.8f), 2.6f, 0.85f, 0.8f);
            CRT(new Vector3(-7.25f, 0.84f, -3.8f));
            Lamp(new Vector3(-3, 0, 2.65f), true);
            Bookshelf(new Vector3(-3, 0, -4.42f), 0);
            Picture(new Vector3(-5.8f, 1.95f, 3.82f), 180);
            Point("Warm living room", new Vector3(-5.2f, 2.4f, 0.2f), new Color(1, 0.69f, 0.35f), 3.5f, 8);
        }

        static void Study()
        {
            room = Group("03 · Study");
            Window(new Vector3(-8.82f, 1.8f, 9.5f), 90);
            Table(new Vector3(-5.8f, 0, 10.2f), 3.4f, 1.5f, 0.88f);
            Clue(1, new Vector3(-6.3f, 0.945f, 10.05f), letterMat, new Vector3(0, -14, 0));
            Clue(2, new Vector3(-5.15f, 0.945f, 10.02f), meetingMat, new Vector3(0, 6, 0));
            Lamp(new Vector3(-6.85f, 0.92f, 10.55f), false);
            Box("Desk chair seat", new Vector3(-5.8f, 0.5f, 8.7f), new Vector3(0.65f, 0.14f, 0.65f), darkWood);
            Box("Desk chair back", new Vector3(-5.8f, 0.98f, 8.42f), new Vector3(0.65f, 0.85f, 0.12f), darkWood);
            foreach (float x in new[] { -6.05f, -5.55f }) foreach (float z in new[] { 8.45f, 8.95f })
                Box("Chair leg", new Vector3(x, 0.23f, z), new Vector3(0.07f, 0.46f, 0.07f), darkWood);
            Bookshelf(new Vector3(-7.4f, 0, 12.65f), 180);
            Box("Filing cabinet", new Vector3(-2.4f, 0.68f, 11.9f), new Vector3(1.1f, 1.36f, 0.8f), metal);
            for (int i = 0; i < 3; i++)
            {
                Box("Drawer", new Vector3(-2.4f, 0.25f + i * 0.43f, 11.48f), new Vector3(0.99f, 0.37f, 0.04f), green, false);
                Box("Drawer pull", new Vector3(-2.4f, 0.28f + i * 0.43f, 11.43f), new Vector3(0.25f, 0.04f, 0.06f), brass, false);
            }
            Box("Cardboard box", new Vector3(-7.4f, 0.3f, 5.2f), new Vector3(1.1f, 0.6f, 0.8f), paper);
            WorldText("ARCHIVE / 1997", new Vector3(-7.9f, 0.45f, 4.785f), Vector3.zero, 0.026f);
            Point("Study ceiling bulb", new Vector3(-4.8f, 2.6f, 7.7f), new Color(0.91f, 0.76f, 0.48f), 2.5f, 8);
        }

        static void Kitchen()
        {
            room = Group("04 · Kitchen / safe room");
            var tiles = Mat("Checker linoleum", new Color(0.65f, 0.63f, 0.47f), Tiles());
            tiles.mainTextureScale = new Vector2(8, 8);
            Box("Linoleum", new Vector3(5.2f, 0.009f, -0.5f), new Vector3(7.2f, 0.018f, 8.75f), tiles, false);
            for (int i = 0; i < 4; i++)
            {
                float x = 3.6f + i * 1.3f;
                Box("Lower cabinet", new Vector3(x, 0.45f, 3.15f), new Vector3(1.25f, 0.9f, 1.1f), green);
                Box("Countertop", new Vector3(x, 0.94f, 3.15f), new Vector3(1.3f, 0.1f, 1.18f), cream);
                Box("Cabinet handle", new Vector3(x, 0.73f, 2.55f), new Vector3(0.3f, 0.04f, 0.05f), brass, false);
                Box("Wall cabinet", new Vector3(x, 2.15f, 3.6f), new Vector3(1.2f, 0.9f, 0.48f), green);
            }
            Box("Sink", new Vector3(4.8f, 1.003f, 3.12f), new Vector3(0.8f, 0.025f, 0.68f), metal, false);
            Box("Sink basin", new Vector3(4.8f, 1.02f, 3.12f), new Vector3(0.61f, 0.02f, 0.48f), black, false);
            LowCylinder("Faucet", new Vector3(4.8f, 1.16f, 3.5f), new Vector3(0.07f, 0.32f, 0.07f), metal);
            Box("Fridge", new Vector3(8.1f, 1.02f, 2.9f), new Vector3(1.3f, 2.04f, 1.2f), cream);
            Box("Fridge seam", new Vector3(8.1f, 1.44f, 2.285f), new Vector3(1.22f, 0.035f, 0.02f), black, false);
            Box("Fridge handle", new Vector3(7.65f, 1.05f, 2.23f), new Vector3(0.06f, 0.47f, 0.06f), metal, false);
            Table(new Vector3(6, 0, -1.1f), 2.5f, 1.6f, 0.78f);
            Box("Kitchen folder", new Vector3(6.2f, 0.815f, -1.1f), new Vector3(0.5f, 0.03f, 0.36f), darkWood, false);
            LowCylinder("Mug", new Vector3(5.4f, 0.9f, -0.8f), new Vector3(0.19f, 0.23f, 0.19f), cream);
            Window(new Vector3(8.82f, 1.8f, -1.8f), -90);
            // A familiar landline, rather than modern props.
            Box("Telephone base", new Vector3(2.8f, 0.85f, -4.1f), new Vector3(0.48f, 0.14f, 0.4f), cream);
            Table(new Vector3(2.8f, 0, -4.1f), 1.3f, 0.65f, 0.73f);
            Box("Telephone receiver", new Vector3(2.8f, 0.99f, -4.1f), new Vector3(0.58f, 0.1f, 0.15f), black, false);
            Point("Kitchen sanctuary", new Vector3(5.7f, 2.5f, -0.8f), new Color(1, 0.79f, 0.47f), 4, 9);
            LowCylinder("Kitchen shade", new Vector3(5.7f, 2.7f, -0.8f), new Vector3(0.8f, 0.2f, 0.8f), cream);
        }

        static Texture2D Tiles()
        {
            var t = new Texture2D(32, 32, TextureFormat.RGB24, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float v = (x / 16 + y / 16) % 2 == 0 ? 0.84f : 0.38f;
                if (x % 16 == 0 || y % 16 == 0) v *= 0.55f;
                t.SetPixel(x, y, new Color(v, v, v));
            }
            t.Apply(); return WriteTexture(t, "Linoleum");
        }

        static void Corridor()
        {
            room = Group("05 · Hallway");
            for (int i = 0; i < 4; i++)
            {
                float z = -2.5f + i * 4.5f;
                var light = Point("Hall bulb", new Vector3(0, 2.65f, z), new Color(1, 0.64f, 0.28f), 1.8f, 5.8f);
                hallLights.Add(light);
                LowCylinder("Ceiling fixture", new Vector3(0, 2.89f, z), new Vector3(0.43f, 0.17f, 0.43f), cream);
            }
            Box("End door", new Vector3(0, 1.1f, 12.8f), new Vector3(1.55f, 2.2f, 0.12f), black);
            var fuse = Box("Fuse box", new Vector3(1.29f, 1.45f, 11.3f), new Vector3(0.22f, 0.65f, 0.5f), metal).AddComponent<DemoInteractable>();
            fuse.kind = DemoInteractable.Interaction.FuseBox; fuse.label = "Проверить щиток";
            WorldText("FUSE\n110 V", new Vector3(1.16f, 1.58f, 11.48f), new Vector3(0, -90, 0), 0.027f);
            Picture(new Vector3(-1.34f, 1.8f, 3), 90);
            Box("Entry console", new Vector3(0.89f, 0.52f, -2.7f), new Vector3(0.68f, 1.04f, 1.1f), darkWood);
            LowCylinder("Ceramic bowl", new Vector3(0.89f, 1.12f, -2.7f), new Vector3(0.32f, 0.12f, 0.32f), cream);
        }

        static void People()
        {
            room = Group("06 · Figures");
            var anton = Person("Антон · placeholder", new Vector3(-3.7f, 0, 1.7f), false);
            anton.rotation = Quaternion.Euler(0, 155, 0);
            var actor = anton.gameObject.AddComponent<DemoInteractable>();
            actor.kind = DemoInteractable.Interaction.Brother; actor.label = "Поговорить с Антоном";
            var bodyCollider = anton.gameObject.AddComponent<BoxCollider>();
            bodyCollider.center = new Vector3(0, 0.9f, 0); bodyCollider.size = new Vector3(0.65f, 1.8f, 0.55f);
            game.brother = anton;
            game.threat = Person("Хозяин · prototype silhouette", new Vector3(0, 0, 12.3f), true);
            game.threat.gameObject.SetActive(false);
        }

        static Transform Person(string name, Vector3 position, bool monster)
        {
            var person = new GameObject(name).transform;
            person.SetParent(room); person.position = position;
            var shape = new GameObject("Low polygon figure").transform;
            shape.SetParent(person, false);
            Material coat = monster ? black : green;
            Material skin = monster ? Mat("Ash skin", new Color(0.31f, 0.30f, 0.25f)) : Mat("Skin", new Color(0.57f, 0.43f, 0.31f));
            float stretch = monster ? 1.22f : 1;
            Action<string, Vector3, Vector3, Material> part = (title, p, s, m) =>
            {
                var item = Box(title, p + position, s, m, false);
                item.transform.SetParent(shape, true);
            };
            part("Left shoe", new Vector3(-0.15f, 0.08f, 0.07f), new Vector3(0.2f, 0.16f, 0.4f), black);
            part("Right shoe", new Vector3(0.15f, 0.08f, 0.07f), new Vector3(0.2f, 0.16f, 0.4f), black);
            part("Left leg", new Vector3(-0.15f, 0.5f, 0), new Vector3(0.21f, 0.8f, 0.25f), darkWood);
            part("Right leg", new Vector3(0.15f, 0.5f, 0), new Vector3(0.21f, 0.8f, 0.25f), darkWood);
            part("Torso", new Vector3(0, 1.12f * stretch, 0), new Vector3(0.62f, 0.67f * stretch, 0.32f), coat);
            part("Left arm", new Vector3(-0.4f, 1.06f, 0.05f), new Vector3(0.16f, monster ? 1.2f : 0.68f, 0.21f), coat);
            part("Right arm", new Vector3(0.4f, 1.06f, 0.05f), new Vector3(0.16f, monster ? 1.2f : 0.68f, 0.21f), coat);
            part("Left hand", new Vector3(-0.4f, monster ? 0.39f : 0.69f, 0.05f), new Vector3(0.14f, 0.18f, 0.13f), skin);
            part("Right hand", new Vector3(0.4f, monster ? 0.39f : 0.69f, 0.05f), new Vector3(0.14f, 0.18f, 0.13f), skin);
            var head = LowCylinder("Head", position + new Vector3(0, 1.65f * stretch, monster ? 0.2f : 0), new Vector3(0.32f, 0.4f, 0.31f), skin);
            head.transform.SetParent(shape, true);
            if (!monster)
            {
                part("Hair", new Vector3(0, 1.85f, -0.035f), new Vector3(0.32f, 0.11f, 0.29f), darkWood);
                foreach (float x in new[] { -0.073f, 0.073f }) part("Eye", new Vector3(x, 1.7f, 0.154f), new Vector3(0.035f, 0.025f, 0.018f), black);
                part("Nose", new Vector3(0, 1.65f, 0.17f), new Vector3(0.045f, 0.08f, 0.055f), skin);
            }
            return person;
        }

        static void Player()
        {
            var player = new GameObject("Player · first person").AddComponent<DemoPlayer>();
            player.game = game;
            player.transform.position = new Vector3(0, 0.05f, -3.6f);
            var controller = player.GetComponent<CharacterController>();
            controller.height = 1.75f; controller.radius = 0.26f; controller.center = new Vector3(0, 0.9f, 0);
            controller.stepOffset = 0.25f;
            var camera = new GameObject("Player camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.SetParent(player.transform, false);
            camera.transform.localPosition = new Vector3(0, 1.65f, 0);
            camera.fieldOfView = 68; camera.nearClipPlane = 0.07f; camera.farClipPlane = 42;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = RenderSettings.fogColor;
            camera.allowHDR = false; camera.allowMSAA = false;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            player.view = camera;
            var flashlight = new GameObject("Flashlight").AddComponent<Light>();
            flashlight.transform.SetParent(camera.transform, false);
            flashlight.transform.localPosition = new Vector3(0.16f, -0.1f, 0.12f);
            flashlight.type = LightType.Spot; flashlight.range = 16; flashlight.spotAngle = 57; flashlight.innerSpotAngle = 28;
            flashlight.color = new Color(1, 0.9f, 0.67f); flashlight.intensity = 7;
            flashlight.shadows = LightShadows.Hard;
            flashlight.gameObject.AddComponent<UniversalAdditionalLightData>();
            player.flashlight = flashlight;
            game.player = player;
        }

        static void Clue(int id, Vector3 position, Material mat, Vector3 rotation)
        {
            var clue = Box(DemoGame.EvidenceTitles[id], position, new Vector3(0.52f, 0.025f, 0.4f), paper).AddComponent<DemoInteractable>();
            clue.kind = DemoInteractable.Interaction.Evidence; clue.evidenceId = id;
            clue.label = "Осмотреть · " + DemoGame.EvidenceTitles[id];
            clue.illustration = (Texture2D)mat.mainTexture;
            var top = GameObject.CreatePrimitive(PrimitiveType.Quad);
            top.name = "Readable surface"; top.transform.SetParent(clue.transform, false);
            // Parent is the scaled paper block, so these values are in its unit-cube space.
            top.transform.localPosition = new Vector3(0, 0.55f, 0);
            top.transform.localRotation = Quaternion.Euler(90, 0, 0);
            top.transform.localScale = new Vector3(0.96f, 0.95f, 1);
            top.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(top.GetComponent<Collider>());
            clue.transform.rotation = Quaternion.Euler(rotation);
            game.evidence[id] = clue;
        }

        static Transform Group(string name)
        {
            var group = new GameObject(name).transform; group.SetParent(root); return group;
        }
        static GameObject Box(string name, Vector3 position, Vector3 size, Material mat, bool collider = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.SetParent(room); obj.transform.position = position; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        static GameObject LowCylinder(string name, Vector3 p, Vector3 scale, Material mat)
        {
            var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            obj.transform.SetParent(room); obj.transform.position = p; obj.transform.localScale = scale;
            obj.GetComponent<MeshFilter>().sharedMesh = cylinder; obj.GetComponent<Renderer>().sharedMaterial = mat; return obj;
        }
        static Mesh MakeCylinder()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uvs = new List<Vector2>();
            Action<Vector3, Vector3, Vector3> tri = (a, b, c) =>
            {
                int at = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
                triangles.Add(at); triangles.Add(at + 1); triangles.Add(at + 2);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.right); uvs.Add(Vector2.up);
            };
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4, b = (i + 1) * Mathf.PI / 4;
                var p = new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f);
                var q = new Vector3(Mathf.Cos(b) * 0.5f, -0.5f, Mathf.Sin(b) * 0.5f);
                var r = p + Vector3.up; var s = q + Vector3.up;
                tri(p, r, q); tri(q, r, s); tri(r, Vector3.up * 0.5f, s); tri(q, Vector3.down * 0.5f, p);
            }
            var mesh = new Mesh { name = "Eight-sided low polygon cylinder" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uvs); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return SaveAsset(mesh, Art + "/Cylinder8.asset");
        }
        static Light Point(string name, Vector3 p, Color color, float intensity, float range)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(room); light.transform.position = p;
            light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range;
            light.gameObject.AddComponent<UniversalAdditionalLightData>(); return light;
        }
        static void Table(Vector3 p, float w, float d, float h)
        {
            Box("Tabletop", p + new Vector3(0, h, 0), new Vector3(w, 0.09f, d), darkWood);
            foreach (float x in new[] { -w * 0.42f, w * 0.42f }) foreach (float z in new[] { -d * 0.37f, d * 0.37f })
                Box("Table leg", p + new Vector3(x, h / 2, z), new Vector3(0.1f, h, 0.1f), darkWood);
        }
        static void Lamp(Vector3 p, bool floor)
        {
            float h = floor ? 1.6f : 0.5f;
            LowCylinder("Lamp base", p + new Vector3(0, 0.035f, 0), new Vector3(0.32f, 0.07f, 0.32f), brass);
            LowCylinder("Lamp stem", p + new Vector3(0, h / 2, 0), new Vector3(0.045f, h, 0.045f), brass);
            LowCylinder("Octagonal shade", p + new Vector3(0, h, 0), new Vector3(floor ? 0.65f : 0.43f, 0.32f, floor ? 0.65f : 0.43f), cream);
            Point("Lamp pool", p + new Vector3(0, h - 0.22f, 0.08f), new Color(1, 0.66f, 0.27f), floor ? 2 : 1.3f, floor ? 4 : 3);
        }
        static void Window(Vector3 p, float yaw)
        {
            var parent = new GameObject("Window / rainy night").transform;
            parent.SetParent(room); parent.position = p; parent.rotation = Quaternion.Euler(0, yaw, 0);
            Action<string, Vector3, Vector3, Material> piece = (title, pos, scale, mat) =>
            {
                var obj = Box(title, Vector3.zero, scale, mat, false);
                obj.transform.SetParent(parent, false); obj.transform.localPosition = pos;
            };
            piece("Cold glass", Vector3.zero, new Vector3(2.2f, 1.6f, 0.03f), glass);
            foreach (float x in new[] { -1.15f, 0, 1.15f }) piece("Window muntin", new Vector3(x, 0, -0.03f), new Vector3(0.07f, 1.75f, 0.11f), cream);
            foreach (float y in new[] { -0.85f, 0, 0.85f }) piece("Window frame", new Vector3(0, y, -0.04f), new Vector3(2.4f, 0.07f, 0.12f), cream);
            piece("Sill", new Vector3(0, -0.9f, 0), new Vector3(2.6f, 0.12f, 0.45f), darkWood);
            foreach (float x in new[] { -1.4f, 1.4f }) piece("Heavy curtain", new Vector3(x, 0, -0.12f), new Vector3(0.45f, 2.2f, 0.2f), fabric);
            var fill = Point("Window cold fill", p + parent.forward * -0.5f, new Color(0.28f, 0.47f, 0.67f), 1.3f, 5);
        }
        static void Picture(Vector3 p, float yaw)
        {
            var frame = Box("Family portrait frame", p, new Vector3(0.65f, 0.52f, 0.06f), darkWood, false);
            frame.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var image = GameObject.CreatePrimitive(PrimitiveType.Quad); image.name = "Photograph";
            image.transform.SetParent(frame.transform, false); image.transform.localPosition = new Vector3(0, 0, -0.55f);
            image.transform.localScale = new Vector3(0.82f, 0.8f, 1);
            image.GetComponent<Renderer>().sharedMaterial = photoMat; UnityEngine.Object.DestroyImmediate(image.GetComponent<Collider>());
        }
        static void Bookshelf(Vector3 p, float yaw)
        {
            var group = new GameObject("Bookcase").transform; group.SetParent(room); group.position = p; group.rotation = Quaternion.Euler(0, yaw, 0);
            Action<string, Vector3, Vector3, Material> piece = (title, pos, size, mat) =>
            { var obj = Box(title, Vector3.zero, size, mat); obj.transform.SetParent(group, false); obj.transform.localPosition = pos; };
            foreach (float x in new[] { -0.85f, 0.85f }) piece("Upright", new Vector3(x, 1.05f, 0), new Vector3(0.1f, 2.1f, 0.4f), darkWood);
            piece("Back", new Vector3(0, 1.05f, 0.19f), new Vector3(1.8f, 2.1f, 0.04f), darkWood);
            for (int shelf = 0; shelf < 4; shelf++)
            {
                float y = 0.12f + shelf * 0.58f;
                piece("Shelf", new Vector3(0, y, 0), new Vector3(1.8f, 0.06f, 0.43f), darkWood);
                for (int i = 0; i < 10; i++) piece("Book", new Vector3(-0.72f + i * 0.15f, y + 0.22f, -0.01f), new Vector3(0.11f, 0.31f + i % 3 * 0.055f, 0.25f), i % 3 == 0 ? fabric : i % 3 == 1 ? green : cream);
            }
        }
        static void CRT(Vector3 p)
        {
            Box("CRT television", p + Vector3.up * 0.4f, new Vector3(1.2f, 0.8f, 0.75f), darkWood);
            Box("CRT bezel", p + new Vector3(0, 0.44f, 0.39f), new Vector3(1.06f, 0.66f, 0.04f), black, false);
            Box("CRT curved screen", p + new Vector3(-0.08f, 0.44f, 0.42f), new Vector3(0.77f, 0.53f, 0.03f), glass, false);
            LowCylinder("Channel dial", p + new Vector3(0.43f, 0.55f, 0.43f), new Vector3(0.1f, 0.08f, 0.1f), brass).transform.Rotate(90, 0, 0);
            Point("CRT glow", p + new Vector3(0, 0.5f, 0.9f), new Color(0.29f, 0.47f, 0.46f), 0.6f, 2);
        }
        static void WorldText(string text, Vector3 p, Vector3 rotation, float size)
        {
            var label = new GameObject(text).AddComponent<TextMesh>(); label.transform.SetParent(room);
            label.transform.position = p; label.transform.eulerAngles = rotation;
            label.text = text; label.font = font; label.fontSize = 40; label.characterSize = size;
            label.color = new Color(0.72f, 0.66f, 0.46f);
            label.GetComponent<Renderer>().sharedMaterial = font.material;
        }
    }
}
