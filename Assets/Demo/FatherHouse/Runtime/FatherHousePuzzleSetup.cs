using System.Linq;
using Hakaton.LocationKit;
using UnityEngine;

namespace Hakaton.Demo.FatherHouse
{
    [DefaultExecutionOrder(-100)]
    public sealed class FatherHousePuzzleSetup : MonoBehaviour
    {
        public Material keyMaterial;
        public Material lockMaterial;
        public Material lockSymbolMaterial;

        void Awake()
        {
            if (keyMaterial == null) keyMaterial = LoadMaterial("Assets/Demo/Menu/Art/Old_brass.mat", new Color(0.7f, 0.5f, 0.3f));
            if (lockMaterial == null) lockMaterial = LoadMaterial("Assets/Demo/FatherHouse/Art/Dull_steel.mat", new Color(0.25f, 0.28f, 0.25f));
            if (lockSymbolMaterial == null) lockSymbolMaterial = LoadMaterial("Assets/Demo/FatherHouse/Art/Warning_red.mat", new Color(0.42f, 0.07f, 0.035f));

            var location = FindAnyObjectByType<LocationRoot>();
            if (location == null || location.game == null || !location.game.gameObject.activeInHierarchy) return;
            var doors = location.GetComponentsInChildren<DemoDoor>(true);
            foreach (var door in doors) door.lockMode = DemoDoor.LockMode.None;
            ConfigureDoor(doors, "Кладовая", DemoDoor.LockMode.Permanent, null,
                "Дверь наглухо заперта. Отсюда её не открыть.");
            ConfigureDoor(doors, "Кухонная кладовая", DemoDoor.LockMode.Permanent, null,
                "Дверь наглухо заперта. Отсюда её не открыть.");
            var study = ConfigureDoor(doors, "Кабинет отца", DemoDoor.LockMode.Item, CuckooClockPuzzle.StudyKeyItem,
                "Заперто. На замке нарисованы бубны. Нужен подходящий ключ.");
            if (study != null)
            {
                study.unlockedMessage = "Ключ с бубновым знаком подошёл. Кабинет открыт.";
                AddDiamondLock(study);
            }
            ConfigureDoor(doors, "Дверь в мастерскую", DemoDoor.LockMode.Puzzle, "workshop_access",
                "Дверь заперта. Нужно найти способ открыть её.");
            ConfigureDoor(doors, "Спуск в подвал", DemoDoor.LockMode.Puzzle, "basement_access",
                "Спуск заперт. Нужно найти способ открыть его.");
            ConfigureClock(location);
            ConfigurePhone(location);
        }

        void ConfigurePhone(LocationRoot location)
        {
            // Телефон расставлен на сцене вручную; здесь только подключаем взаимодействие.
            foreach (var piece in location.GetComponentsInChildren<LocationPiece>(true))
            {
                if (piece.kitId != "phone" || piece.GetComponent<HousePhone>() != null) continue;
                if (piece.GetComponent<Collider>() == null)
                {
                    var phoneCollider = piece.gameObject.AddComponent<BoxCollider>();
                    phoneCollider.center = new Vector3(0f, 0.13f, 0f);
                    phoneCollider.size = new Vector3(0.6f, 0.26f, 0.42f);
                }
                var interactable = piece.gameObject.AddComponent<HousePhone>();
                interactable.kind = DemoInteractable.Interaction.Custom;
                interactable.evidenceId = -1;
                interactable.label = "Телефон";
            }
        }

        static DemoDoor ConfigureDoor(DemoDoor[] doors, string doorLabel, DemoDoor.LockMode mode, string requirement, string message)
        {
            var door = doors.FirstOrDefault(d => d.label == doorLabel);
            if (door == null)
            {
                Debug.LogWarning("Не найдена дверь для настройки: " + doorLabel);
                return null;
            }
            door.lockMode = mode;
            door.requirement = requirement;
            door.lockedMessage = message;
            door.SetOpen(false);
            return door;
        }

