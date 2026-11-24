using System;
using UnityEngine;
using UnityEngine.UI;
namespace Hakaton.Demo.Investigation
{
    // Shared, resolution-independent controls for the case file and conversations.
    public static class CaseUI
    {
        public static readonly Color Paper=new Color(.15f,.17f,.14f),Ink=new Color(.91f,.87f,.76f),Gold=new Color(.83f,.62f,.33f);
        public static RectTransform Rect(Transform p,string name,float x,float y,float w,float h)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
        public static Image Box(Transform p,string name,float x,float y,float w,float h,Color color)
        {var r=Rect(p,name,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=color;return im;}
        public static Text Text(Transform p,string value,float x,float y,float w,float h,int size=18,Color? color=null)
        {var r=Rect(p,"Text",x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.fontSize=size;t.color=color??Ink;t.raycastTarget=false;return t;}
        public static Button Button(Transform p,string text,float x,float y,float w,Action action,float h=43)
        {var im=Box(p,text,x,y,w,h,Paper);var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.onClick.AddListener(()=>action());var t=Text(im.transform,text,12,4,w-24,h-8,17);t.alignment=TextAnchor.MiddleLeft;return b;}
        public static InputField Input(Transform p,string text,float x,float y,float w,float h,bool multi)
        {
            var im=Box(p,"Редактор",x,y,w,h,new Color(.08f,.10f,.08f));var f=im.gameObject.AddComponent<InputField>();f.targetGraphic=im;
            var t=Text(im.transform,"",9,6,w-18,h-12,18);t.supportRichText=false;f.textComponent=t;f.lineType=multi?InputField.LineType.MultiLineNewline:InputField.LineType.SingleLine;f.characterLimit=multi?2400:100;f.text=text;return f;
        }
        public static RectTransform Scroll(Transform p,float x,float y,float w,float h,float contentHeight,out ScrollRect scroll)
        {
            var im=Box(p,"Прокрутка",x,y,w,h,new Color(.05f,.07f,.055f));var view=im.rectTransform;view.gameObject.AddComponent<RectMask2D>();
            scroll=view.gameObject.AddComponent<ScrollRect>();var content=Rect(view,"Содержимое",0,0,w-12,contentHeight);scroll.viewport=view;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=28;scroll.movementType=ScrollRect.MovementType.Clamped;return content;
        }
    }
}
