using System.Linq;
using Hakaton.Demo.CarIntro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Hakaton.Demo.Investigation
{
    public sealed class CaseCarMonologue : MonoBehaviour
    {
        CarIntroSequence sequence;CaseDialogueLine[] lines;int line,frame;GameObject canvas;Text text;bool finished;
        public void Initialize(CarIntroSequence intro)
        {
            sequence=intro;sequence.HoldForMonologue();
            lines=JsonUtility.FromJson<CaseDialogueCollection>(Resources.Load<TextAsset>("CaseDialogues").text).scenes.First(s=>s.id==1).lines.Where(l=>l.speaker!="@").ToArray();
            canvas=new GameObject("Монолог в машине",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=100;
            var scale=canvas.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);
            var panel=CaseUI.Box(canvas.transform,"Субтитры",120,484,1040,212,new Color(.02f,.025f,.02f,.92f));
            text=CaseUI.Text(panel.transform,"",24,18,992,125,24);CaseUI.Button(panel.transform,"ДАЛЕЕ [ПРОБЕЛ / ENTER]",24,152,992,Next,42);
            if(UnityEngine.EventSystems.EventSystem.current==null){var events=new GameObject("Ввод монолога",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));events.transform.SetParent(canvas.transform,false);}
            Cursor.visible=true;Cursor.lockState=CursorLockMode.None;Render();
        }
        void Render(){text.text=lines[line].text;frame=Time.frameCount;}
        void Update(){var k=Keyboard.current;if(k!=null&&Time.frameCount>frame&&(k.spaceKey.wasPressedThisFrame||k.enterKey.wasPressedThisFrame))Next();}
        void Next(){if(finished||Time.frameCount==frame)return;if(++line<lines.Length){Render();return;}finished=true;CaseFile.Current.Complete(1);CaseFile.Save();Destroy(canvas);sequence.BeginAfterMonologue();Destroy(this);}
    }
}
