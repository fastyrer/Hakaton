using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Hakaton.Demo.Menu
{
    public sealed class MenuButtonFeedback : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        public Text label;public Image marker;
        void Set(bool selected){label.color=selected?new Color(.92f,.69f,.36f):new Color(.84f,.81f,.72f);marker.enabled=selected;}
        void OnEnable()=>Set(false);
        public void OnPointerEnter(PointerEventData e){Set(true);}
        public void OnPointerExit(PointerEventData e){Set(EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject==gameObject);}
        public void OnSelect(BaseEventData e)=>Set(true);
        public void OnDeselect(BaseEventData e)=>Set(false);
    }
}
