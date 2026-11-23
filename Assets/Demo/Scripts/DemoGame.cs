using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Hakaton.Demo
{
    public enum PuzzleState { Undiscovered, Available, Started, Solved }

    public sealed class DemoGame : MonoBehaviour
    {
        public enum Stage { Explore, Changed, Chase, Dead, Complete }
        public bool arrivalScene;
        public string locationTitle;
        [TextArea] public string locationIntroduction;
        public string locationObjective;
        public bool HasEnteredHouse { get; private set; }
        public string SelectedDestination { get; private set; }
        public Stage Phase { get; private set; }
        public DemoPlayer player;
        public DemoHud hud;
        public DetectiveBoard board;
        public DemoSound sound;
        public DemoDoor kitchenDoor;
        public Transform threat;
        public Transform brother;
        public Light[] hallLights;
        public DemoInteractable[] evidence;
        public readonly HashSet<int> Found = new HashSet<int>();
        public readonly Dictionary<string, PuzzleState> PuzzleStates = new Dictionary<string, PuzzleState>();
        public readonly HashSet<string> Items = new HashSet<string>();
        public bool Blocked => hud.Mode != DemoHud.Screen.Play;
        public bool InKitchen => player.transform.position.x > 2.5f && player.transform.position.z < 3.7f;
        public float ChaseElapsed { get; private set; }
        bool quietEvent;
        bool escapeHeld, journalHeld;
        int threatWaypoint;
        float threatSteps;
        Vector3 threatOrigin;

        public static readonly string[] EvidenceTitles =
        { "Семейная фотография", "Письмо без конверта", "Запись о встрече",
          "Протокол обнаружения тела", "Опись пистолета · .45 ACP", "Заключение эксперта · 9×19 мм" };
        public static readonly string[] EvidenceText =
        {
            "Старая фотография всей семьи. Удивительно, что отец хранил её. Здесь мы такие молодые...\n\n\n\n6 30",
            "Бумага сложена вчетверо. Подписи нет.\n\n«Я не уеду, пока не закончу с бумагами. Не говори мальчикам. Я объясню всё сам».\n\nДаты нет. Из письма нельзя понять, кому оно адресовано.",
            "Оторванный лист настольного календаря. Октябрь 1997.\n\n«Четверг. 8:30 PM. Келлер. Боковая дверь. Папка — в кухне».\n\nЗапланированная встреча ещё не доказывает, что она состоялась. Стоит спросить Антона о посетителе.",
            "ДЕЛО В-97/041 · КОПИЯ ПРОТОКОЛА\n\n16 октября 1997 года, 06:40. Тело моего отца, Волкова, обнаружено у служебного подъезда старой насосной станции.\n\nОгнестрельное ранение головы. Справа от тела, в 38 см от кисти, лежал пистолет. В материалах стоит предварительная версия: самоубийство.\n\nПриложения: опись оружия и заключение эксперта.",
            "ДЕЛО В-97/041 · ВЕЩДОК № 12\n\nПистолет Colt M1911A1. Штатный калибр: .45 ACP (11,43×23 мм). Найден рядом с телом отца.\n\nОружие опечатано. Следов переоборудования ствола не установлено.\n\nВ описи нет подтверждения, что именно из этого пистолета произведён смертельный выстрел.",
            "ДЕЛО В-97/041 · ЭКСПЕРТИЗА № 73\n\nПри вскрытии из головы отца извлечена пуля. Установленный калибр: 9×19 мм Parabellum.\n\nКалибр пули НЕ СОВПАДАЕТ с калибром найденного рядом Colt M1911A1 — .45 ACP.\n\nЭтот пистолет не мог выстрелить извлечённой пулей. Вывод о самоубийстве требует пересмотра; второе оружие не найдено."
        };

        void Start()
        {
            Application.targetFrameRate = 60;
            Phase = Stage.Explore;
            if (board == null) board = GetComponent<DetectiveBoard>() ?? gameObject.AddComponent<DetectiveBoard>();
            board.Initialize(this);
            InvestigationTravel.Restore(this);
            if (threat != null)
            {
                threatOrigin = threat.position;
                threat.gameObject.SetActive(false);
            }
            hud.Initialize(this);
            if(!CarIntro.CarIntroArrival.TryEnter(this))hud.ShowIntro();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            bool escape = keyboard != null && keyboard.escapeKey.isPressed;
            bool journal = keyboard != null && keyboard.tabKey.isPressed;
            bool escapePressed = escape && !escapeHeld;
            bool journalPressed = journal && !journalHeld;
            escapeHeld = escape;
            journalHeld = journal;
            if (keyboard != null)
            {
                if (escapePressed) hud.Back();
                else if (journalPressed && Phase != Stage.Dead && Phase != Stage.Complete)
                {
                    if (hud.Mode == DemoHud.Screen.Play || hud.Mode == DemoHud.Screen.Evidence || hud.Mode == DemoHud.Screen.Document) hud.ShowBoard();
                    else if (hud.Mode == DemoHud.Screen.Board || hud.Mode == DemoHud.Screen.Journal) hud.Play();
                }
            }
            if (Blocked) return;
            if (arrivalScene)
            {
                Vector3 position = player.transform.position;
                if (Mathf.Abs(position.x) < 8.8f && position.z > -4.7f && position.z < 12.8f)
                    HasEnteredHouse = true;
                return;
            }
            if (!quietEvent && Found.Count > 0 && Mathf.Abs(player.transform.position.x) < 1.25f)
            {
                quietEvent = true;
                sound.Knock(new Vector3(-2, 1.5f, 9));
                hud.Notify("[ За стеной — три глухих удара. ]", 4);
            }
            if (Phase != Stage.Chase) return;
            ChaseElapsed += Time.deltaTime;
            if (ChaseElapsed < 1.7f) return;
            Vector3 destination;
            if (threatWaypoint == 0)
            {
                destination = new Vector3(0, 0, 0);
                if (Vector3.Distance(threat.position, destination) < 0.25f) threatWaypoint++;
            }
            else if (threatWaypoint == 1)
            {
                destination = new Vector3(2, 0, 0);
                if (Vector3.Distance(threat.position, destination) < 0.25f) threatWaypoint++;
            }
            else destination = new Vector3(player.transform.position.x, 0, player.transform.position.z);
            threat.position = Vector3.MoveTowards(threat.position, destination, 2.9f * Time.deltaTime);
            Vector3 direction = destination - threat.position;
            if (direction.sqrMagnitude > 0.01f) threat.rotation = Quaternion.LookRotation(direction);
            threat.GetChild(0).localRotation = Quaternion.Euler(0, 0, Mathf.Sin(ChaseElapsed * 9) * 6);
            threatSteps -= Time.deltaTime;
            if (threatSteps < 0) { sound.Knock(threat.position + Vector3.up); threatSteps = 0.53f; }
            if (Vector2.Distance(new Vector2(threat.position.x, threat.position.z),
                    new Vector2(player.transform.position.x, player.transform.position.z)) < 0.85f)
            {
                Phase = Stage.Dead;
                sound.Stinger();
                hud.ShowDeath();
            }
        }

        public string Objective => !string.IsNullOrWhiteSpace(locationObjective) ? locationObjective : arrivalScene
            ? (HasEnteredHouse ? "Осмотрите дом · машина во дворе открывает выбор локации" : "Войдите в дом · патрульная машина припаркована во дворе")
            : Phase switch
        {
            Stage.Explore => Found.Contains(2) ? "Покажите Антону запись о встрече" : "Осмотрите вещи отца · улики " + Found.Count + "/3",
            Stage.Changed => "Проверьте щиток в дальнем конце коридора",
            Stage.Chase => InKitchen ? "Закройте за собой дверь кухни · E" : "Бегите на кухню · освещённый проход слева впереди",
            Stage.Complete => "Вы в безопасности",
            _ => ""
        };

        public static string HomeScene=>Application.CanStreamedLevelBeLoaded("FatherHouse")?"FatherHouse":"HouseArrival";
        public void OpenTravelMenu() => hud.ShowTravelMenu();

        public void SelectDestination(string destination)
        {
            SelectedDestination = destination;
            string scene = destination == "Полицейский участок" ? "PoliceStation" : destination == "Дом Волковых" ? HomeScene : null;
            if (scene != null && SceneManager.GetActiveScene().name != scene && Application.CanStreamedLevelBeLoaded(scene))
            {
                InvestigationTravel.Capture(this);
                Time.timeScale = 1;
                SceneManager.LoadScene(scene);
                return;
            }
            hud.ShowDestinationStub(destination);
        }

        public PuzzleState GetPuzzleState(string id) => !string.IsNullOrWhiteSpace(id) && PuzzleStates.TryGetValue(id, out var state)
            ? state : PuzzleState.Undiscovered;

        public void SetPuzzleState(string id, PuzzleState state)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            if (!PuzzleStates.TryGetValue(id, out var current) || state > current) PuzzleStates[id] = state;
        }

        public bool HasItem(string id) => !string.IsNullOrWhiteSpace(id) && Items.Contains(id);
        public bool GrantItem(string id) => !string.IsNullOrWhiteSpace(id) && Items.Add(id);

        public void Inspect(DemoInteractable item)
        {
            if (item == null || item.evidenceId < 0 || item.evidenceId >= EvidenceTitles.Length) return;
            if (Found.Add(item.evidenceId)) { sound.Creak(item.transform.position); SubtitleSequence.Fire("evidence_" + item.evidenceId); }
            hud.ShowEvidence(item.evidenceId, DemoHud.Screen.Play);
        }

        public Texture2D EvidenceIllustration(int id) => evidence != null && id >= 0 && id < evidence.Length && evidence[id] != null
            ? evidence[id].illustration : InvestigationTravel.Illustration(id);

        public void Talk()
        {
            if (Phase == Stage.Dead) { hud.ShowDialogue("АНТОН", "Ты снова здесь. Дом ещё стоит. Может, на этот раз всё будет иначе."); return; }
            if (Phase == Stage.Complete) { hud.ShowDialogue("АНТОН", "Ты выбрался. Теперь остаётся только решить, что делать с тем, что нашёл."); return; }
            hud.ShowDialogue(Phase == Stage.Explore
                ? "АНТОН\n\nТы всё-таки приехал. В гостиной ещё остались фотографии. Кабинет я почти не трогал.\n\nВ тот вечер никто чужой не приходил. Отец собирался уехать — по крайней мере, он так сказал."
                : "АНТОН\n\nСвет опять погас? Щиток в конце коридора. Я подожду здесь.");
        }

        public void Present(int id)
        {
            if (!Found.Contains(id)) return;
            if (id != 2)
            {
                hud.ShowDialogue(id == 0
                    ? "АНТОН\n\nДа, это мы. Первый год в этом доме. Но фотография ничего не говорит о том вечере."
                    : "АНТОН\n\nЯ не знаю, кому он писал. Тут даже даты нет. Ты нашёл что-нибудь ещё?");
                return;
            }
            if (Phase != Stage.Explore) { Talk(); return; }
            hud.ShowRevelation("АНТОН\n\nКеллер?.. Я слышал машину у боковой двери. Подумал, отец опять говорил по делам. Самого посетителя я не видел.\n\nПапка могла остаться на кухне. Подожди… у тебя тоже погас свет?", ChangeHouse);
        }

        void ChangeHouse()
        {
            Phase = Stage.Changed;
            foreach (var light in hallLights) { light.color = new Color(0.8f, 0.17f, 0.075f); light.intensity *= 0.6f; }
            sound.Knock(new Vector3(0, 1, 12));
            hud.Play();
            hud.Notify("[ Из дальнего конца коридора слышен треск проводов. ]", 5);
        }

        public void UseFuseBox()
        {
            if (Phase == Stage.Changed) StartChase();
            else hud.Notify("Щиток закрыт. Сейчас здесь всё в порядке.");
        }

        public void StartChase()
        {
            Phase = Stage.Chase;
            ChaseElapsed = threatSteps = 0;
            threatWaypoint = 0;
            threat.position = threatOrigin;
            threat.rotation = Quaternion.Euler(0, 180, 0);
            threat.gameObject.SetActive(true);
            kitchenDoor.SetOpen(true);
            player.Teleport(new Vector3(0, 0.1f, 9.1f), 180);
            player.flashlight.enabled = true;
            sound.Stinger();
            hud.Play();
            hud.Notify("БЕГИТЕ НА КУХНЮ. ЗАКРОЙТЕ ДВЕРЬ.", 5);
        }

        public void Escape()
        {
            Phase = Stage.Complete;
            threat.gameObject.SetActive(false);
            hud.ShowEnding();
        }

        public void Restart() { InvestigationTravel.Reset(); Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        void OnDestroy() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; Time.timeScale = 1; }
    }
}
