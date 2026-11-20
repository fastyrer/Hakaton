using System.Collections.Generic;
using Hakaton.Demo;
using UnityEngine;

namespace Hakaton.LocationKit
{
    [DisallowMultipleComponent]
    public sealed class LocationRoot : MonoBehaviour
    {
        [Header("Название и вводный текст")]
        public string locationName = "Новая локация";
        public bool customPresentation = true;
        [TextArea(2, 4)] public string introduction = "Осмотрите помещение и соберите материалы по делу.";
        public string objective = "Осмотрите локацию · E — взаимодействие · Tab — доска";
        [Header("Игровые объекты")]
        public DemoGame game;
        public LocationSpawn spawn;
        public Transform architecture;
        public Transform props;
        public Transform lights;
        public Transform gameplay;

        void Awake()
        {
            Connect();
            if (game != null && game.player != null && spawn != null)
                game.player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
        }

        public void Connect()
        {
            if (game == null) game = GetComponentInChildren<DemoGame>(true);
            if (game == null) return;
            if (customPresentation)
            {
                game.locationTitle = locationName;
                game.locationIntroduction = introduction;
                game.locationObjective = objective;
            }
            var clues = new DemoInteractable[DemoGame.EvidenceTitles.Length];
            var shelters = new List<Collider>();
            var weather = new List<DemoWeather>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var item in root.GetComponentsInChildren<DemoInteractable>(true))
                    if (!(item is DemoDoor) && item.enabled && item.gameObject.activeInHierarchy && item.kind == DemoInteractable.Interaction.Evidence &&
                        item.evidenceId >= 0 && item.evidenceId < clues.Length && clues[item.evidenceId] == null)
                        clues[item.evidenceId] = item;
                foreach (var volume in root.GetComponentsInChildren<LocationShelter>(true))
                    if (volume.gameObject.activeInHierarchy) shelters.Add(volume.GetComponent<Collider>());
                weather.AddRange(root.GetComponentsInChildren<DemoWeather>(true));
            }
            game.evidence = clues;
            foreach (var w in weather)
            {
                w.player = game.player;
                if (customPresentation || shelters.Count > 0) w.useShelterVolumes = true;
                w.shelters = shelters.ToArray();
            }
        }
    }
}
