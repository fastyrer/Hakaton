using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Hakaton.Demo.Editor
{
    public static class ArrivalSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/HouseArrival.unity";
        const string Art = "Assets/Demo/Art/Arrival";
        static Transform outside;
        static Material siding, trim, roof, grass, asphalt, concrete, timber, bark, leaves, black, white, chrome, window, warmWindow, red, blue, amber;
        static Mesh cylinder;

        [MenuItem("Hakaton/Build arrival / yard and police car")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the active scene first.");
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(DemoSceneBuilder.ScenePath, OpenSceneMode.Single);
            // Work in a separate scene; the original detective prototype stays intact.
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            var game = UnityEngine.Object.FindAnyObjectByType<DemoGame>();
            game.arrivalScene = true;
            if (game.brother != null) UnityEngine.Object.DestroyImmediate(game.brother.gameObject);
            if (game.threat != null) UnityEngine.Object.DestroyImmediate(game.threat.gameObject);
            game.brother = game.threat = null;
            foreach (var item in UnityEngine.Object.FindObjectsByType<DemoInteractable>())
            {
                if (item is DemoDoor door) { door.kitchenDoor = false; continue; }
                if (item.kind == DemoInteractable.Interaction.Evidence) continue;
                if (item.kind == DemoInteractable.Interaction.FrontDoor) UnityEngine.Object.DestroyImmediate(item.gameObject);
                else UnityEngine.Object.DestroyImmediate(item);
            }
            BoardSceneSetup.Configure(game);
            outside = new GameObject("ARRIVAL · yard and exterior").transform;
            Materials();
            OpenEntrance();
            HouseExterior();
            Yard();
            PoliceCar();
            Weather(game);
            game.player.transform.SetPositionAndRotation(new Vector3(-3, 0.08f, -27.2f), Quaternion.Euler(0, 15, 0));
            game.player.flashlight.enabled = false;
            game.player.view.farClipPlane = 85;
            game.player.view.fieldOfView = 76;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.24f, 0.30f, 0.34f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.21f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.09f, 0.12f, 0.14f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.10f, 0.145f, 0.17f);
            RenderSettings.fogStartDistance = 13;
            RenderSettings.fogEndDistance = 53;
            game.player.view.backgroundColor = RenderSettings.fogColor;
            var moon = new GameObject("Clouded moonlight").AddComponent<Light>();
            moon.transform.SetParent(outside, false);
            moon.transform.rotation = Quaternion.Euler(48, -34, 0);
            moon.type = LightType.Directional;
            moon.color = new Color(0.53f, 0.68f, 0.82f);
            moon.intensity = 0.55f;
            moon.shadows = LightShadows.Hard;
            moon.gameObject.AddComponent<UniversalAdditionalLightData>();
            EditorUtility.SetDirty(game);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(DemoSceneBuilder.ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 1.7f, -9), Quaternion.Euler(17, -12, 0), 24);
            Debug.Log("HouseArrival saved: accessible house, yard, patrol car, rain, fog and destination placeholders.");
        }

        static void Materials()
        {
            siding = Material("Weathered_siding", new Color(0.39f, 0.43f, 0.39f), Texture("Siding", 0));
            trim = Material("Painted_trim", new Color(0.68f, 0.68f, 0.56f));
            roof = Material("Roof_shingles", new Color(0.19f, 0.22f, 0.24f), Texture("Shingles", 1));
            grass = Material("Wet_grass", new Color(0.15f, 0.23f, 0.17f), Texture("Grass", 2));
            grass.mainTextureScale = new Vector2(30, 38);
            asphalt = Material("Wet_asphalt", new Color(0.15f, 0.18f, 0.19f), Texture("Asphalt", 2));
            asphalt.mainTextureScale = new Vector2(4, 12);
            asphalt.SetFloat("_Smoothness", 0.55f);
            concrete = Material("Concrete", new Color(0.37f, 0.41f, 0.40f), Texture("Concrete", 2));
            timber = Material("Fence_wood", new Color(0.29f, 0.30f, 0.24f), Texture("Fence", 0));
            bark = Material("Tree_bark", new Color(0.18f, 0.16f, 0.12f));
            leaves = Material("Dark_foliage", new Color(0.09f, 0.17f, 0.13f));
            black = Material("Patrol_black", new Color(0.025f, 0.038f, 0.045f));
            black.SetFloat("_Smoothness", 0.4f);
            white = Material("Patrol_ivory", new Color(0.74f, 0.76f, 0.70f));
            chrome = Material("Dull_chrome", new Color(0.38f, 0.44f, 0.45f));
            window = Material("Patrol_windows", new Color(0.075f, 0.14f, 0.17f));
            warmWindow = Emission("Warm_window", new Color(0.42f, 0.32f, 0.16f), new Color(0.24f, 0.15f, 0.055f));
            red = Emission("Lightbar_red", new Color(0.55f, 0.035f, 0.02f), new Color(0.7f, 0.02f, 0.01f));
            blue = Emission("Lightbar_blue", new Color(0.025f, 0.12f, 0.42f), new Color(0.01f, 0.07f, 0.6f));
            amber = Emission("Headlamps", new Color(0.85f, 0.78f, 0.49f), new Color(0.7f, 0.6f, 0.3f));
            cylinder = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Demo/Art/Generated/Cylinder8.asset");
        }

        static Material Material(string name, Color color, Texture2D texture = null)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.05f);
            if (texture != null) m.SetTexture("_BaseMap", texture);
            return Save(m, name + ".mat");
        }

        static Material Emission(string name, Color color, Color emission)
        {
            var m = Material(name, color);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            EditorUtility.SetDirty(m);
            return m;
        }

        static T Save<T>(T asset, string file) where T : UnityEngine.Object
        {
            string path = Art + "/" + file;
            asset.name = Path.GetFileNameWithoutExtension(file);
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                UnityEngine.Object.DestroyImmediate(asset);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Texture2D Texture(string name, int pattern)
        {
            var t = new Texture2D(64, 64, TextureFormat.RGB24, false);
            var random = new System.Random(215 + name.Length);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float value = 0.74f + (float)random.NextDouble() * 0.26f;
                if (pattern == 0 && y % 8 == 0) value *= 0.45f;
                if (pattern == 1 && (y % 16 == 0 || (x + ((y / 16) % 2) * 16) % 32 == 0)) value *= 0.35f;
                t.SetPixel(x, y, new Color(value, value, value));
            }
            t.Apply();
            string path = Art + "/" + name + ".png";
            File.WriteAllBytes(path, t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static GameObject Box(Transform parent, string name, Vector3 p, Vector3 size, Material m, bool collision = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = p;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = m;
            if (!collision) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }

        static Transform Group(string name)
        {
            var result = new GameObject(name).transform;
            result.SetParent(outside, false);
            return result;
        }

        static void OpenEntrance()
        {
            var architecture = GameObject.Find("01 · Architecture").transform;
            var remove = new List<GameObject>();
            foreach (Transform item in architecture)
                if (Mathf.Abs(item.position.z + 5) < 0.01f && Mathf.Abs(item.position.x) < 0.01f && item.localScale.x > 17)
                    remove.Add(item.gameObject);
            foreach (var item in remove) UnityEngine.Object.DestroyImmediate(item);
            foreach (float x in new[] { -4.96f, 4.96f })
            {
                Box(architecture, "Front wall / entry opening", new Vector3(x, 1.5f, -5), new Vector3(8.08f, 3, 0.25f), siding);
                Box(architecture, "Entry skirting", new Vector3(x, 0.1f, -5), new Vector3(8.08f, 0.2f, 0.3f), timber);
            }
            Box(architecture, "Entry header", new Vector3(0, 2.66f, -5), new Vector3(1.84f, 0.68f, 0.25f), siding);
            var hinge = new GameObject("Front entrance · working door").transform;
            hinge.SetParent(architecture, false);
            hinge.position = new Vector3(-0.86f, 0, -5.05f);
            var door = hinge.gameObject.AddComponent<DemoDoor>();
            door.label = "Входная дверь";
            door.openAngle = -105;
            Box(hinge, "Front door leaf", new Vector3(0.85f, 1.12f, 0), new Vector3(1.7f, 2.24f, 0.13f), timber);
            foreach (float x in new[] { 0.44f, 1.25f })
                Box(hinge, "Recessed door panel", new Vector3(x, 1.12f, -0.075f), new Vector3(0.64f, 1.65f, 0.025f), siding, false);
            foreach (float z in new[] { -0.12f, 0.12f })
                Box(hinge, "Door handle", new Vector3(1.5f, 1.05f, z), new Vector3(0.13f, 0.07f, 0.12f), chrome, false);
            door.SetOpen(false);
            foreach (float x in new[] { -0.98f, 0.98f })
                Box(architecture, "Entry casing", new Vector3(x, 1.17f, -5.16f), new Vector3(0.17f, 2.34f, 0.22f), trim, false);
            Box(architecture, "Entry lintel", new Vector3(0, 2.3f, -5.16f), new Vector3(2.12f, 0.17f, 0.22f), trim, false);
        }

        static void HouseExterior()
        {
            var house = Group("01 · Clapboard house / porch");
            foreach (float x in new[] { -4.96f, 4.96f })
                Box(house, "Exterior clapboard", new Vector3(x, 1.52f, -5.19f), new Vector3(8.08f, 3, 0.07f), siding, false);
            foreach (float x in new[] { -9.16f, 9.16f })
                Box(house, "Exterior side siding", new Vector3(x, 1.52f, 4), new Vector3(0.06f, 3, 18.4f), siding, false);
            Box(house, "Rear siding", new Vector3(0, 1.52f, 13.17f), new Vector3(18.4f, 3, 0.06f), siding, false);
            foreach (float x in new[] { -9.25f, 9.25f })
                Box(house, "Corner trim", new Vector3(x, 1.55f, -5.23f), new Vector3(0.16f, 3.1f, 0.16f), trim, false);
            var a = new Vector3(-9.6f, 3.12f, -5.8f);
            var b = new Vector3(0, 5.35f, -5.8f);
            var c = new Vector3(9.6f, 3.12f, -5.8f);
            var d = new Vector3(-9.6f, 3.12f, 13.8f);
            var e = new Vector3(0, 5.35f, 13.8f);
            var f = new Vector3(9.6f, 3.12f, 13.8f);
            MeshObject(house, "Gabled shingle roof", new[] { a, d, e, a, e, b, b, e, f, b, f, c }, roof, true);
            MeshObject(house, "Front and rear gable", new[] { a, b, c, d, f, e }, siding, false);
            Box(house, "Chimney", new Vector3(-5.5f, 4.6f, 8), new Vector3(0.85f, 2.5f, 0.95f), timber);
            Box(house, "Chimney cap", new Vector3(-5.5f, 5.9f, 8), new Vector3(1, 0.16f, 1.1f), concrete);
            Box(house, "Porch slab", new Vector3(0, 0.015f, -6.85f), new Vector3(5.8f, 0.15f, 3.7f), concrete);
            Box(house, "Porch awning", new Vector3(0, 2.87f, -6.85f), new Vector3(6, 0.18f, 4), roof);
            foreach (float x in new[] { -2.65f, 2.65f })
            {
                Box(house, "Porch column", new Vector3(x, 1.48f, -8.46f), new Vector3(0.18f, 2.8f, 0.18f), trim);
                Box(house, "Porch balustrade", new Vector3(x, 0.73f, -7.05f), new Vector3(0.1f, 0.1f, 2.5f), trim, false);
                for (int i = 0; i < 7; i++) Box(house, "Baluster", new Vector3(x, 0.43f, -8.15f + i * 0.36f), new Vector3(0.07f, 0.56f, 0.07f), trim, false);
            }
            Box(house, "Doormat", new Vector3(0, 0.097f, -5.68f), new Vector3(1.35f, 0.025f, 0.7f), bark, false);
            Window(house, new Vector3(-5.25f, 1.65f, -5.3f), 0);
            Window(house, new Vector3(5.25f, 1.65f, -5.3f), 0);
            Window(house, new Vector3(-9.27f, 1.65f, 0), 90);
            Window(house, new Vector3(9.27f, 1.65f, 4), -90);
            foreach (float x in new[] { -1.55f, 1.55f })
            {
                Box(house, "Porch lantern", new Vector3(x, 2.05f, -5.45f), new Vector3(0.18f, 0.32f, 0.18f), amber, false);
                Point(house, "Porch warm pool", new Vector3(x, 2.05f, -5.8f), new Color(1, 0.68f, 0.35f), 4, 7);
            }
            Label(house, "215", new Vector3(1.45f, 1.42f, -5.3f), Vector3.zero, 0.065f, trim.color);
            Label(house, "VOLKOV", new Vector3(0, 2.57f, -5.32f), Vector3.zero, 0.042f, trim.color);
        }

        static void Yard()
        {
            var yard = Group("02 · Yard / drive / fence");
            Box(yard, "Ground", new Vector3(0, -0.18f, -4), new Vector3(48, 0.32f, 66), grass);
            Box(yard, "Driveway", new Vector3(8, -0.008f, -17), new Vector3(6.8f, 0.025f, 24), asphalt, false);
            Box(yard, "Front path", new Vector3(0, -0.005f, -18.8f), new Vector3(1.75f, 0.03f, 20.4f), concrete, false);
            for (int i = 0; i < 10; i++)
                Box(yard, "Path expansion joint", new Vector3(0, 0.012f, -10 - i * 1.9f), new Vector3(1.75f, 0.009f, 0.025f), asphalt, false);
            Box(yard, "Residential street", new Vector3(0, -0.02f, -33), new Vector3(100, 0.05f, 5.8f), asphalt, false);
            Box(yard, "Curb", new Vector3(0, 0, -29.8f), new Vector3(44, 0.16f, 0.22f), concrete);
            for (int i = -5; i < 6; i++)
                Box(yard, "Street centre marking", new Vector3(i * 9, 0.015f, -33), new Vector3(4, 0.014f, 0.09f), trim, false);
            Fence(yard, new Vector3(-21, 0, -29), new Vector3(21, 0, -29));
            Fence(yard, new Vector3(-21, 0, -29), new Vector3(-21, 0, 24));
            Fence(yard, new Vector3(21, 0, -29), new Vector3(21, 0, 24));
            Fence(yard, new Vector3(-21, 0, 24), new Vector3(21, 0, 24));
            // Invisible boundaries sit directly behind the visible timber fencing.
            foreach (float x in new[] { -21.15f, 21.15f }) Boundary(yard, new Vector3(x, 2, -2.5f), new Vector3(0.2f, 4, 53));
            foreach (float z in new[] { -29.15f, 24.15f }) Boundary(yard, new Vector3(0, 2, z), new Vector3(42, 4, 0.2f));
            Box(yard, "Mailbox post", new Vector3(3.2f, 0.63f, -26.5f), new Vector3(0.14f, 1.26f, 0.14f), timber);
            Box(yard, "US mailbox", new Vector3(3.2f, 1.35f, -26.5f), new Vector3(0.43f, 0.37f, 0.75f), chrome);
            Box(yard, "Mailbox flag", new Vector3(3.45f, 1.52f, -26.5f), new Vector3(0.04f, 0.28f, 0.15f), red, false);
            Label(yard, "215", new Vector3(3.2f, 1.36f, -26.9f), Vector3.zero, 0.035f, Color.black);
            Box(yard, "Streetlamp post", new Vector3(13.5f, 2.15f, -22), new Vector3(0.12f, 4.3f, 0.12f), black);
            Box(yard, "Streetlamp head", new Vector3(13.5f, 4.3f, -22), new Vector3(0.65f, 0.16f, 0.5f), amber, false);
            Point(yard, "Driveway lamp", new Vector3(13.5f, 4.05f, -22), new Color(0.69f, 0.77f, 0.74f), 6, 15);
            foreach (var p in new[] { new Vector3(-16, 0, -16), new Vector3(-17, 0, 5), new Vector3(17, 0, 14), new Vector3(-12, 0, 19), new Vector3(12, 0, 21) }) Tree(yard, p);
            for (int i = 0; i < 6; i++)
            {
                var patch = Box(yard, "Puddle", new Vector3(6.7f + (i % 2) * 2.1f, 0.009f, -9 - i * 3), new Vector3(1.05f + i % 3 * 0.22f, 0.008f, 1.8f), window, false);
                patch.transform.localRotation = Quaternion.Euler(0, i * 27, 0);
            }
        }

        static void PoliceCar()
        {
            var car = Group("03 · Patrol car / interact to travel");
            car.position = new Vector3(8, 0, -16);
            car.rotation = Quaternion.Euler(0, -12, 0);
            var interaction = car.gameObject.AddComponent<DemoInteractable>();
            interaction.kind = DemoInteractable.Interaction.Car;
            interaction.label = "Полицейская машина · выбрать локацию";
            var collider = car.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 1.02f, 0);
            collider.size = new Vector3(2.22f, 1.72f, 5.35f);
            Box(car, "Chassis", new Vector3(0, 0.56f, 0), new Vector3(2.1f, 0.37f, 5.1f), black, false);
            Box(car, "Ivory door body", new Vector3(0, 0.99f, 0.1f), new Vector3(2.15f, 0.54f, 2.5f), white, false);
            Box(car, "Long black hood", new Vector3(0, 0.97f, -1.86f), new Vector3(2.08f, 0.42f, 1.45f), black, false);
            Box(car, "Black trunk", new Vector3(0, 0.99f, 1.92f), new Vector3(2.08f, 0.42f, 1.23f), black, false);
            Box(car, "Window cabin", new Vector3(0, 1.48f, 0.15f), new Vector3(1.83f, 0.58f, 2.3f), window, false);
            Box(car, "Black roof", new Vector3(0, 1.8f, 0.2f), new Vector3(1.91f, 0.1f, 2.1f), black, false);
            foreach (float x in new[] { -0.94f, 0.94f })
            {
                Box(car, "B pillar", new Vector3(x, 1.49f, 0.18f), new Vector3(0.07f, 0.6f, 0.09f), black, false);
                Box(car, "Window sill", new Vector3(x, 1.2f, 0.1f), new Vector3(0.07f, 0.06f, 2.3f), chrome, false);
                Box(car, "Door mirror", new Vector3(x * 1.24f, 1.3f, -0.86f), new Vector3(0.22f, 0.13f, 0.28f), black, false);
                foreach (float z in new[] { -0.45f, 0.75f }) Box(car, "Door handle", new Vector3(x * 1.16f, 1.08f, z), new Vector3(0.025f, 0.04f, 0.23f), chrome, false);
            }
            foreach (float x in new[] { -1.04f, 1.04f }) foreach (float z in new[] { -1.68f, 1.68f })
            {
                Cylinder(car, "Eight-sided tire", new Vector3(x, 0.43f, z), new Vector3(0.82f, 0.26f, 0.82f), black, new Vector3(0, 0, 90));
                Cylinder(car, "Steel hubcap", new Vector3(x * 1.13f, 0.43f, z), new Vector3(0.44f, 0.03f, 0.44f), chrome, new Vector3(0, 0, 90));
            }
            foreach (float z in new[] { -2.62f, 2.62f }) Box(car, "Chrome bumper", new Vector3(0, 0.57f, z), new Vector3(2.21f, 0.18f, 0.13f), chrome, false);
            Box(car, "Front grille", new Vector3(0, 0.92f, -2.62f), new Vector3(1, 0.22f, 0.045f), chrome, false);
            for (int i = 0; i < 6; i++) Box(car, "Grille slat", new Vector3(-0.4f + i * 0.16f, 0.92f, -2.65f), new Vector3(0.055f, 0.19f, 0.02f), black, false);
            foreach (float x in new[] { -0.76f, 0.76f })
            {
                Box(car, "Headlight", new Vector3(x, 0.94f, -2.64f), new Vector3(0.42f, 0.19f, 0.045f), amber, false);
                Box(car, "Tail light", new Vector3(x, 0.94f, 2.61f), new Vector3(0.39f, 0.17f, 0.045f), red, false);
                var headlight = new GameObject("Headlamp pool").AddComponent<Light>();
                headlight.transform.SetParent(car, false);
                headlight.transform.localPosition = new Vector3(x, 0.9f, -2.73f);
                headlight.transform.localRotation = Quaternion.Euler(12, 180, 0);
                headlight.type = LightType.Spot;
                headlight.color = new Color(1, 0.86f, 0.58f);
                headlight.intensity = 5;
                headlight.range = 12;
                headlight.spotAngle = 52;
                headlight.gameObject.AddComponent<UniversalAdditionalLightData>();
            }
            Box(car, "Lightbar base", new Vector3(0, 1.9f, 0.1f), new Vector3(1.7f, 0.09f, 0.35f), chrome, false);
            Box(car, "Red beacon", new Vector3(-0.48f, 2.04f, 0.1f), new Vector3(0.66f, 0.24f, 0.32f), red, false);
            Box(car, "Blue beacon", new Vector3(0.48f, 2.04f, 0.1f), new Vector3(0.66f, 0.24f, 0.32f), blue, false);
            foreach (float side in new[] { -1f, 1f })
            {
                Label(car, "POLICE", new Vector3(side * 1.09f, 0.91f, 0), new Vector3(0, side < 0 ? 90 : -90, 0), 0.082f, new Color(0.025f, 0.045f, 0.065f));
                Label(car, "N.J. / 027", new Vector3(side * 1.095f, 0.7f, 0), new Vector3(0, side < 0 ? 90 : -90, 0), 0.03f, new Color(0.08f, 0.11f, 0.12f));
            }
            Box(car, "Licence plate", new Vector3(0, 0.6f, -2.71f), new Vector3(0.48f, 0.19f, 0.02f), white, false);
            Label(car, "NJ 027", new Vector3(0, 0.6f, -2.73f), Vector3.zero, 0.024f, Color.black);
        }

        static void Weather(DemoGame game)
        {
            var weatherRoot = Group("04 · Rain and weather");
            var weather = weatherRoot.gameObject.AddComponent<DemoWeather>();
            weather.player = game.player;
            var emitter = new GameObject("Rain / roof collision");
            emitter.transform.SetParent(weatherRoot, false);
            emitter.transform.localPosition = new Vector3(0, 12, -3);
            emitter.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var rain = emitter.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rain.main;
            main.duration = 3;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 1.2f;
            main.startSpeed = 15;
            main.startSize = 0.028f;
            main.startColor = new Color(0.58f, 0.72f, 0.8f, 0.5f);
            main.maxParticles = 5000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = rain.emission;
            emission.rateOverTime = 2600;
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(44, 57, 0.1f);
            var collision = rain.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.collidesWith = ~0;
            collision.enableDynamicColliders = true;
            collision.lifetimeLoss = 1;
            collision.quality = ParticleSystemCollisionQuality.High;
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor("_BaseColor", Color.white);
            var renderer = rain.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Save(material, "Rain.mat");
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 4;
            renderer.velocityScale = 0.025f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            weather.rain = rain;
        }

        static void Window(Transform parent, Vector3 p, float yaw)
        {
            var frame = new GameObject("Warm exterior window").transform;
            frame.SetParent(parent, false); frame.localPosition = p; frame.localRotation = Quaternion.Euler(0, yaw, 0);
            Box(frame, "Window glow", Vector3.zero, new Vector3(2.2f, 1.5f, 0.04f), warmWindow, false);
            foreach (float x in new[] { -1.16f, 0, 1.16f }) Box(frame, "Window upright", new Vector3(x, 0, -0.04f), new Vector3(0.09f, 1.7f, 0.1f), trim, false);
            foreach (float y in new[] { -0.83f, 0, 0.83f }) Box(frame, "Window horizontal", new Vector3(0, y, -0.04f), new Vector3(2.4f, 0.09f, 0.1f), trim, false);
            foreach (float x in new[] { -1.43f, 1.43f }) Box(frame, "Shutter", new Vector3(x, 0, 0), new Vector3(0.35f, 1.6f, 0.1f), timber, false);
        }

        static void Fence(Transform parent, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            int count = Mathf.CeilToInt(delta.magnitude / 0.55f);
            bool alongX = Mathf.Abs(delta.x) > Mathf.Abs(delta.z);
            for (int i = 0; i <= count; i++)
                Box(parent, "Fence paling", Vector3.Lerp(from, to, (float)i / count) + Vector3.up * 0.65f,
                    alongX ? new Vector3(0.16f, 1.3f, 0.085f) : new Vector3(0.085f, 1.3f, 0.16f), timber, false);
            foreach (float y in new[] { 0.32f, 0.93f })
                Box(parent, "Fence rail", (from + to) * 0.5f + Vector3.up * y,
                    alongX ? new Vector3(delta.magnitude, 0.08f, 0.08f) : new Vector3(0.08f, 0.08f, delta.magnitude), timber, false);
        }

        static void Boundary(Transform parent, Vector3 p, Vector3 size)
        {
            var o = new GameObject("Fence collision");
            o.transform.SetParent(parent, false); o.transform.localPosition = p;
            o.AddComponent<BoxCollider>().size = size;
        }

        static void Cylinder(Transform parent, string name, Vector3 p, Vector3 size, Material m, Vector3 rotation)
        {
            var o = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            o.transform.SetParent(parent, false); o.transform.localPosition = p; o.transform.localScale = size;
            o.transform.localRotation = Quaternion.Euler(rotation);
            o.GetComponent<MeshFilter>().sharedMesh = cylinder;
            o.GetComponent<Renderer>().sharedMaterial = m;
        }

        static void Tree(Transform parent, Vector3 p)
        {
            var tree = new GameObject("Low polygon evergreen").transform;
            tree.SetParent(parent, false); tree.localPosition = p;
            Cylinder(tree, "Trunk", new Vector3(0, 1.5f, 0), new Vector3(0.55f, 3, 0.55f), bark, Vector3.zero);
            for (int level = 0; level < 3; level++)
            {
                var points = new List<Vector3>();
                float radius = 2.5f - level * 0.55f;
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4, b = (i + 1) * Mathf.PI / 4;
                    points.Add(new Vector3(Mathf.Cos(a) * radius, 1.5f + level * 1.4f, Mathf.Sin(a) * radius));
                    points.Add(new Vector3(0, 4.4f + level * 1.4f, 0));
                    points.Add(new Vector3(Mathf.Cos(b) * radius, 1.5f + level * 1.4f, Mathf.Sin(b) * radius));
                }
                MeshObject(tree, "Fir tier " + level, points.ToArray(), leaves, false, "FirTier" + level);
            }
        }

        static void MeshObject(Transform parent, string name, Vector3[] points, Material mat, bool collider, string assetName = null)
        {
            var mesh = new Mesh();
            int[] indices = new int[points.Length];
            var uv = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++) { indices[i] = i; uv[i] = new Vector2(points[i].x * 0.4f, points[i].z * 0.4f); }
            mesh.vertices = points; mesh.triangles = indices; mesh.uv = uv;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = Save(mesh, (assetName ?? name.Replace(" ", "_")) + ".asset");
            var o = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            o.transform.SetParent(parent, false);
            o.GetComponent<MeshFilter>().sharedMesh = mesh;
            o.GetComponent<Renderer>().sharedMaterial = mat;
            if (collider) o.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static void Point(Transform parent, string name, Vector3 p, Color color, float intensity, float range)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false); light.transform.localPosition = p;
            light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range;
            light.gameObject.AddComponent<UniversalAdditionalLightData>();
        }

        static void Label(Transform parent, string text, Vector3 p, Vector3 rotation, float size, Color color)
        {
            var label = new GameObject(text).AddComponent<TextMesh>();
            label.transform.SetParent(parent, false); label.transform.localPosition = p; label.transform.localRotation = Quaternion.Euler(rotation);
            label.text = text; label.fontSize = 64; label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.color = color;
            label.GetComponent<Renderer>().sharedMaterial = label.font.material;
        }
    }
}
