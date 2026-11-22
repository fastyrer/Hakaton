using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Hakaton.Demo.Menu
{
    public sealed class MainMenu : MonoBehaviour
    {
        public Button newGame, introduction, settings, quit, back;
        public RectTransform settingsPanel, menuPanel;
        public Slider volume, mouse;
        public Text volumeValue, mouseValue, status;
        public Image transition;
        public bool IsLoading{get;private set;}
        public bool SettingsOpen=>settingsPanel.gameObject.activeSelf;
        void Awake()
        {
            Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            AudioListener.volume=DemoPreferences.Volume;
            volume.SetValueWithoutNotify(DemoPreferences.Volume);mouse.SetValueWithoutNotify(DemoPreferences.MouseSensitivity);
            volume.onValueChanged.AddListener(SetVolume);mouse.onValueChanged.AddListener(SetMouse);
            newGame.onClick.AddListener(StartGame);introduction.onClick.AddListener(WatchIntroduction);
            settings.onClick.AddListener(OpenSettings);back.onClick.AddListener(CloseSettings);quit.onClick.AddListener(Quit);
            settingsPanel.gameObject.SetActive(false);RefreshValues();
            var c=transition.color;c.a=0;transition.color=c;transition.raycastTarget=false;
        }
        void Start(){if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(newGame.gameObject);}
        void Update(){if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&SettingsOpen)CloseSettings();}
        void SetVolume(float value){DemoPreferences.SetVolume(value);RefreshValues();}
        void SetMouse(float value){DemoPreferences.SetMouse(value);RefreshValues();}
        void RefreshValues(){volumeValue.text=Mathf.RoundToInt(volume.value*100)+"%";mouseValue.text=Mathf.RoundToInt(Mathf.InverseLerp(.03f,.3f,mouse.value)*100)+"%";}
        public void OpenSettings()
        {
            if(IsLoading)return;menuPanel.gameObject.SetActive(false);settingsPanel.gameObject.SetActive(true);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(volume.gameObject);
        }
        public void CloseSettings()
        {
            PlayerPrefs.Save();settingsPanel.gameObject.SetActive(false);menuPanel.gameObject.SetActive(true);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(settings.gameObject);
        }
        public void StartGame()
        {
            if(IsLoading)return;InvestigationTravel.Reset();
            if(Application.CanStreamedLevelBeLoaded("CarIntro"))Load("CarIntro");
            else{MenuNavigation.SetTrailerDestination(DemoGame.HomeScene);Load("OpeningTrailer");}
        }
        public void WatchIntroduction()
        {
            if(IsLoading)return;MenuNavigation.SetTrailerDestination("MainMenu");Load("OpeningTrailer");
        }
        void Load(string scene)
        {
            if(!Application.CanStreamedLevelBeLoaded(scene)){status.text="Не удалось открыть сцену. Проверьте список сцен сборки.";return;}
            IsLoading=true;transition.raycastTarget=true;StartCoroutine(EnterScene(scene));
        }
        IEnumerator EnterScene(string scene)
        {
            float time=0;while(time<.5f){time+=Time.unscaledDeltaTime;var c=transition.color;c.a=Mathf.Clamp01(time/.5f);transition.color=c;yield return null;}
            PlayerPrefs.Save();yield return SceneManager.LoadSceneAsync(scene);
        }
        public void Quit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}
