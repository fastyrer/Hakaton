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
    [InitializeOnLoad]
    public static class LocationKitOperations
    {
        [Serializable]
        public sealed class RoomOptions
        {
            public int width = 2, depth = 2, doorSection = 2;
            public Vector3 origin;
            public float yaw;
            public bool door = true, windows = true, ceiling = true, roof, shelter = true;
            public Material wallFinish;
        }
        public sealed class Issue
        {
            public bool error;
            public string message;
            public UnityEngine.Object context;
            public Issue(bool fatal, string text, UnityEngine.Object target = null) { error = fatal; message = text; context = target; }
        }

        static LocationKitOperations() { Undo.undoRedoPerformed += RefreshAfterUndo; }
        static int Begin(string name) { Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName(name); return Undo.GetCurrentGroup(); }
        static void End(int group) { Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); SceneView.RepaintAll(); }
        static LocationKitCatalog Catalog => KitLibraryBuilder.Load() ?? throw new InvalidOperationException("Сначала создайте библиотеку деталей.");
        public static IEnumerable<T> SceneObjects<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true));
        public static LocationRoot CurrentRoot => SceneObjects<LocationRoot>().FirstOrDefault();

        public static Vector3 Snap(Vector3 point, float grid, float level, bool keepHeight)
        {
            grid = Mathf.Max(0.05f, grid);
            return new Vector3(Mathf.Round(point.x / grid) * grid, keepHeight ? point.y : level, Mathf.Round(point.z / grid) * grid);
        }

        public static LocationRoot EnsureRoot()
        {
            var root = CurrentRoot;
            if (root != null) return root;
            var obj = new GameObject("ЛОКАЦИЯ"); Undo.RegisterCreatedObjectUndo(obj, "Создать корень локации");
            root = Undo.AddComponent<LocationRoot>(obj);
            root.locationName = SceneManager.GetActiveScene().name;
            root.game = SceneObjects<DemoGame>().FirstOrDefault();
            root.customPresentation = root.game == null;
            root.architecture = Group(obj.transform, "01 · Архитектура");
            root.props = Group(obj.transform, "02 · Предметы и улица");
            root.lights = Group(obj.transform, "03 · Свет");
            root.gameplay = Group(obj.transform, "04 · Игровые объекты");
            if (root.game != null && root.game.player != null && Catalog.Find("spawn") != null)
            {
                var p = root.game.player.transform;
                root.spawn = Instantiate(Catalog.Find("spawn"), p.position, p.rotation, root.gameplay).GetComponent<LocationSpawn>();
            }
            EditorUtility.SetDirty(root);
            return root;
        }

        static Transform Group(Transform parent, string title)
        {
            var obj = new GameObject(title); Undo.RegisterCreatedObjectUndo(obj, "Создать группу");
            obj.transform.SetParent(parent, false); return obj.transform;
        }
        static Transform ParentFor(LocationRoot root, KitCategory category) => category == KitCategory.Architecture ? root.architecture :
            category == KitCategory.Lights ? root.lights : category == KitCategory.Gameplay ? root.gameplay : root.props;

        public static GameObject Place(string id, Vector3 point, float yaw, bool preventDuplicate = true)
        {
            int group = Begin("Поставить деталь");
            try
            {
                var root = EnsureRoot();
                var item = Catalog.Find(id) ?? throw new InvalidOperationException("Деталь не найдена: " + id);
                if (id == "spawn" && root.spawn != null)
                {
                    Undo.RecordObject(root.spawn.transform, "Переместить старт");
                    root.spawn.transform.SetPositionAndRotation(point, Quaternion.Euler(0, yaw, 0));
                    AlignPlayer(root); return root.spawn.gameObject;
                }
                if (id == "rain")
                {
                    var existing = SceneObjects<DemoWeather>().FirstOrDefault();
                    if (existing != null)
                    {
                        var piece = existing.GetComponentInParent<LocationPiece>();
                        var target = piece != null ? piece.transform : existing.transform;
                        Undo.RecordObject(target, "Переместить дождь"); target.position = point;
                        var env = root.GetComponent<LocationEnvironment>();
                        if (env != null) { Undo.RecordObject(env, "Включить дождь"); env.rain = true; env.Apply(); }
                        else { Undo.RecordObject(existing.gameObject, "Включить дождь"); existing.gameObject.SetActive(true); }
                        return target.gameObject;
                    }
                }
                if (preventDuplicate)
                {
                    var existing = SceneObjects<LocationPiece>().FirstOrDefault(p => p.kitId == id &&
                        Vector3.Distance(p.transform.position, point) < 0.01f && Mathf.Abs(Mathf.DeltaAngle(p.transform.eulerAngles.y, yaw)) < 0.1f);
                    if (existing != null) return existing.gameObject;
                }
                var placed = Instantiate(item, point, Quaternion.Euler(0, yaw, 0), ParentFor(root, item.category));
                if (id == "spawn") { Undo.RecordObject(root, "Точка старта"); root.spawn = placed.GetComponent<LocationSpawn>(); AlignPlayer(root); }
                if (id == "rain")
                {
                    var env = EnsureEnvironment(root);
                    Undo.RecordObject(env, "Включить дождь"); env.rainSystem = placed.GetComponentInChildren<ParticleSystem>(true); env.rain = true; env.Apply();
                }
                RefreshConnections(root);
                return placed;
            }
            finally { End(group); }
        }

        static GameObject Instantiate(LocationKitCatalog.Item item, Vector3 point, Quaternion rotation, Transform parent)
        {
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(item.prefab, SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(obj, "Добавить " + item.title);
            obj.name = item.title;
            obj.transform.SetParent(parent, true);
            obj.transform.SetPositionAndRotation(point, rotation * item.prefab.transform.localRotation);
            var piece = obj.GetComponent<LocationPiece>();
            if (piece == null)
            {
                piece = Undo.AddComponent<LocationPiece>(obj);
                piece.kitId = item.id; piece.category = item.category; piece.localBounds = item.bounds;
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
            return obj;
        }

        public static GameObject CreateRoom(RoomOptions options)
        {
            int group = Begin("Собрать комнату");
            try
            {
                var root = EnsureRoot();
                int width = Mathf.Clamp(options.width, 1, 12), depth = Mathf.Clamp(options.depth, 1, 12);
                var room = new GameObject("Комната " + width * 4 + "×" + depth * 4);
                Undo.RegisterCreatedObjectUndo(room, "Создать комнату"); room.transform.SetParent(root.architecture, false);
                room.transform.SetPositionAndRotation(options.origin, Quaternion.Euler(0, options.yaw, 0));
                Action<string, Vector3, float> add = (id, p, yaw) =>
                {
                    var obj = Instantiate(Catalog.Find(id), room.transform.TransformPoint(p), room.transform.rotation * Quaternion.Euler(0, yaw, 0), room.transform);
                    if (options.wallFinish != null && (id.StartsWith("wall") || id == "doorway" || id == "window_wall")) PaintObject(obj, options.wallFinish);
                };
                for (int x = 0; x < width; x++) for (int z = 0; z < depth; z++)
                {
                    add("floor_wood", new Vector3(2 + x * 4, 0, 2 + z * 4), 0);
                    if (options.ceiling) add("ceiling", new Vector3(2 + x * 4, 0, 2 + z * 4), 0);
                }
                for (int x = 0; x < width; x++)
                {
                    add(options.door && x == Mathf.Clamp(options.doorSection - 1, 0, width - 1) ? "doorway" : "wall_paper", new Vector3(2 + x * 4, 0, 0), 0);
                    add(options.windows && x == width / 2 ? "window_wall" : "wall_paper", new Vector3(2 + x * 4, 0, depth * 4), 180);
                }
                for (int z = 0; z < depth; z++)
                {
                    add(options.windows && z == depth / 2 ? "window_wall" : "wall_paper", new Vector3(0, 0, 2 + z * 4), 90);
                    add("wall_paper", new Vector3(width * 4, 0, 2 + z * 4), 270);
                }
                if (options.roof)
                {
                    var roof = Instantiate(Catalog.Find("roof"), room.transform.TransformPoint(new Vector3(width * 2, 0, depth * 2)), room.transform.rotation, room.transform);
                    roof.transform.localScale = new Vector3(width, 1, depth);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(roof.transform);
                }
                if (options.shelter)
                {
                    var shelter = Instantiate(Catalog.Find("shelter"), room.transform.TransformPoint(new Vector3(width * 2, 0, depth * 2)), room.transform.rotation, room.transform);
                    var box = shelter.GetComponent<BoxCollider>(); Undo.RecordObject(box, "Размер помещения");
                    box.size = new Vector3(width * 4, 3, depth * 4); PrefabUtility.RecordPrefabInstancePropertyModifications(box);
                }
                RefreshConnections(root); Selection.activeGameObject = room; return room;
            }
            finally { End(group); }
        }

        public static void PaintSelection(Material material)
        {
            if (material == null) return;
            int group = Begin("Сменить материал деталей");
            try { foreach (var obj in Selection.gameObjects) if (!EditorUtility.IsPersistent(obj)) PaintObject(obj, material); }
            finally { End(group); }
        }
        static void PaintObject(GameObject obj, Material material)
        {
            var piece = obj.GetComponent<LocationPiece>();
            var targets = piece != null && piece.paintTargets != null && piece.paintTargets.Length > 0 ? piece.paintTargets : obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in targets)
            {
                if (renderer == null || renderer is ParticleSystemRenderer) continue;
                Undo.RecordObject(renderer, "Материал детали");
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
        public static void SnapSelection(float grid, float level, bool snapY)
        {
            int group = Begin("Выровнять по сетке");
            try { foreach (var t in Selection.transforms) if (!EditorUtility.IsPersistent(t)) { Undo.RecordObject(t, "Сетка"); t.position = Snap(t.position, grid, level, !snapY); PrefabUtility.RecordPrefabInstancePropertyModifications(t); } }
            finally { End(group); }
        }
        public static void RotateSelection(float degrees)
        {
            int group = Begin("Повернуть детали");
            try { foreach (var t in Selection.transforms) if (!EditorUtility.IsPersistent(t)) { Undo.RecordObject(t, "Поворот"); t.Rotate(0, degrees, 0, Space.World); PrefabUtility.RecordPrefabInstancePropertyModifications(t); } }
            finally { End(group); }
        }
        public static void Delete(LocationPiece piece)
        {
            if (piece == null || EditorUtility.IsPersistent(piece)) return;
            int group = Begin("Убрать деталь");
            try { Undo.DestroyObjectImmediate(piece.gameObject); RefreshConnections(CurrentRoot); }
            finally { End(group); }
        }

        public static void AddPlayerRig()
        {
            int group = Begin("Добавить игровой комплект");
            try
            {
                var root = EnsureRoot();
                var existing = SceneObjects<DemoGame>().FirstOrDefault();
                Undo.RecordObject(root, "Игровой комплект");
                if (existing == null)
                {
                    var rig = (GameObject)PrefabUtility.InstantiatePrefab(Catalog.playerRig, SceneManager.GetActiveScene());
                    Undo.RegisterCreatedObjectUndo(rig, "Игрок и интерфейс"); rig.name = "Игрок и интерфейс";
                    rig.transform.SetParent(root.gameplay, false); root.game = rig.GetComponent<DemoGame>();
                }
                else root.game = existing;
                if (root.spawn == null)
                {
                    var p = root.game.player != null ? root.game.player.transform.position : new Vector3(0, 0.08f, 0);
                    var marker = Instantiate(Catalog.Find("spawn"), p, Quaternion.identity, root.gameplay);
                    root.spawn = marker.GetComponent<LocationSpawn>();
                }
                RefreshConnections(root); AlignPlayer(root);
            }
            finally { End(group); }
        }

        public static LocationEnvironment EnsureEnvironment(LocationRoot root)
        {
            var env = root.GetComponent<LocationEnvironment>() ?? Undo.AddComponent<LocationEnvironment>(root.gameObject);
            if (env.mainLight == null)
            {
                var obj = new GameObject("Общий свет"); Undo.RegisterCreatedObjectUndo(obj, "Создать общий свет"); obj.transform.SetParent(root.lights, false);
                obj.transform.rotation = Quaternion.Euler(48, -34, 0);
                var light = obj.AddComponent<Light>(); light.type = LightType.Directional; light.shadows = LightShadows.Hard;
                obj.AddComponent<UniversalAdditionalLightData>(); env.mainLight = light;
            }
            if (env.rainSystem == null)
            {
                var existing = SceneObjects<DemoWeather>().FirstOrDefault();
                if (existing != null) env.rainSystem = existing.rain;
                else
                {
                    var rain = Instantiate(Catalog.Find("rain"), Vector3.zero, Quaternion.identity, root.gameplay);
                    env.rainSystem = rain.GetComponentInChildren<ParticleSystem>(true);
                }
            }
            return env;
        }
        public static void SetEnvironment(int preset)
        {
            int group = Begin("Свет и погода");
            try
            {
                var root = EnsureRoot(); var env = EnsureEnvironment(root); Undo.RecordObject(env, "Настройки атмосферы");
                if (preset == 0)
                { env.fog = true; env.fogColor = new Color(0.10f, 0.145f, 0.17f); env.fogStart = 13; env.fogEnd = 53; env.ambientColor = new Color(0.22f, 0.28f, 0.31f); env.mainLightColor = new Color(0.53f, 0.68f, 0.82f); env.mainLightIntensity = 0.55f; env.rain = true; }
                else if (preset == 1)
                { env.fog = true; env.fogColor = new Color(0.08f, 0.075f, 0.055f); env.fogStart = 20; env.fogEnd = 60; env.ambientColor = new Color(0.30f, 0.27f, 0.21f); env.mainLightColor = new Color(1, 0.78f, 0.5f); env.mainLightIntensity = 0.15f; env.rain = false; }
                else
                { env.fog = true; env.fogColor = new Color(0.32f, 0.38f, 0.41f); env.fogStart = 25; env.fogEnd = 80; env.ambientColor = new Color(0.39f, 0.43f, 0.44f); env.mainLightColor = new Color(0.8f, 0.86f, 0.9f); env.mainLightIntensity = 0.85f; env.rain = false; }
                env.Apply(); RefreshConnections(root); EditorUtility.SetDirty(env);
            }
            finally { End(group); }
        }
        public static void ApplyEnvironment(LocationEnvironment env)
        { if (env == null) return; env.Apply(); EditorUtility.SetDirty(env); EditorSceneManager.MarkSceneDirty(env.gameObject.scene); SceneView.RepaintAll(); }
        public static void SetSpawn(Vector3 p, float yaw)
        { Place("spawn", p, yaw, false); }
        static void AlignPlayer(LocationRoot root)
        {
            if (root.game == null || root.game.player == null || root.spawn == null) return;
            var t = root.game.player.transform; Undo.RecordObject(t, "Позиция игрока"); t.SetPositionAndRotation(root.spawn.transform.position, root.spawn.transform.rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        }
        public static void RefreshConnections(LocationRoot root)
        {
            if (root == null) return;
            if (root.game != null) Undo.RecordObject(root.game, "Связи игровых объектов");
            root.Connect();
            if (root.game != null) { EditorUtility.SetDirty(root.game); PrefabUtility.RecordPrefabInstancePropertyModifications(root.game); }
            foreach (var weather in SceneObjects<DemoWeather>()) PrefabUtility.RecordPrefabInstancePropertyModifications(weather);
            EditorUtility.SetDirty(root);
        }
        static void RefreshAfterUndo()
        {
            if (EditorApplication.isPlaying) return;
            var root = CurrentRoot;
            if (root == null) return;
            root.Connect();
            var env = root.GetComponent<LocationEnvironment>(); if (env != null) env.Apply();
            SceneView.RepaintAll();
        }

        public static string CreateLocation(string name, bool room, bool yard, bool rain)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Сначала остановите Play.");
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Сохраните текущую сцену перед созданием новой: Cmd/Ctrl+S.");
            var catalog = Catalog;
            string clean = new string(name.Trim().Where(c => !Path.GetInvalidFileNameChars().Contains(c) && c != '/' && c != '\\').Take(64).ToArray());
            if (string.IsNullOrWhiteSpace(clean)) clean = "Location";
            Directory.CreateDirectory("Assets/Scenes/Locations"); AssetDatabase.Refresh();
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Locations/" + clean + ".unity");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            int group = Begin("Создать новую локацию");
            try
            {
                var root = EnsureRoot(); root.locationName = name.Trim(); root.customPresentation = true;
                AddPlayerRig(); SetSpawn(new Vector3(2, 0.08f, -5), 0);
                SetEnvironment(rain ? 0 : 2);
                if (yard)
                    for (int x = -2; x <= 3; x++) for (int z = -3; z <= 3; z++) Place("ground_grass", new Vector3(x * 4, -0.025f, z * 4), 0);
                else Place("floor_wood", new Vector3(2, 0, -4), 0);
                if (room) CreateRoom(new RoomOptions { width = 2, depth = 2, doorSection = 2, origin = new Vector3(-4, 0, 0), roof = true });
                root.Connect(); EditorUtility.SetDirty(root);
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
                var list = EditorBuildSettings.scenes.ToList();
                if (!list.Any(s => s.path == path)) { list.Add(new EditorBuildSettingsScene(path, true)); EditorBuildSettings.scenes = list.ToArray(); }
                AssetDatabase.SaveAssets();
                Selection.activeGameObject = root.gameObject;
                SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 1, 1), Quaternion.Euler(32, -25, 0), 18);
                return path;
            }
            finally { Undo.CollapseUndoOperations(group); SceneView.RepaintAll(); }
        }

        public static string CreateExample()
        {
            string path = CreateLocation("Мастерская — пример", true, true, true);
            Place("table", new Vector3(-2, 0, 5), 0);
            Place("table", new Vector3(1.5f, 0, 5), 0);
            Place("chair", new Vector3(-2, 0, 3.8f), 180);
            Place("sofa", new Vector3(-2, 0, 1.2f), 180);
            Place("bookcase", new Vector3(-2, 0, 7.6f), 180);
            Place("cabinet", new Vector3(3.2f, 0, 7.3f), 180);
            Place("clue_0", new Vector3(-2, 0.88f, 5), 0);
            Place("clue_1", new Vector3(1.1f, 0.88f, 5), 0);
            Place("clue_2", new Vector3(1.9f, 0.88f, 5), 0);
            Place("desk_lamp", new Vector3(-2.6f, 0.88f, 5.2f), 0);
            Place("warm_light", new Vector3(0, 0, 4), 0);
            Place("warm_light", new Vector3(1.5f, 0, -0.6f), 0);
            Place("car", new Vector3(7, 0, -5), -20);
            for (int x = -2; x <= 2; x++) Place("fence", new Vector3(x * 4, 0, -10), 0);
            Place("tree", new Vector3(-7, 0, 5), 0);
            Place("path", new Vector3(2, 0.01f, -2), 0);
            Place("path", new Vector3(2, 0.01f, -6), 0);
            CurrentRoot.Connect(); EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path); return path;
        }

        public static List<Issue> Validate()
        {
            var issues = new List<Issue>();
            var roots = SceneObjects<LocationRoot>().ToArray();
            if (roots.Length == 0) issues.Add(new Issue(true, "Нет корня локации. Добавьте игровой комплект или первую деталь."));
            if (roots.Length > 1) issues.Add(new Issue(true, "В сцене несколько корней локации. Оставьте один.", roots[1]));
            var games = SceneObjects<DemoGame>().Where(g => g.gameObject.activeInHierarchy).ToArray();
            if (games.Length != 1) issues.Add(new Issue(true, "Нужен один активный игровой комплект; сейчас: " + games.Length));
            var root = roots.FirstOrDefault();
            if (root != null)
            {
                if (root.spawn == null) issues.Add(new Issue(true, "Не задана точка старта.", root));
                else
                {
                    Physics.SyncTransforms();
                    if (!Physics.Raycast(root.spawn.transform.position + Vector3.up * 0.1f, Vector3.down, 5, ~0, QueryTriggerInteraction.Ignore))
                        issues.Add(new Issue(true, "Под точкой старта нет пола с коллайдером.", root.spawn));
                }
            }
            foreach (var game in games)
                if (game.player == null || game.hud == null || game.sound == null || game.player.view == null)
                    issues.Add(new Issue(true, "В игровом комплекте потеряны игрок, камера, интерфейс или звук.", game));
            var clues = SceneObjects<DemoInteractable>().Where(i => !(i is DemoDoor) && i.kind == DemoInteractable.Interaction.Evidence && i.gameObject.activeInHierarchy).ToArray();
            foreach (var set in clues.GroupBy(c => c.evidenceId))
            {
                if (set.Key < 0 || set.Key >= DemoGame.EvidenceTitles.Length) issues.Add(new Issue(true, "Улика имеет неизвестный номер.", set.First()));
                else if (set.Count() > 1) issues.Add(new Issue(false, "Повторяется улика «" + DemoGame.EvidenceTitles[set.Key] + "». Это один предмет инвентаря; оставьте одну копию.", set.Last()));
            }
            int missing = SceneObjects<Transform>().Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if (missing > 0) issues.Add(new Issue(true, "Найдены потерянные скрипты: " + missing));
            foreach (var renderer in SceneObjects<Renderer>()) if (renderer.sharedMaterials.Any(m => m == null))
                issues.Add(new Issue(false, "Не назначен материал: " + renderer.name, renderer));
            if (!SceneObjects<Light>().Any(l => l.gameObject.activeInHierarchy && l.enabled)) issues.Add(new Issue(false, "В сцене нет активного света."));
            return issues;
        }
    }
}
