using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hakaton.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Hakaton.LocationKit.Editor
{
    public static class KitLibraryBuilder
    {
        public const string CatalogPath = "Assets/LocationKit/LocationKitCatalog.asset";
        public const string PrefabFolder = "Assets/LocationKit/Prefabs";
        static Scene source, work;
        static LocationKitCatalog catalog;
        static List<Renderer> paint;
        static Material oak, walnut, wallpaper, plaster, ivory, charcoal, brass, cloth, green, metal, paper, siding, concrete, grass, asphalt, roof, glass;

        public static LocationKitCatalog Load() => AssetDatabase.LoadAssetAtPath<LocationKitCatalog>(CatalogPath);

        public static LocationKitCatalog Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Остановите Play перед созданием библиотеки.");
            Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh();
            catalog = Load();
            if (catalog == null)
            { catalog = ScriptableObject.CreateInstance<LocationKitCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            try
            {
                source = EditorSceneManager.OpenPreviewScene("Assets/Scenes/HouseArrival.unity");
                work = EditorSceneManager.NewPreviewScene();
                LoadMaterials();
                Add("floor_wood", "Пол · дерево 4×4", KitCategory.Architecture, "Верх пола совпадает с высотой сетки.", r => Box(r, "Пол", new Vector3(0, -0.1f, 0), new Vector3(4, 0.2f, 4), oak, true));
                Add("floor_tile", "Пол · плитка 4×4", KitCategory.Architecture, "Плитка для кухни и подсобных помещений.", r => Box(r, "Плитка", new Vector3(0, -0.1f, 0), new Vector3(4, 0.2f, 4), M("Generated/Checker_linoleum"), true));
                Add("ceiling", "Потолок 4×4", KitCategory.Architecture, "Ставится на уровне пола; потолок будет на высоте 3 м.", r => Box(r, "Потолок", new Vector3(0, 2.95f, 0), new Vector3(4, 0.1f, 4), M("Generated/Ceiling"), true));
                Add("wall_paper", "Стена · обои 4 м", KitCategory.Architecture, "Ширина 4 м, высота 3 м, толщина 20 см.", r => Wall(r, wallpaper));
                Add("wall_siding", "Стена · фасад 4 м", KitCategory.Architecture, "Наружная обшивка с теми же размерами.", r => Wall(r, siding));
                Add("doorway", "Стена с дверью", KitCategory.Architecture, "Проём и работающая дверь: открыть и закрыть через E.", Doorway);
                Add("window_wall", "Стена с окном", KitCategory.Architecture, "Настоящий проём, стекло, рама и подоконник.", WindowWall);
                Add("roof", "Двускатная крыша", KitCategory.Architecture, "Модуль 4×4; закрывает комнату от дождя. Можно растягивать по X/Z.", Roof);
                Add("table", "Стол", KitCategory.Props, "Стол высотой 88 см. Улики можно положить на столешницу.", Table);
                Add("chair", "Стул", KitCategory.Props, "Простой деревянный стул.", Chair);
                Add("sofa", "Диван", KitCategory.Props, "Низкополигональный диван с отдельными подушками.", Sofa);
                Add("bookcase", "Книжный шкаф", KitCategory.Props, "Готовый шкаф с книгами.", r => CloneInto(r, FindSource(t => t.name == "Bookcase").gameObject, Vector3.zero));
                Add("cabinet", "Картотечный шкаф", KitCategory.Props, "Металлический шкаф с ящиками.", Cabinet);
                Add("crate", "Коробка", KitCategory.Props, "Картонная коробка; подходит для стопок.", r => Box(r, "Коробка", new Vector3(0, 0.3f, 0), new Vector3(1, 0.6f, 0.8f), paper, true), true);
                Add("crt", "CRT-телевизор", KitCategory.Props, "Телевизор рубежа 2000 года.", Television, true);
                Add("phone", "Проводной телефон", KitCategory.Props, "Небольшой предмет для столов и тумб.", Phone, true);
                Add("fridge", "Холодильник", KitCategory.Props, "Холодильник с ручкой и отдельной дверцей морозильника.", Fridge);
                Add("rug", "Ковёр", KitCategory.Props, "Плоский ковёр 3×2 м без дополнительного коллайдера.", r => Box(r, "Ковёр", new Vector3(0, 0.008f, 0), new Vector3(3, 0.016f, 2), cloth, true, false), true);
                Add("ground_grass", "Газон 4×4", KitCategory.Exterior, "Участок земли с коллайдером.", r => Box(r, "Земля", new Vector3(0, -0.15f, 0), new Vector3(4, 0.3f, 4), grass, true));
                Add("ground_drive", "Асфальт 4×4", KitCategory.Exterior, "Модуль подъездной дорожки или улицы.", r => Box(r, "Асфальт", new Vector3(0, -0.1f, 0), new Vector3(4, 0.2f, 4), asphalt, true));
                Add("path", "Дорожка 2×4", KitCategory.Exterior, "Бетонная пешеходная дорожка.", r => Box(r, "Дорожка", new Vector3(0, -0.06f, 0), new Vector3(2, 0.12f, 4), concrete, true));
                Add("fence", "Забор 4 м", KitCategory.Exterior, "Секция ограды с коллайдером.", Fence);
                Add("tree", "Дерево", KitCategory.Exterior, "Низкополигональная ель.", r =>
                {
                    var c = CloneInto(r, FindSource(t => t.name == "Low polygon evergreen").gameObject, Vector3.zero);
                    foreach (var renderer in c.GetComponentsInChildren<Renderer>()) if (renderer.name.StartsWith("Fir tier")) paint.Add(renderer);
                    var collider = r.AddComponent<CapsuleCollider>(); collider.center = new Vector3(0, 1.5f, 0); collider.height = 3; collider.radius = 0.3f;
                });
                Add("car", "Полицейская машина", KitCategory.Exterior, "Меню локаций уже подключено: E или ЛКМ по кузову.", r =>
                {
                    var car = CloneInto(r, FindSource(t => t.name == "03 · Patrol car / interact to travel").gameObject, Vector3.zero);
                    foreach (var renderer in car.GetComponentsInChildren<Renderer>())
                        if (renderer.name == "Ivory door body" || renderer.name == "Long black hood" || renderer.name == "Black trunk" || renderer.name == "Black roof") paint.Add(renderer);
                });
                Add("warm_light", "Тёплая лампа", KitCategory.Lights, "Лампа на высоте 2,6 м от уровня пола.", r => Bulb(r, new Color(1, 0.69f, 0.35f)));
                Add("cold_light", "Холодная лампа", KitCategory.Lights, "Холодный свет для коридоров и улицы.", r => Bulb(r, new Color(0.48f, 0.7f, 1)));
                Add("floor_lamp", "Торшер", KitCategory.Lights, "Тёплый торшер с источником света.", r => Lamp(r, 1.6f));
                Add("desk_lamp", "Настольная лампа", KitCategory.Lights, "Лампа для размещения на столе.", r => Lamp(r, 0.5f), true);
                for (int i = 0; i < 3; i++)
                {
                    int id = i;
                    Add("clue_" + i, "Улика · " + DemoGame.EvidenceTitles[i], KitCategory.Gameplay,
                        "Готовая улика для инвентаря и доски. Разместите на столе с режимом «По поверхности».", r =>
                        {
                            var clue = FindSource(t => t.GetComponent<DemoInteractable>() is DemoInteractable item && !(item is DemoDoor) && item.kind == DemoInteractable.Interaction.Evidence && item.evidenceId == id);
                            var copy = CloneInto(r, clue.gameObject, new Vector3(0, 0.0125f, 0));
                            copy.transform.localRotation = Quaternion.identity;
                            paint.Add(copy.GetComponent<Renderer>());
                        }, true);
                }
                Add("spawn", "Точка старта", KitCategory.Gameplay, "Задаёт место появления и направление взгляда. Повторная установка перемещает существующий старт.", r => r.AddComponent<LocationSpawn>());
                Add("shelter", "Зона помещения 4×4", KitCategory.Gameplay, "Приглушает дождь внутри. Растяните объём под размер комнаты; сам дождь останавливают крыша и потолок.", r =>
                {
                    var volume = r.AddComponent<LocationShelter>();
                    var box = volume.GetComponent<BoxCollider>(); box.isTrigger = true; box.center = new Vector3(0, 1.5f, 0); box.size = new Vector3(4, 3, 4);
                });
                Add("rain", "Дождь", KitCategory.Gameplay, "Одна система погоды на локацию; площадь и интенсивность на вкладке «Локация».", Rain);
                if (catalog.playerRig == null) catalog.playerRig = SavePrefab("PlayerRig", PlayerRig);
                AddFinish("Обои", wallpaper); AddFinish("Штукатурка", plaster); AddFinish("Дерево", oak);
                AddFinish("Плитка", M("Generated/Checker_linoleum")); AddFinish("Фасад", siding);
                AddFinish("Бетон", concrete); AddFinish("Асфальт", asphalt);
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
                return catalog;
            }
            finally
            {
                if (work.IsValid()) EditorSceneManager.ClosePreviewScene(work);
                if (source.IsValid()) EditorSceneManager.ClosePreviewScene(source);
            }
        }

        static void LoadMaterials()
        {
            oak = M("Generated/Oak_floor"); walnut = M("Generated/Dark_walnut"); wallpaper = M("Generated/Striped_wallpaper");
            plaster = M("Generated/Faded_plaster"); ivory = M("Generated/Ivory"); charcoal = M("Generated/Charcoal");
            brass = M("Generated/Aged_brass"); cloth = M("Generated/Worn_burgundy"); green = M("Generated/Kitchen_green");
            metal = M("Generated/Dull_enamel"); paper = M("Generated/Old_paper"); siding = M("Arrival/Weathered_siding");
            concrete = M("Arrival/Concrete"); grass = M("Arrival/Wet_grass"); asphalt = M("Arrival/Wet_asphalt");
            roof = M("Arrival/Roof_shingles"); glass = M("Generated/Night_through_window");
        }
        static Material M(string path)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Art/" + path + ".mat");
            if (m == null) throw new InvalidOperationException("Не найден материал: " + path);
            return m;
        }
        static Transform FindSource(Func<Transform, bool> predicate)
        {
            foreach (var root in source.GetRootGameObjects()) foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (predicate(t)) return t;
            throw new InvalidOperationException("Исходный объект библиотеки не найден.");
        }
        static void Add(string id, string title, KitCategory category, string description, Action<GameObject> factory, bool surface = false)
        {
            var existing = catalog.Find(id);
            if (existing != null && existing.prefab != null) return; // Keep manual prefab and catalog edits.
            GameObject prefab = SavePrefab(id, r =>
            {
                paint = new List<Renderer>();
                factory(r);
                var piece = r.AddComponent<LocationPiece>(); piece.kitId = id; piece.category = category;
                piece.paintTargets = paint.Distinct().ToArray();
                piece.localBounds = CalculateBounds(r);
            });
            if (existing == null) { existing = new LocationKitCatalog.Item { id = id }; catalog.items.Add(existing); }
            existing.title = title; existing.description = description; existing.category = category;
            existing.prefab = prefab; existing.preferSurface = surface;
            existing.bounds = id == "rain" ? new Bounds(new Vector3(0, 1.5f, 0), new Vector3(4, 3, 4)) : CalculateBounds(prefab);
        }
        static GameObject SavePrefab(string id, Action<GameObject> factory)
        {
            string path = PrefabFolder + "/" + id + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject(id); SceneManager.MoveGameObjectToScene(root, work);
            try { factory(root); return PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        static void AddFinish(string title, Material material)
        { if (!catalog.finishes.Exists(f => f.title == title)) catalog.finishes.Add(new LocationKitCatalog.Finish { title = title, material = material }); }

        public static Bounds CalculateBounds(GameObject root)
        {
            bool set = false; var bounds = new Bounds(Vector3.zero, Vector3.one);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer) continue;
                Bounds local = renderer.localBounds;
                foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
                {
                    var point = root.transform.InverseTransformPoint(renderer.transform.TransformPoint(local.center + Vector3.Scale(local.extents, new Vector3(x, y, z))));
                    if (!set) { bounds = new Bounds(point, Vector3.zero); set = true; } else bounds.Encapsulate(point);
                }
            }
            if (!set) foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                var box = collider as BoxCollider;
                if (box == null) continue;
                var center = root.transform.InverseTransformPoint(box.transform.TransformPoint(box.center));
                var size = Vector3.Scale(box.size, box.transform.lossyScale);
                if (!set) { bounds = new Bounds(center, size); set = true; } else bounds.Encapsulate(new Bounds(center, size));
            }
            if (!set) bounds = new Bounds(new Vector3(0, 0.9f, 0), new Vector3(0.7f, 1.8f, 0.7f));
            return bounds;
        }

        static GameObject Box(GameObject parent, string name, Vector3 p, Vector3 size, Material material, bool paintable = false, bool collision = true)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name;
            box.transform.SetParent(parent.transform, false); box.transform.localPosition = p; box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            if (paintable) paint.Add(box.GetComponent<Renderer>());
            if (!collision) UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }
        static GameObject CloneInto(GameObject parent, GameObject original, Vector3 position)
        {
            var copy = UnityEngine.Object.Instantiate(original);
            copy.name = original.name; copy.transform.SetParent(parent.transform, false);
            copy.transform.localPosition = position; copy.transform.localRotation = Quaternion.identity;
            return copy;
        }
        static void Wall(GameObject r, Material m)
        {
            Box(r, "Стена", new Vector3(0, 1.5f, 0), new Vector3(4, 3, 0.2f), m, true);
            Box(r, "Плинтус", new Vector3(0, 0.1f, 0), new Vector3(4, 0.2f, 0.24f), walnut, false);
        }
        static void Doorway(GameObject r)
        {
            foreach (float x in new[] { -1.45f, 1.45f }) Box(r, "Стена у проёма", new Vector3(x, 1.5f, 0), new Vector3(1.1f, 3, 0.2f), wallpaper, true);
            Box(r, "Над дверью", new Vector3(0, 2.66f, 0), new Vector3(1.8f, 0.68f, 0.2f), wallpaper, true);
            foreach (float x in new[] { -0.96f, 0.96f }) Box(r, "Наличник", new Vector3(x, 1.16f, 0), new Vector3(0.13f, 2.32f, 0.3f), ivory);
            Box(r, "Верх наличника", new Vector3(0, 2.3f, 0), new Vector3(2.05f, 0.13f, 0.3f), ivory);
            var hinge = new GameObject("Дверь · петля"); hinge.transform.SetParent(r.transform, false); hinge.transform.localPosition = new Vector3(-0.85f, 0, 0);
            var door = hinge.AddComponent<DemoDoor>(); door.label = "Дверь"; door.openAngle = -105;
            Box(hinge, "Полотно двери", new Vector3(0.85f, 1.1f, 0), new Vector3(1.7f, 2.2f, 0.12f), walnut);
            foreach (float z in new[] { -0.11f, 0.11f }) Box(hinge, "Ручка", new Vector3(1.5f, 1.03f, z), new Vector3(0.13f, 0.06f, 0.12f), brass, false, false);
            door.SetOpen(false);
        }
        static void WindowWall(GameObject r)
        {
            foreach (float x in new[] { -1.55f, 1.55f }) Box(r, "Бок окна", new Vector3(x, 1.5f, 0), new Vector3(0.9f, 3, 0.2f), wallpaper, true);
            Box(r, "Под окном", new Vector3(0, 0.475f, 0), new Vector3(2.2f, 0.95f, 0.2f), wallpaper, true);
            Box(r, "Над окном", new Vector3(0, 2.65f, 0), new Vector3(2.2f, 0.7f, 0.2f), wallpaper, true);
            Box(r, "Стекло", new Vector3(0, 1.625f, 0), new Vector3(2.2f, 1.35f, 0.04f), glass);
            foreach (float x in new[] { -1.1f, 0, 1.1f }) Box(r, "Рама", new Vector3(x, 1.625f, 0), new Vector3(0.07f, 1.45f, 0.15f), ivory, false, false);
            foreach (float y in new[] { 0.94f, 1.62f, 2.32f }) Box(r, "Рама", new Vector3(0, y, 0), new Vector3(2.3f, 0.07f, 0.15f), ivory, false, false);
            Box(r, "Подоконник", new Vector3(0, 0.93f, 0), new Vector3(2.5f, 0.12f, 0.46f), walnut);
        }
        static void Roof(GameObject r)
        {
            string path = PrefabFolder + "/RoofMesh.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                Vector3 a = new Vector3(-2.2f, 3.1f, -2.2f), b = new Vector3(0, 4.45f, -2.2f), c = new Vector3(2.2f, 3.1f, -2.2f);
                Vector3 d = new Vector3(-2.2f, 3.1f, 2.2f), e = new Vector3(0, 4.45f, 2.2f), f = new Vector3(2.2f, 3.1f, 2.2f);
                mesh = new Mesh { name = "RoofMesh" };
                mesh.vertices = new[] { a, d, e, a, e, b, b, e, f, b, f, c, a, b, c, d, f, e };
                mesh.triangles = Enumerable.Range(0, 18).ToArray();
                mesh.uv = mesh.vertices.Select(v => new Vector2(v.x, v.z)).ToArray();
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path);
            }
            var body = new GameObject("Крыша", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            body.transform.SetParent(r.transform, false); body.GetComponent<MeshFilter>().sharedMesh = mesh;
            body.GetComponent<Renderer>().sharedMaterial = roof; body.GetComponent<MeshCollider>().sharedMesh = mesh;
            paint.Add(body.GetComponent<Renderer>());
        }
        static void Table(GameObject r)
        {
            Box(r, "Столешница", new Vector3(0, 0.82f, 0), new Vector3(1.8f, 0.12f, 0.9f), walnut, true);
            foreach (float x in new[] { -0.74f, 0.74f }) foreach (float z in new[] { -0.32f, 0.32f }) Box(r, "Ножка", new Vector3(x, 0.38f, z), new Vector3(0.1f, 0.76f, 0.1f), walnut, true);
        }
        static void Chair(GameObject r)
        {
            Box(r, "Сиденье", new Vector3(0, 0.48f, 0), new Vector3(0.65f, 0.12f, 0.65f), walnut, true);
            Box(r, "Спинка", new Vector3(0, 0.95f, 0.28f), new Vector3(0.65f, 0.86f, 0.1f), walnut, true);
            foreach (float x in new[] { -0.25f, 0.25f }) foreach (float z in new[] { -0.25f, 0.25f }) Box(r, "Ножка", new Vector3(x, 0.22f, z), new Vector3(0.07f, 0.44f, 0.07f), walnut, true);
        }
        static void Sofa(GameObject r)
        {
            Box(r, "Основание", new Vector3(0, 0.36f, 0), new Vector3(3, 0.5f, 1.2f), walnut);
            Box(r, "Спинка", new Vector3(0, 0.95f, 0.5f), new Vector3(3, 1.1f, 0.25f), cloth, true);
            foreach (float x in new[] { -1.4f, 1.4f }) Box(r, "Подлокотник", new Vector3(x, 0.7f, 0), new Vector3(0.25f, 0.8f, 1.2f), cloth, true);
            for (int i = -1; i <= 1; i++) Box(r, "Подушка", new Vector3(i * 0.86f, 0.69f, -0.08f), new Vector3(0.82f, 0.2f, 0.95f), cloth, true);
        }
        static void Cabinet(GameObject r)
        {
            Box(r, "Корпус", new Vector3(0, 0.68f, 0), new Vector3(1.1f, 1.36f, 0.8f), metal, true);
            for (int i = 0; i < 3; i++)
            {
                Box(r, "Ящик", new Vector3(0, 0.25f + i * 0.43f, -0.42f), new Vector3(0.99f, 0.37f, 0.04f), green, true, false);
                Box(r, "Ручка", new Vector3(0, 0.27f + i * 0.43f, -0.46f), new Vector3(0.25f, 0.04f, 0.06f), brass, false, false);
            }
        }
        static void Television(GameObject r)
        {
            Box(r, "Телевизор", new Vector3(0, 0.4f, 0), new Vector3(1.2f, 0.8f, 0.75f), walnut, true);
            Box(r, "Экран", new Vector3(-0.08f, 0.44f, -0.39f), new Vector3(0.8f, 0.55f, 0.025f), glass, false, false);
            Box(r, "Кнопки", new Vector3(0.44f, 0.4f, -0.41f), new Vector3(0.1f, 0.25f, 0.035f), brass, false, false);
        }
        static void Phone(GameObject r)
        {
            Box(r, "Основание", new Vector3(0, 0.07f, 0), new Vector3(0.48f, 0.14f, 0.4f), ivory, true);
            Box(r, "Трубка", new Vector3(0, 0.2f, 0), new Vector3(0.58f, 0.1f, 0.15f), charcoal);
        }
        static void Fridge(GameObject r)
        {
            Box(r, "Холодильник", new Vector3(0, 1.02f, 0), new Vector3(1.3f, 2.04f, 1.2f), ivory, true);
            Box(r, "Шов", new Vector3(0, 1.44f, -0.61f), new Vector3(1.22f, 0.035f, 0.02f), charcoal, false, false);
            Box(r, "Ручка", new Vector3(-0.45f, 1.05f, -0.66f), new Vector3(0.06f, 0.47f, 0.06f), metal, false, false);
        }
        static void Fence(GameObject r)
        {
            for (int i = 0; i <= 8; i++) Box(r, "Штакетник", new Vector3(-2 + i * 0.5f, 0.65f, 0), new Vector3(0.15f, 1.3f, 0.08f), M("Arrival/Fence_wood"), true, false);
            foreach (float y in new[] { 0.3f, 0.95f }) Box(r, "Перекладина", new Vector3(0, y, 0.05f), new Vector3(4, 0.08f, 0.08f), walnut, true, false);
            var box = r.AddComponent<BoxCollider>(); box.center = new Vector3(0, 0.65f, 0); box.size = new Vector3(4, 1.3f, 0.15f);
        }
        static void Bulb(GameObject r, Color color)
        {
            Box(r, "Светильник", new Vector3(0, 2.75f, 0), new Vector3(0.42f, 0.1f, 0.42f), ivory);
            Point(r, new Vector3(0, 2.55f, 0), color, 4, 8);
        }
        static void Lamp(GameObject r, float height)
        {
            Box(r, "Основание", new Vector3(0, 0.04f, 0), new Vector3(0.32f, 0.08f, 0.32f), brass);
            Box(r, "Стойка", new Vector3(0, height / 2, 0), new Vector3(0.05f, height, 0.05f), brass);
            Box(r, "Абажур", new Vector3(0, height, 0), new Vector3(0.48f, 0.3f, 0.48f), ivory);
            Point(r, new Vector3(0, height - 0.2f, -0.15f), new Color(1, 0.69f, 0.35f), 2, 4);
        }
        static void Point(GameObject r, Vector3 p, Color color, float intensity, float range)
        {
            var light = new GameObject("Свет").AddComponent<Light>(); light.transform.SetParent(r.transform, false); light.transform.localPosition = p;
            light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range;
            light.gameObject.AddComponent<UniversalAdditionalLightData>();
        }
        static void Rain(GameObject r)
        {
            var original = FindSource(t => t.GetComponent<DemoWeather>() != null);
            var copy = CloneInto(r, original.gameObject, Vector3.zero);
            var w = copy.GetComponent<DemoWeather>(); w.player = null; w.useShelterVolumes = true; w.shelters = Array.Empty<Collider>();
            var emitter = w.rain.transform; emitter.localPosition = new Vector3(0, 12, 0);
            var shape = w.rain.shape; shape.scale = new Vector3(32, 32, 0.1f);
            var emission = w.rain.emission; emission.rateOverTime = 1200;
        }
        static void PlayerRig(GameObject r)
        {
            var original = FindSource(t => t.GetComponent<DemoGame>() != null).GetComponent<DemoGame>();
            var game = r.AddComponent<DemoGame>(); game.arrivalScene = true;
            game.sound = r.AddComponent<DemoSound>(); game.board = r.AddComponent<DetectiveBoard>();
            var player = CloneInto(r, original.player.gameObject, new Vector3(0, 0.08f, 0)).GetComponent<DemoPlayer>();
            player.game = game; game.player = player;
            var ui = new GameObject("Интерфейс").AddComponent<DemoHud>(); ui.transform.SetParent(r.transform, false);
            ui.font = original.hud.font; ui.retroMaterial = original.hud.retroMaterial; game.hud = ui;
            game.evidence = Array.Empty<DemoInteractable>(); game.hallLights = Array.Empty<Light>();
        }
    }
}