        void ConfigureClock(LocationRoot location)
        {
            Transform clock = location.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "clock bird");
            if (clock == null)
            {
                Debug.LogWarning("Не найден объект clock bird для загадки с ключом.");
                return;
            }
            if (clock.GetComponent<CuckooClockPuzzle>() != null) return;
            var renderers = clock.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(clock.position, Vector3.one);
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (clock.GetComponentsInChildren<Collider>(true).Length == 0)
            {
                var collider = clock.gameObject.AddComponent<BoxCollider>();
                collider.center = clock.InverseTransformPoint(bounds.center);
                Vector3 scale = clock.lossyScale;
                collider.size = new Vector3(bounds.size.x / Mathf.Abs(scale.x), bounds.size.y / Mathf.Abs(scale.y), bounds.size.z / Mathf.Abs(scale.z));
            }
            var dropPoint = new GameObject("Точка падения ключа").transform;
            dropPoint.SetParent(clock, false);
            dropPoint.position = new Vector3(bounds.center.x, 0.12f, bounds.min.z - 0.5f);

            var key = new GameObject("Ключ от кабинета · бубны", typeof(BoxCollider), typeof(QuestPickup)).GetComponent<QuestPickup>();
            key.transform.SetParent(clock, false);
            key.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 0.08f);
            key.kind = DemoInteractable.Interaction.Custom;
            key.evidenceId = -1;
            key.label = "Ключ с бубновым знаком";
            key.itemId = CuckooClockPuzzle.StudyKeyItem;
            key.successMessage = "Получен ключ от кабинета отца. На ручке — бубновый знак.";
            var keyCollider = key.GetComponent<BoxCollider>();
            keyCollider.size = new Vector3(0.42f, 0.18f, 0.25f);
            MakeKey(key.transform);

            var puzzle = clock.gameObject.AddComponent<CuckooClockPuzzle>();
            puzzle.kind = DemoInteractable.Interaction.Custom;
            puzzle.evidenceId = -1;
            puzzle.label = "Часы с кукушкой";
            puzzle.key = key;
            puzzle.keyDropPoint = dropPoint;
            key.gameObject.SetActive(false);
        }

        void MakeKey(Transform parent)
        {
            Box("Стержень ключа", parent, new Vector3(0.02f, 0, 0), new Vector3(0.24f, 0.025f, 0.025f), keyMaterial);
            Box("Верхняя часть ручки", parent, new Vector3(-0.12f, 0, 0.065f), new Vector3(0.12f, 0.025f, 0.025f), keyMaterial);
            Box("Нижняя часть ручки", parent, new Vector3(-0.12f, 0, -0.065f), new Vector3(0.12f, 0.025f, 0.025f), keyMaterial);
            Box("Боковая часть ручки", parent, new Vector3(-0.18f, 0, 0), new Vector3(0.025f, 0.025f, 0.15f), keyMaterial);
            Box("Бородка ключа", parent, new Vector3(0.13f, 0, 0.04f), new Vector3(0.04f, 0.025f, 0.08f), keyMaterial);
            var diamond = Box("Бубновый знак", parent, new Vector3(-0.12f, 0.02f, 0), new Vector3(0.055f, 0.015f, 0.055f), lockSymbolMaterial);
            diamond.transform.localRotation = Quaternion.Euler(0, 45, 0);
        }

        void AddDiamondLock(DemoDoor door)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 position = new Vector3(1.18f, 1.08f, side * 0.095f);
                Box("Замок кабинета", door.transform, position, new Vector3(0.19f, 0.27f, 0.045f), lockMaterial);
                var diamond = Box("Бубны на замке", door.transform, position + new Vector3(0, 0.025f, side * 0.03f),
                    new Vector3(0.09f, 0.09f, 0.018f), lockSymbolMaterial);
                diamond.transform.localRotation = Quaternion.Euler(0, 0, 45);
                Box("Скважина", door.transform, position + new Vector3(0, -0.075f, side * 0.031f),
                    new Vector3(0.025f, 0.055f, 0.018f), lockSymbolMaterial);
            }
        }

        Material LoadMaterial(string path, Color color)
        {
#if UNITY_EDITOR
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
#endif
            var fallback = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            fallback.color = color;
            fallback.name = "Fallback_" + path;
            return fallback;
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            var collider = result.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            Destroy(collider);
            return result;
        }
    }
}
