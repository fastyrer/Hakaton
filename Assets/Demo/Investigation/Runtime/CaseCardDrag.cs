using UnityEngine;
using UnityEngine.EventSystems;
namespace Hakaton.Demo.Investigation
{
    public sealed class CaseCardDrag : MonoBehaviour,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public InvestigationBoardView board;public string id;bool dragged;
        public void OnPointerClick(PointerEventData e){if(!dragged&&e.button==PointerEventData.InputButton.Left)board.Select(id);}
        public void OnBeginDrag(PointerEventData e){dragged=true;board.Select(id);transform.SetAsLastSibling();}
        public void OnDrag(PointerEventData e)
        {var canvas=GetComponentInParent<Canvas>();board.Move(id,e.delta/(canvas!=null?canvas.scaleFactor:1));}
        public void OnEndDrag(PointerEventData e){board.Save();dragged=false;}
    }
}
