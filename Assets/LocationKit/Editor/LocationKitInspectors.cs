using System.Linq;
using Hakaton.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hakaton.LocationKit.Editor
{
    [CustomEditor(typeof(LocationPiece)), CanEditMultipleObjects]
    public sealed class LocationPieceInspector : UnityEditor.Editor
    {
        Material material;
        bool advanced;
        public override void OnInspectorGUI()
        {
            var piece = (LocationPiece)target;
            var item = KitLibraryBuilder.Load()?.Find(piece.kitId);
            EditorGUILayout.LabelField(item != null ? item.title : piece.gameObject.name, EditorStyles.boldLabel);
            if (item != null) EditorGUILayout.HelpBox(item.description, MessageType.None);
            if (GUILayout.Button("Продолжить сборку этой деталью")) LocationBuilderWindow.Open().SelectItem(piece.kitId);
            material = (Material)EditorGUILayout.ObjectField("Отделка", material, typeof(Material), false);
            using (new EditorGUI.DisabledScope(material == null || EditorUtility.IsPersistent(piece)))
                if (GUILayout.Button("Применить к выделенным деталям")) LocationKitOperations.PaintSelection(material);
            if (targets.Length == 1)
            {
                var door = piece.GetComponentInChildren<DemoDoor>(true);
                if (door != null)
                {
                    EditorGUILayout.Space(); EditorGUILayout.LabelField("Рабочая дверь", EditorStyles.boldLabel);
                    var so = new SerializedObject(door); so.Update();
                    EditorGUILayout.PropertyField(so.FindProperty("label"), new GUIContent("Подсказка игроку"));
                    EditorGUILayout.PropertyField(so.FindProperty("openAngle"), new GUIContent("Угол открытия"));
                    EditorGUILayout.PropertyField(so.FindProperty("isOpen"), new GUIContent("Открыта в начале"));
                    if (so.ApplyModifiedProperties())
                    {
                        Undo.RecordObject(door.transform, "Положение двери"); door.SetOpen(door.isOpen);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(door.transform);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(door);
                    }
                }
                var clue = piece.GetComponentsInChildren<DemoInteractable>(true).FirstOrDefault(i => !(i is DemoDoor) && i.kind == DemoInteractable.Interaction.Evidence);
                if (clue != null)
                {
                    EditorGUILayout.Space(); EditorGUILayout.LabelField("Улика для доски", EditorStyles.boldLabel);
                    int id = EditorGUILayout.Popup("Тип предмета", Mathf.Clamp(clue.evidenceId, 0, DemoGame.EvidenceTitles.Length - 1), DemoGame.EvidenceTitles);
                    if (id != clue.evidenceId && !EditorUtility.IsPersistent(piece))
                    {
                        Undo.RecordObject(clue, "Сменить улику");
                        string[] files = { "Family_photograph", "Unsent_letter", "Meeting_note" };
                        var image = AssetDatabase.LoadAssetAtPath<Material>("Assets/Demo/Art/Generated/" + files[id % 3] + ".mat");
                        clue.evidenceId = id; clue.label = "Взять / осмотреть · " + DemoGame.EvidenceTitles[id]; clue.illustration = image.mainTexture as Texture2D;
                        var surface = clue.transform.Find("Readable surface");
                        if (surface != null) { var renderer = surface.GetComponent<Renderer>(); Undo.RecordObject(renderer, "Иллюстрация улики"); renderer.sharedMaterial = image; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); }
                        PrefabUtility.RecordPrefabInstancePropertyModifications(clue);
                        LocationKitOperations.RefreshConnections(LocationKitOperations.CurrentRoot);
                    }
                    EditorGUILayout.HelpBox("Доступны улики дома и материалы архивного дела. Повторение одного типа не создаёт второй предмет инвентаря.", MessageType.Info);
                }
                if (piece.category == KitCategory.Lights)
                {
                    var light = piece.GetComponentInChildren<Light>();
                    if (light != null)
                    {
                        EditorGUILayout.Space(); EditorGUILayout.LabelField("Свет", EditorStyles.boldLabel);
                        var so = new SerializedObject(light); so.Update();
                        EditorGUILayout.PropertyField(so.FindProperty("m_Color"), new GUIContent("Цвет"));
                        EditorGUILayout.PropertyField(so.FindProperty("m_Intensity"), new GUIContent("Яркость"));
                        EditorGUILayout.PropertyField(so.FindProperty("m_Range"), new GUIContent("Дальность"));
                        so.ApplyModifiedProperties();
                    }
                }
                var shelter = piece.GetComponent<LocationShelter>();
                if (shelter != null)
                {
                    EditorGUILayout.Space(); EditorGUILayout.LabelField("Помещение", EditorStyles.boldLabel);
                    var box = shelter.GetComponent<BoxCollider>(); var so = new SerializedObject(box); so.Update();
                    EditorGUILayout.PropertyField(so.FindProperty("m_Center"), new GUIContent("Центр объёма"));
                    EditorGUILayout.PropertyField(so.FindProperty("m_Size"), new GUIContent("Размер объёма"));
                    if (so.ApplyModifiedProperties()) LocationKitOperations.RefreshConnections(LocationKitOperations.CurrentRoot);
                }
            }
            advanced = EditorGUILayout.Foldout(advanced, "Поверхности для перекраски");
            if (advanced)
            {
                serializedObject.Update(); EditorGUILayout.PropertyField(serializedObject.FindProperty("paintTargets"), true); serializedObject.ApplyModifiedProperties();
            }
        }
    }

    [CustomEditor(typeof(LocationRoot))]
    public sealed class LocationRootInspector : UnityEditor.Editor
    {
        bool links;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("locationName"), new GUIContent("Название локации"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("customPresentation"), new GUIContent("Свой вводный экран"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("introduction"), new GUIContent("Вводный текст"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("objective"), new GUIContent("Задача игрока"));
            links = EditorGUILayout.Foldout(links, "Связи игровых объектов");
            if (links) foreach (var p in new[] { "game", "spawn", "architecture", "props", "lights", "gameplay" }) EditorGUILayout.PropertyField(serializedObject.FindProperty(p));
            serializedObject.ApplyModifiedProperties();
            if (GUILayout.Button("Открыть конструктор")) LocationBuilderWindow.Open();
            if (GUILayout.Button("Обновить связи")) LocationKitOperations.RefreshConnections((LocationRoot)target);
        }
    }

    [CustomEditor(typeof(LocationEnvironment))]
    public sealed class LocationEnvironmentInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            string[] names = { "fog", "fogColor", "fogStart", "fogEnd", "ambientColor", "mainLightColor", "mainLightIntensity", "rain", "rainArea", "rainHeight", "rainRate" };
            string[] titles = { "Туман", "Цвет тумана", "Начало тумана", "Конец тумана", "Общий свет", "Цвет света неба", "Яркость неба", "Дождь", "Площадь дождя X/Z", "Высота дождя", "Количество капель" };
            for (int i = 0; i < names.Length; i++) EditorGUILayout.PropertyField(serializedObject.FindProperty(names[i]), new GUIContent(titles[i]));
            bool changed = serializedObject.ApplyModifiedProperties();
            var env = (LocationEnvironment)target;
            bool apply = GUILayout.Button("Применить атмосферу");
            if ((changed || apply) && !EditorUtility.IsPersistent(env)) LocationKitOperations.ApplyEnvironment(env);
        }
    }
}
