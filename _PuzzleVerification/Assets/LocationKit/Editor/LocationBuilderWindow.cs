using System;
using System.Collections.Generic;
using System.Linq;
using Hakaton.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Hakaton.LocationKit.Editor
{
    public sealed class LocationBuilderWindow : EditorWindow
    {
        [SerializeField] int tab, category, finishIndex;
        [SerializeField] float grid = 1, level, yaw;
        [SerializeField] bool surface, preventDuplicate = true, snapY, brush;
        [SerializeField] string selectedId = "floor_wood", search = "", newName = "Новая локация";
        [SerializeField] bool starterRoom = true, starterYard = true, starterRain;
        [SerializeField] LocationKitOperations.RoomOptions room = new LocationKitOperations.RoomOptions();
        [SerializeField] Vector2 paletteScroll, panelScroll, settingsScroll;
        LocationKitCatalog catalog;
        Material selectedMaterial;
        GameObject customPrefab;
        string status = "Выберите деталь и поставьте её кликом в окне Scene.";
        bool help, busy;
        List<LocationKitOperations.Issue> issues;
        readonly string[] categories = { "Архитектура", "Предметы", "Улица", "Свет", "Игра" };
        readonly float[] grids = { 0.25f, 0.5f, 1, 2, 4 };
        readonly string[] gridLabels = { "0,25 м", "0,5 м", "1 м", "2 м", "4 м" };

        [MenuItem("Hakaton/Конструктор локаций")]
        public static LocationBuilderWindow Open()
        {
            var w = GetWindow<LocationBuilderWindow>("Конструктор локаций");
            w.minSize = new Vector2(710, 540);
            if (w.position.width < 850) w.position = new Rect(90, 65, 1000, 760);
            w.Show(); return w;
        }

        void OnEnable()
        {
            catalog = KitLibraryBuilder.Load();
            SceneView.duringSceneGui += DuringScene;
            EditorApplication.playModeStateChanged += PlayChanged;
            Undo.undoRedoPerformed += Repaint;
        }
        void OnDisable()
        {
            SceneView.duringSceneGui -= DuringScene;
            EditorApplication.playModeStateChanged -= PlayChanged;
            Undo.undoRedoPerformed -= Repaint;
            brush = false; SceneView.RepaintAll();
        }
        void PlayChanged(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingEditMode) brush = false; Repaint(); }
        void OnSelectionChange() => Repaint();
        LocationKitCatalog.Item Selected => catalog != null ? catalog.Find(selectedId) : null;

        public void SelectItem(string id, bool painting = true)
        {
            catalog = KitLibraryBuilder.Load();
            var item = catalog != null ? catalog.Find(id) : null;
            if (item == null) return;
            selectedId = id; category = (int)item.category; tab = 0; surface = item.preferSurface; brush = painting;
            status = painting ? item.title + ": ЛКМ — поставить, R — повернуть, Esc — выйти." : item.title;
            Repaint(); SceneView.RepaintAll();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("КОНСТРУКТОР ЛОКАЦИЙ", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 }, GUILayout.Height(28));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Как пользоваться", GUILayout.Width(145))) help = !help;
            }
            GUILayout.Label("Модуль 4 м · стены 3 м · двери и улики уже работают", EditorStyles.miniLabel);
            if (help) EditorGUILayout.HelpBox("1. Выберите деталь на вкладке «Строить».\n2. В Scene: ЛКМ — поставить, R — поворот, Shift+ЛКМ — убрать деталь, Esc — закончить. Alt+мышь оставлен для навигации.\n3. Для столов и улик включайте «По поверхности», уменьшайте шаг сетки.\n4. «Комнаты» собирает каркас целиком. «Локация» добавляет игрока, старт и погоду.\n5. Выходите из кисти через Esc, затем используйте обычные W/E/R, Cmd/Ctrl+D и Delete. Все действия с деталями поддерживают Undo/Redo.", MessageType.Info);
            if (catalog == null)
            {
                EditorGUILayout.HelpBox("Нужна библиотека деталей. Создаётся один раз из материалов и моделей проекта; готовые префабы можно редактировать самостоятельно.", MessageType.Info);
                using (new EditorGUI.DisabledScope(busy || EditorApplication.isPlaying))
                    if (GUILayout.Button(busy ? "Создаю библиотеку…" : "Создать библиотеку деталей", GUILayout.Height(40))) BuildLibrary();
                return;
            }
            tab = GUILayout.Toolbar(tab, new[] { "Строить", "Комнаты", "Локация", "Проверка" }, GUILayout.Height(29));
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || busy))
            {
                if (tab == 0) DrawPalette();
                else
                {
                    panelScroll = EditorGUILayout.BeginScrollView(panelScroll);
                    if (tab == 1) DrawRoom(); else if (tab == 2) DrawLocation(); else DrawValidation();
                    EditorGUILayout.EndScrollView();
                }
            }
            if (EditorApplication.isPlaying) EditorGUILayout.HelpBox("Для строительства остановите Play. Игровые изменения не сохраняются в сцену.", MessageType.Info);
            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox(status, MessageType.None);
        }

        void DrawPalette()
        {
            EditorGUILayout.Space(5);
            int next = GUILayout.Toolbar(category, categories, GUILayout.Height(25));
            if (next != category) { category = next; brush = false; }
            search = EditorGUILayout.TextField("Поиск", search);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll);
                    var items = catalog.items.Where(i => i.prefab != null && (int)i.category == category &&
                        (string.IsNullOrWhiteSpace(search) || i.title.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
                    int columns = Mathf.Max(1, Mathf.FloorToInt((position.width - 305) / 132));
                    var style = new GUIStyle(GUI.skin.button) { imagePosition = ImagePosition.ImageAbove, wordWrap = true, fontSize = 11 };
                    for (int at = 0; at < items.Length; at += columns)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            for (int c = 0; c < columns && at + c < items.Length; c++)
                            {
                                var item = items[at + c];
                                var icon = AssetPreview.GetAssetPreview(item.prefab) ?? AssetPreview.GetMiniThumbnail(item.prefab);
                                Color previous = GUI.backgroundColor; if (item.id == selectedId) GUI.backgroundColor = new Color(0.45f, 0.8f, 0.65f);
                                if (GUILayout.Button(new GUIContent(item.title, icon, item.description), style, GUILayout.Width(126), GUILayout.Height(112))) SelectItem(item.id);
                                GUI.backgroundColor = previous;
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                    if (items.Length == 0) EditorGUILayout.HelpBox("Нет деталей по этому запросу.", MessageType.None);
                    EditorGUILayout.EndScrollView();
                }
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(270)))
                {
                    settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll);
                    var item = Selected;
                    GUILayout.Label(item != null ? item.title : "Выберите деталь", EditorStyles.boldLabel);
                    if (item != null) GUILayout.Label(item.description, EditorStyles.wordWrappedMiniLabel);
                    EditorGUILayout.Space(8);
                    int index = Array.FindIndex(grids, v => Mathf.Abs(v - grid) < 0.01f);
                    index = EditorGUILayout.Popup("Шаг сетки", Mathf.Max(0, index), gridLabels); grid = grids[index];
                    level = EditorGUILayout.FloatField("Уровень пола Y", level);
                    surface = EditorGUILayout.Toggle("По поверхности", surface);
                    preventDuplicate = EditorGUILayout.Toggle("Без дублей на месте", preventDuplicate);
                    EditorGUILayout.LabelField("Поворот", Mathf.Repeat(yaw, 360) + "°");
                    using (new EditorGUILayout.HorizontalScope())
                    { if (GUILayout.Button("−90°")) yaw -= 90; if (GUILayout.Button("+90°")) yaw += 90; }
                    var old = GUI.backgroundColor; if (brush) GUI.backgroundColor = new Color(0.4f, 0.8f, 0.6f);
                    if (GUILayout.Button(brush ? "Размещение: ВКЛ · Esc" : "Начать размещение", GUILayout.Height(32))) { brush = !brush; SceneView.RepaintAll(); }
                    GUI.backgroundColor = old;
                    if (GUILayout.Button("Поставить в центр вида")) Run(() =>
                    {
                        var view = SceneView.lastActiveSceneView;
                        if (item != null && view != null) Selection.activeGameObject = LocationKitOperations.Place(item.id, LocationKitOperations.Snap(view.pivot, grid, level, false), yaw, preventDuplicate);
                    });
                    if (item != null && GUILayout.Button("Найти префаб в Project")) EditorGUIUtility.PingObject(item.prefab);
                    EditorGUILayout.Space(10);
                    GUILayout.Label("Выделенные объекты", EditorStyles.boldLabel);
                    snapY = EditorGUILayout.Toggle("Сбрасывать Y на уровень", snapY);
                    if (GUILayout.Button("Выровнять по сетке")) Run(() => LocationKitOperations.SnapSelection(grid, level, snapY));
                    if (GUILayout.Button("Повернуть выделенное +90°")) Run(() => LocationKitOperations.RotateSelection(90));
                    EditorGUILayout.Space(6);
                    DrawMaterial();
                    EditorGUILayout.Space(8);
                    GUILayout.Label("Свои детали", EditorStyles.boldLabel);
                    customPrefab = (GameObject)EditorGUILayout.ObjectField("Префаб", customPrefab, typeof(GameObject), false);
                    using (new EditorGUI.DisabledScope(customPrefab == null))
                        if (GUILayout.Button("Добавить в эту категорию")) RegisterCustom();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Дополнить библиотеку")) BuildLibrary();
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        void DrawMaterial()
        {
            if (catalog.finishes.Count > 0)
            {
                finishIndex = EditorGUILayout.Popup("Отделка", Mathf.Clamp(finishIndex, 0, catalog.finishes.Count - 1), catalog.finishes.Select(f => f.title).ToArray());
                if (GUILayout.Button("Применить отделку к выделенному")) Run(() => LocationKitOperations.PaintSelection(catalog.finishes[finishIndex].material));
            }
            selectedMaterial = (Material)EditorGUILayout.ObjectField("Свой материал", selectedMaterial, typeof(Material), false);
            using (new EditorGUI.DisabledScope(selectedMaterial == null))
                if (GUILayout.Button("Применить свой материал")) Run(() => LocationKitOperations.PaintSelection(selectedMaterial));
        }

        void DrawRoom()
        {
            EditorGUILayout.HelpBox("Каркас создаётся отдельной группой. После сборки каждую стену, дверь или плитку можно двигать и удалять отдельно. Создание комнаты отменяется одним Undo.", MessageType.Info);
            room.width = EditorGUILayout.IntSlider("Ширина · секции", room.width, 1, 12);
            room.depth = EditorGUILayout.IntSlider("Глубина · секции", room.depth, 1, 12);
            EditorGUILayout.LabelField("Размер комнаты", room.width * 4 + " × " + room.depth * 4 + " м · высота стен 3 м");
            room.origin = EditorGUILayout.Vector3Field("Левый передний угол", room.origin);
            room.yaw = EditorGUILayout.IntSlider("Поворот · 90°", Mathf.RoundToInt(room.yaw / 90), 0, 3) * 90;
            if (GUILayout.Button("Взять положение из центра Scene"))
            { if (SceneView.lastActiveSceneView != null) room.origin = LocationKitOperations.Snap(SceneView.lastActiveSceneView.pivot, grid, level, false); }
            room.door = EditorGUILayout.Toggle("Входная дверь спереди", room.door);
            if (room.door) room.doorSection = EditorGUILayout.IntSlider("Секция входа", room.doorSection, 1, room.width);
            room.windows = EditorGUILayout.Toggle("Окна сзади и сбоку", room.windows);
            room.ceiling = EditorGUILayout.Toggle("Потолок", room.ceiling);
            room.roof = EditorGUILayout.Toggle("Двускатная крыша", room.roof);
            room.shelter = EditorGUILayout.Toggle("Приглушать дождь внутри", room.shelter);
            room.wallFinish = (Material)EditorGUILayout.ObjectField("Материал стен", room.wallFinish, typeof(Material), false);
            EditorGUILayout.Space(12);
            if (GUILayout.Button("Собрать новую комнату", GUILayout.Height(40))) Run(() => { brush = false; LocationKitOperations.CreateRoom(room); status = "Комната собрана. Выделите группу и нажмите F, чтобы приблизиться."; });
        }

        void DrawLocation()
        {
            GUILayout.Label("Новая сцена", EditorStyles.boldLabel);
            newName = EditorGUILayout.TextField("Название", newName);
            starterRoom = EditorGUILayout.Toggle("Стартовая комната 8×8", starterRoom);
            starterYard = EditorGUILayout.Toggle("Площадка вокруг", starterYard);
            starterRain = EditorGUILayout.Toggle("Дождь и ночной туман", starterRain);
            if (SceneManager.GetActiveScene().isDirty) EditorGUILayout.HelpBox("Перед созданием новой сцены сохраните текущую: Cmd/Ctrl+S.", MessageType.Info);
            using (new EditorGUI.DisabledScope(SceneManager.GetActiveScene().isDirty))
                if (GUILayout.Button("Создать отдельную локацию", GUILayout.Height(36))) Run(() =>
                { brush = false; string path = LocationKitOperations.CreateLocation(newName, starterRoom, starterYard, starterRain); status = "Создана сцена: " + path; });
            EditorGUILayout.Space(15);
            GUILayout.Label("Текущая локация", EditorStyles.boldLabel);
            var root = LocationKitOperations.CurrentRoot;
            if (root == null || root.game == null)
                if (GUILayout.Button("Подключить игрока, интерфейс и доску", GUILayout.Height(32))) Run(LocationKitOperations.AddPlayerRig);
            root = LocationKitOperations.CurrentRoot;
            if (root != null)
            {
                var so = new SerializedObject(root); so.Update();
                EditorGUILayout.PropertyField(so.FindProperty("locationName"), new GUIContent("Название локации"));
                EditorGUILayout.PropertyField(so.FindProperty("customPresentation"), new GUIContent("Свой вводный экран"));
                if (root.customPresentation)
                { EditorGUILayout.PropertyField(so.FindProperty("introduction"), new GUIContent("Вводный текст")); EditorGUILayout.PropertyField(so.FindProperty("objective"), new GUIContent("Задача игрока")); }
                if (so.ApplyModifiedProperties()) { LocationKitOperations.RefreshConnections(root); EditorSceneManager.MarkSceneDirty(root.gameObject.scene); }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Выбрать старт")) { if (root.spawn != null) { Selection.activeGameObject = root.spawn.gameObject; SceneView.lastActiveSceneView?.FrameSelected(); } }
                    if (GUILayout.Button("Старт из центра вида")) SpawnAtView();
                }
                if (GUILayout.Button("Play из этой точки", GUILayout.Height(32))) Run(() => { SpawnAtView(); brush = false; EditorApplication.isPlaying = true; });
                EditorGUILayout.Space(12);
                GUILayout.Label("Свет и погода", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Туманная ночь")) Run(() => LocationKitOperations.SetEnvironment(0));
                    if (GUILayout.Button("Тёплый дом")) Run(() => LocationKitOperations.SetEnvironment(1));
                    if (GUILayout.Button("Пасмурный день")) Run(() => LocationKitOperations.SetEnvironment(2));
                }
                var env = root.GetComponent<LocationEnvironment>();
                if (env != null)
                {
                    var settings = new SerializedObject(env); settings.Update();
                    EditorGUILayout.PropertyField(settings.FindProperty("fog"), new GUIContent("Туман"));
                    EditorGUILayout.PropertyField(settings.FindProperty("fogColor"), new GUIContent("Цвет тумана"));
                    EditorGUILayout.PropertyField(settings.FindProperty("fogStart"), new GUIContent("Начало тумана, м"));
                    EditorGUILayout.PropertyField(settings.FindProperty("fogEnd"), new GUIContent("Конец тумана, м"));
                    EditorGUILayout.PropertyField(settings.FindProperty("ambientColor"), new GUIContent("Общий свет"));
                    EditorGUILayout.PropertyField(settings.FindProperty("mainLightIntensity"), new GUIContent("Яркость неба"));
                    EditorGUILayout.PropertyField(settings.FindProperty("rain"), new GUIContent("Дождь"));
                    if (env.rain)
                    { EditorGUILayout.PropertyField(settings.FindProperty("rainArea"), new GUIContent("Площадь дождя X/Z")); EditorGUILayout.PropertyField(settings.FindProperty("rainHeight"), new GUIContent("Высота дождя")); EditorGUILayout.PropertyField(settings.FindProperty("rainRate"), new GUIContent("Количество капель")); }
                    if (settings.ApplyModifiedProperties()) LocationKitOperations.ApplyEnvironment(env);
                }
            }
            EditorGUILayout.Space(12);
            if (GUILayout.Button("Сохранить текущую сцену", GUILayout.Height(32))) Run(() =>
            {
                var scene = SceneManager.GetActiveScene();
                if (string.IsNullOrWhiteSpace(scene.path)) EditorSceneManager.SaveScene(scene, EditorUtility.SaveFilePanelInProject("Сохранить локацию", "Location", "unity", "Выберите имя сцены"));
                else EditorSceneManager.SaveScene(scene);
                status = "Сцена сохранена.";
            });
        }

        void DrawValidation()
        {
            EditorGUILayout.HelpBox("Проверка ищет потерянные скрипты, отсутствующий пол под стартом, лишних игроков и повторяющиеся улики. Клик по сообщению выделяет объект.", MessageType.Info);
            if (GUILayout.Button("Проверить текущую сцену", GUILayout.Height(36))) issues = LocationKitOperations.Validate();
            if (GUILayout.Button("Обновить связи игровых объектов")) Run(() => LocationKitOperations.RefreshConnections(LocationKitOperations.EnsureRoot()));
            if (issues != null)
            {
                if (issues.Count == 0) EditorGUILayout.HelpBox("Проблем не найдено. Можно запускать Play.", MessageType.Info);
                foreach (var issue in issues)
                {
                    EditorGUILayout.HelpBox(issue.message, issue.error ? MessageType.Error : MessageType.Warning);
                    if (issue.context != null && GUILayout.Button("Выделить: " + issue.context.name)) { Selection.activeObject = issue.context; SceneView.lastActiveSceneView?.FrameSelected(); }
                }
            }
        }

        void SpawnAtView()
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null) return;
            var p = LocationKitOperations.Snap(view.pivot, grid, level + 0.08f, false);
            LocationKitOperations.SetSpawn(p, view.rotation.eulerAngles.y);
            status = "Старт перемещён к центру вида Scene.";
        }

        void BuildLibrary()
        {
            if (busy) return; busy = true;
            EditorApplication.delayCall += () =>
            {
                try { catalog = KitLibraryBuilder.Build(); status = "Библиотека готова: " + catalog.items.Count + " деталей. Готовые префабы и пользовательские изменения сохранены."; }
                catch (Exception ex) { status = ex.Message; Debug.LogException(ex); }
                finally { busy = false; Repaint(); }
            };
        }
        void RegisterCustom()
        {
            Run(() =>
            {
                if (customPrefab == null || !EditorUtility.IsPersistent(customPrefab) || PrefabUtility.GetPrefabAssetType(customPrefab) == PrefabAssetType.NotAPrefab)
                    throw new InvalidOperationException("Перетащите готовый префаб из окна Project.");
                string id = "custom_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(customPrefab));
                var existing = catalog.Find(id);
                if (existing == null)
                {
                    Undo.RecordObject(catalog, "Добавить свой префаб");
                    existing = new LocationKitCatalog.Item { id = id, title = customPrefab.name, description = "Пользовательский префаб.", category = (KitCategory)category,
                        prefab = customPrefab, preferSurface = category == (int)KitCategory.Props, bounds = KitLibraryBuilder.CalculateBounds(customPrefab) };
                    catalog.items.Add(existing); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
                }
                SelectItem(existing.id); status = "Префаб добавлен в палитру.";
            });
        }
        void Run(Action action)
        {
            try { action(); Repaint(); }
            catch (InvalidOperationException ex) { status = ex.Message; Repaint(); }
            catch (Exception ex) { status = ex.Message; Debug.LogException(ex); Repaint(); }
        }

        void DuringScene(SceneView view)
        {
            if (!brush || EditorApplication.isPlaying || Selected == null || PrefabStageUtility.GetCurrentPrefabStage() != null) return;
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { brush = false; e.Use(); Repaint(); view.Repaint(); return; }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R) { yaw += e.shift ? -90 : 90; e.Use(); Repaint(); view.Repaint(); }
            if (e.alt || e.button == 1 || e.button == 2) return;
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 point;
            var plane = new Plane(Vector3.up, new Vector3(0, level, 0));
            bool onSurface = false;
            if (surface && Physics.Raycast(ray, out var hit, 2000, ~0, QueryTriggerInteraction.Ignore)) { point = hit.point; onSurface = true; }
            else if (plane.Raycast(ray, out float enter)) point = ray.GetPoint(enter);
            else return;
            point = LocationKitOperations.Snap(point, grid, level, onSurface);
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (e.type == EventType.MouseMove) view.Repaint();
            if (e.type == EventType.Repaint)
            {
                var previousDepth = Handles.zTest;
                Handles.zTest = CompareFunction.LessEqual;
                var color = e.shift ? new Color(1, 0.35f, 0.25f) : new Color(0.35f, 1, 0.7f);
                Quaternion rot = Quaternion.Euler(0, yaw, 0) * Selected.prefab.transform.localRotation;
                using (new Handles.DrawingScope(color, Matrix4x4.TRS(point, rot, Selected.prefab.transform.localScale)))
                {
                    Handles.DrawWireCube(Selected.bounds.center, Selected.bounds.size);
                    Handles.DrawLine(Vector3.zero, Vector3.back * 1.2f);
                }
                using (new Handles.DrawingScope(new Color(0.5f, 0.8f, 0.8f, 0.22f)))
                    for (int i = -10; i <= 10; i++)
                    {
                        Vector3 center = new Vector3(point.x, level + 0.005f, point.z);
                        Handles.DrawLine(center + new Vector3(i * grid, 0, -10 * grid), center + new Vector3(i * grid, 0, 10 * grid));
                        Handles.DrawLine(center + new Vector3(-10 * grid, 0, i * grid), center + new Vector3(10 * grid, 0, i * grid));
                    }
                Handles.Label(point + Vector3.up * (Mathf.Max(0.4f, Selected.bounds.max.y) + 0.3f), Selected.title + " · " + Mathf.Repeat(yaw, 360) + "°");
                Handles.BeginGUI();
                GUI.Box(new Rect(12, 12, 405, 47), Selected.title + "\nЛКМ — поставить · R — поворот · Shift+ЛКМ — убрать · Esc — выход");
                Handles.EndGUI();
                Handles.zTest = previousDepth;
            }
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (e.shift)
                {
                    if (Physics.Raycast(ray, out var eraseHit, 2000, ~0, QueryTriggerInteraction.Ignore))
                    {
                        var piece = eraseHit.collider.GetComponentInParent<LocationPiece>();
                        if (piece != null) Run(() => LocationKitOperations.Delete(piece));
                    }
                }
                else Run(() => { Selection.activeGameObject = LocationKitOperations.Place(Selected.id, point, yaw, preventDuplicate); status = "Поставлено: " + Selected.title; });
                e.Use(); view.Repaint();
            }
        }
    }
}
