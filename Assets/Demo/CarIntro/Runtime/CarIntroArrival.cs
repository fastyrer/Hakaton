using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hakaton.LocationKit;
using Hakaton.Demo.Vehicles;
namespace Hakaton.Demo.CarIntro
{
    // One-shot audio/black-frame bridge; normal travel and direct scene opening keep their introduction.
    public sealed class CarIntroArrival : MonoBehaviour
    {
        static CarIntroArrival current;
        AudioSource rain;Transform parkedCar;Image black;DemoGame game;DemoWeather[] weather;
        float elapsed,rainLevel;bool entered;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset()=>current=null;
        public static void Begin(CarIntroSequence sequence)
        {
            if(current!=null)Destroy(current.gameObject);
            var root=new GameObject("Прибытие · плавный переход");var bridge=root.AddComponent<CarIntroArrival>();current=bridge;DontDestroyOnLoad(root);
            sequence.RestoreWindowMaterials();
            bridge.parkedCar=sequence.car;sequence.car.SetPositionAndRotation(CarIntroSequence.Road(1),CarIntroSequence.Heading(1));sequence.car.SetParent(root.transform,true);
            var rig=sequence.car.GetComponent<CrownVictoriaRig>();if(rig!=null)foreach(Transform driver in rig.driverSocket)driver.gameObject.SetActive(false);
            bridge.rain=sequence.ambience;
            if(bridge.rain!=null){bridge.rain.transform.SetParent(root.transform,false);bridge.rainLevel=Mathf.Min(bridge.rain.volume,CarIntroSequence.RainEndVolume);bridge.rain.volume=bridge.rainLevel;if(!bridge.rain.isPlaying)bridge.rain.Play();}
            // Canvas lives on a child: descendants of a ScreenSpaceOverlay canvas are forced into
            // screen space, which would corrupt the parked car parented to this same root.
            var canvasRoot=new GameObject("Затемнение",typeof(Canvas));canvasRoot.transform.SetParent(root.transform,false);
            var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32000;
            bridge.black=new GameObject("Чёрный кадр",typeof(RectTransform),typeof(Image)).GetComponent<Image>();bridge.black.transform.SetParent(canvasRoot.transform,false);var r=bridge.black.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;bridge.black.color=Color.black;bridge.black.raycastTarget=false;
            SceneManager.sceneLoaded+=bridge.Loaded;SceneManager.LoadScene(DemoGame.HomeScene);
        }
        void Loaded(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!=DemoGame.HomeScene){Destroy(gameObject);return;}
            if(parkedCar!=null)
            {
                foreach(var piece in FindObjectsByType<LocationPiece>())if(piece.kitId=="car")Destroy(piece.gameObject);
                parkedCar.SetParent(null,true);SceneManager.MoveGameObjectToScene(parkedCar.gameObject,scene);
                parkedCar.SetPositionAndRotation(CarIntroSequence.Road(1),CarIntroSequence.Heading(1));
                if(parkedCar.GetComponent<BoxCollider>()==null){var box=parkedCar.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.72f,0);box.size=new Vector3(1.98628f,1.44f,5.3848f);}
                var interaction=parkedCar.GetComponent<DemoInteractable>()??parkedCar.gameObject.AddComponent<DemoInteractable>();interaction.kind=DemoInteractable.Interaction.Car;interaction.label="Полицейская машина · выбрать локацию";
            }
            else Debug.LogWarning("CarIntroArrival: машина из вступления не доехала до сцены, оставляю машину из набора локации.");
            weather=FindObjectsByType<DemoWeather>();foreach(var w in weather)w.RainFade=0;
        }
        public static bool TryEnter(DemoGame owner)
        {
            if(current==null||current.entered||owner.gameObject.scene.name!=DemoGame.HomeScene)return false;
            current.game=owner;current.entered=true;owner.hud.Play();owner.player.Teleport(new Vector3(0,.08f,-14),0);owner.player.enabled=false;return true;
        }
        void LateUpdate()
        {
            if(!entered)return;elapsed+=Time.unscaledDeltaTime;float u=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/1.6f));
            black.color=new Color(0,0,0,1-u);if(rain!=null)rain.volume=rainLevel*(1-u);if(weather!=null)foreach(var w in weather)if(w!=null)w.RainFade=u;
            if(elapsed>=1.6f){if(game!=null)game.player.enabled=true;Destroy(gameObject);}
        }
        void OnDestroy()
        {
            SceneManager.sceneLoaded-=Loaded;if(game!=null)game.player.enabled=true;if(weather!=null)foreach(var w in weather)if(w!=null)w.RainFade=1;if(current==this)current=null;
        }
    }
}
